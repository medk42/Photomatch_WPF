using PhotomatchCore.Gui.GuiControls.Helper;
using PhotomatchCore.Utilities;
using PhotomatchCore.Logic.Model;
using PhotomatchCore.Logic.Perspective;
using PhotomatchCore.Gui.GuiControls.ToolHandlers.ModelCreationToolHandlers.Helper;
using PhotomatchCore.Gui.GuiControls.ToolHandlers.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers;
using System;

namespace PhotomatchCore.Gui.GuiControls.ToolHandlers.ModelCreationToolHandlers
{

	/// <summary>
	/// Class for handling edge creation.
	/// </summary>
	public class ModelCreationEdgeHandler : BaseModelCreationToolHandler
	{
		/// <summary>
		/// Enum representing the states of this class.
		/// </summary>
		private enum EdgeState { 
			/// <summary>
			/// Default state, mouse is not above model, nothing is selected.
			/// </summary>
			Nothing, 
			
			/// <summary>
			/// Mouse is above model, but nothing is selected - start edge from this vertex/mid/edgepoint (3d cursor displayed).
			/// </summary>
			MouseAboveModel,

			/// <summary>
			/// First vertex selected - next state is EdgeMouseAboveModel or EdgeXYZ based on the location of the mouse (3d cursor displayed).
			/// </summary>
			FirstVertexSelected,

			/// <summary>
			/// Mouse above model, first vertex selected - end edge at this vertex/mid/edgepoint  (3d cursor displayed).
			/// </summary>
			EdgeMouseAboveModel,

			/// <summary>
			/// Mouse is not above model, first vertex selected - edge ends closest to mouse on x, y or z axis (3d cursor displayed at the end of line).
			/// </summary>
			EdgeXYZ,

			/// <summary>
			/// Mouse above model, first vertex selected, SHIFT is down - keep edge direction, end edge closest to the 3d point 
			/// selected on model or on the vertex/edge under mouse if the created edge can intersect it (3d cursor displayed).
			/// </summary>
			EdgeHoldDirToModel,

			/// <summary>
			/// Mouse above model, first vertex selected, SHIFT is down - keep edge direction, edge ends closest to mouse on its 
			/// direction (3d cursor displayed at the end of line).
			/// </summary>
			EdgeHoldDir
		}

		public override ModelCreationTool ToolType => ModelCreationTool.Edge;

		private PerspectiveData Perspective;
		private Model Model;
		private ModelVisualization ModelVisualization;
		private IImageView Window;

		private double PointDrawRadius;
		private double PointGrabRadius;

		private IEllipse ModelHoverEllipse;
		private ILine ModelEdgeLine;
		private IModelCreationEdgeHandlerSelector[] VertexSelectors;
		private VectorDirection[] DirectionSelectors;
		private Cursor Cursor;

		private VectorDirection HoldDirectionSelector;
		private IModelCreationEdgeHandlerVertex FirstVertex;
		private IModelCreationEdgeHandlerVertex CurrentVertex;
		private ModelCreationEdgeHandlerDirectionProjection CurrentDirection;
		private bool EdgeHoldingDirToModel = false;

		/// <summary>
		/// Make ModelEdgeLine visible for all states other than Nothing and MouseAboveModel.
		/// </summary>
		private EdgeState State
		{
			get => State_;
			set
			{
				State_ = value;
				ModelEdgeLine.Visible = !(State_ == EdgeState.Nothing || State_ == EdgeState.MouseAboveModel);
			}
		}
		private EdgeState State_;

		/// <summary>
		/// Handler uses 3 VertexSelectors - for selecting vertices, points on edges and edge midpoints. 
		/// Handler uses 3 DirectionSelectors - for selecting direction of the edge, x/y/z.
		/// Handler displays a 3d cursor when applicable.
		/// </summary>
		/// <param name="perspective">Handler converts between world and screen space.</param>
		/// <param name="model">Handler is creating edges.</param>
		/// <param name="modelVisualization">Handler displays the model.</param>
		/// <param name="window">Handler is displaying the edge to be created and other visualizations.</param>
		/// <param name="pointGrabRadius">Screen distance in pixels, from which a vertex/edge can be selected.</param>
		/// <param name="pointDrawRadius">The radius of drawn vertices in pixels on screen.</param>
		public ModelCreationEdgeHandler(PerspectiveData perspective, Model model, ModelVisualization modelVisualization, IImageView window, double pointDrawRadius, double pointGrabRadius)
		{
			ModelVisualization = modelVisualization;
			Perspective = perspective;
			Model = model;
			Window = window;

			PointDrawRadius = pointDrawRadius;
			PointGrabRadius = pointGrabRadius;

			ModelHoverEllipse = Window.CreateEllipse(new Vector2(), PointDrawRadius, ApplicationColor.Highlight);
			ModelHoverEllipse.Visible = false;

			ModelEdgeLine = Window.CreateLine(new Vector2(), new Vector2(), 0, ApplicationColor.Highlight);
			ModelEdgeLine.Visible = false;

			VertexSelectors = new IModelCreationEdgeHandlerSelector[]
			{
				new ModelCreationEdgeHandlerVertexSelector(ModelVisualization),
				new ModelCreationEdgeHandlerMidpointSelector(ModelVisualization, Model, Perspective, Window, PointGrabRadius),
				new ModelCreationEdgeHandlerEdgepointSelector(ModelVisualization, Model, Perspective)
			};

			DirectionSelectors = new VectorDirection[]
			{
				new VectorDirection(new Vector3(1, 0, 0), Perspective, ApplicationColor.XAxis),
				new VectorDirection(new Vector3(0, 1, 0), Perspective, ApplicationColor.YAxis),
				new VectorDirection(new Vector3(0, 0, 1), Perspective, ApplicationColor.ZAxis)
			};

			this.Cursor = new Cursor(window, perspective);

			this.State = EdgeState.Nothing;

			Active = false;
			SetActive(Active);
		}

		/// <summary>
		/// Get vertex/edge/midpoint under mouse on the model, put the 3d cursor on the vertex.
		/// </summary>
		/// <returns>Interface representing various data about the found vertex.</returns>
		private IModelCreationEdgeHandlerVertex GetVisualizeVertexUnderMouse(Vector2 mouseCoord)
		{
			ModelHoverEllipse.Visible = false;
			Cursor.Visible = false;

			foreach (var selector in VertexSelectors)
			{
				IModelCreationEdgeHandlerVertex vertexUnderMouse = selector.GetVertex(mouseCoord);
				if (vertexUnderMouse != null)
				{
					ModelHoverEllipse.Position = vertexUnderMouse.ScreenPosition;
					ModelHoverEllipse.Visible = true;
					ModelHoverEllipse.Color = selector.VertexColor;

					Cursor.Visible = true;
					Cursor.Position = vertexUnderMouse.WorldPosition;
					return vertexUnderMouse;
				}
			}

			return null;
		}

		/// <summary>
		/// Select the direction along which the edge would be closest to mouse. 
		/// </summary>
		/// <returns>
		/// The color of the selected direction and a class representing a point 
		/// at which the edge would be closest to mouse (on screen) if it followed
		/// selected direction.
		/// </returns>
		private Tuple<ApplicationColor, ModelCreationEdgeHandlerDirectionProjection> GetBestDirection(Vector2 mouseCoord)
		{
			var bestColor = DirectionSelectors[0].EdgeColor;
			var bestDirection = DirectionSelectors[0].Project(FirstVertex.WorldPosition, mouseCoord);
			foreach (var selector in DirectionSelectors)
			{
				var direction = selector.Project(FirstVertex.WorldPosition, mouseCoord);
				if (direction.DistanceScreen < bestDirection.DistanceScreen)
				{
					bestDirection = direction;
					bestColor = selector.EdgeColor;
				}
			}

			return new Tuple<ApplicationColor, ModelCreationEdgeHandlerDirectionProjection>(bestColor, bestDirection);
		}

		/// <summary>
		/// Change state and visualizations, see states for more detail.
		/// </summary>
		public override void MouseMove(Vector2 mouseCoord)
		{
			if (Active)
			{
				CurrentVertex = GetVisualizeVertexUnderMouse(mouseCoord);

				if (CurrentVertex == null)
				{
					switch (State)
					{
						case EdgeState.MouseAboveModel:
							State = EdgeState.Nothing;
							break;
						case EdgeState.FirstVertexSelected:
							State = EdgeState.EdgeXYZ;
							break;
						case EdgeState.EdgeMouseAboveModel:
							State = EdgeState.EdgeXYZ;
							break;
						case EdgeState.EdgeHoldDirToModel:
							State = EdgeState.EdgeHoldDir;
							break;
					}
				}
				else
				{
					switch (State)
					{
						case EdgeState.Nothing:
							State = EdgeState.MouseAboveModel;
							break;
						case EdgeState.FirstVertexSelected:
							State = EdgeState.EdgeMouseAboveModel;
							break;
						case EdgeState.EdgeXYZ:
							State = EdgeState.EdgeMouseAboveModel;
							break;
						case EdgeState.EdgeHoldDir:
							State = EdgeState.EdgeHoldDirToModel;
							break;
					}
				}

				switch (State)
				{
					case EdgeState.EdgeMouseAboveModel:
						ModelEdgeLine.End = CurrentVertex.ScreenPosition;
						ModelEdgeLine.Color = ApplicationColor.Highlight;
						break;

					case EdgeState.EdgeXYZ:
					case EdgeState.EdgeHoldDir:
						var bestDirection = GetBestDirection(mouseCoord);
						if (State == EdgeState.EdgeHoldDir)
							bestDirection = new Tuple<ApplicationColor, ModelCreationEdgeHandlerDirectionProjection>
							(HoldDirectionSelector.EdgeColor, HoldDirectionSelector.Project(FirstVertex.WorldPosition, mouseCoord));

						CurrentDirection = bestDirection.Item2;

						ModelEdgeLine.End = Perspective.WorldToScreen(CurrentDirection.ProjectedWorld);
						ModelEdgeLine.Color = bestDirection.Item1;

						Cursor.Position = CurrentDirection.ProjectedWorld;
						Cursor.Visible = true;
						break;

					case EdgeState.EdgeHoldDirToModel:						
						Ray3D CurrentDirectionRay = new Ray3D(FirstVertex.WorldPosition, HoldDirectionSelector.Direction);

						if (!CurrentVertex.UpdateToHoldRay(CurrentDirectionRay))
						{
							Vector3RayProj currentVertexProj = Intersections3D.ProjectVectorToRay(CurrentVertex.WorldPosition, CurrentDirectionRay);
							CurrentDirection = new ModelCreationEdgeHandlerDirectionProjection()
							{
								ProjectedWorld = currentVertexProj.Projection,
								DistanceWorld = currentVertexProj.Distance,
								Direction = CurrentDirection.Direction
							};
							EdgeHoldingDirToModel = false;
							ModelEdgeLine.End = Perspective.WorldToScreen(CurrentDirection.ProjectedWorld);
						}
						else
						{
							ModelHoverEllipse.Position = CurrentVertex.ScreenPosition;
							ModelHoverEllipse.Color = ApplicationColor.Edgepoint;
							ModelEdgeLine.End = CurrentVertex.ScreenPosition;
							EdgeHoldingDirToModel = true;
						}

						ModelEdgeLine.Color = HoldDirectionSelector.EdgeColor;
						break;
				}
			}
		}

		/// <summary>
		/// Change state, visualizations and possibly create edge.
		/// </summary>
		public override void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			if (Active)
			{
				switch (State)
				{
					case EdgeState.MouseAboveModel:
						FirstVertex = CurrentVertex;
						ModelEdgeLine.Start = CurrentVertex.ScreenPosition;
						ModelEdgeLine.End = CurrentVertex.ScreenPosition;

						State = EdgeState.FirstVertexSelected;
						break;
					case EdgeState.EdgeXYZ:
					case EdgeState.EdgeHoldDir:
						Model.AddEdge(FirstVertex.ModelVertex, Model.AddVertex(CurrentDirection.ProjectedWorld));

						State = EdgeState.Nothing;
						break;
					case EdgeState.EdgeMouseAboveModel:
						Model.AddEdge(FirstVertex.ModelVertex, CurrentVertex.ModelVertex);

						State = EdgeState.MouseAboveModel;
						break;
					case EdgeState.EdgeHoldDirToModel:
						if (EdgeHoldingDirToModel)
							Model.AddEdge(FirstVertex.ModelVertex, CurrentVertex.ModelVertex);
						else
							Model.AddEdge(FirstVertex.ModelVertex, Model.AddVertex(CurrentDirection.ProjectedWorld));

						State = EdgeState.MouseAboveModel;
						break;

				}
			}
		}

		/// <summary>
		/// Cancel line creation on ESC.
		/// Start holding direction in states EdgeXYZ and EdgeMouseAboveModel, switch to states EdgeHoldDir and EdgeHoldDirToModel.
		/// </summary>
		public override void KeyDown(KeyboardKey key)
		{
			if (Active)
			{
				foreach (var selector in VertexSelectors)
					selector.KeyDown(key);

				if (key == KeyboardKey.LeftShift)
				{
					switch (State)
					{
						case EdgeState.EdgeXYZ:
							State = EdgeState.EdgeHoldDir;
							HoldDirectionSelector = new VectorDirection(CurrentDirection.Direction, Perspective, ApplicationColor.Highlight);
							break;
						case EdgeState.EdgeMouseAboveModel:
							State = EdgeState.EdgeHoldDirToModel;
							HoldDirectionSelector = new VectorDirection((CurrentVertex.WorldPosition - FirstVertex.WorldPosition).Normalized(), Perspective, ApplicationColor.Highlight);
							break;
					}
				}
				else if (key == KeyboardKey.Escape)
				{
					switch (State)
					{
						case EdgeState.EdgeXYZ:
						case EdgeState.EdgeHoldDir:
							State = EdgeState.Nothing;
							break;
						case EdgeState.EdgeMouseAboveModel:
						case EdgeState.EdgeHoldDirToModel:
						case EdgeState.FirstVertexSelected:
							State = EdgeState.MouseAboveModel;
							break;
					}

					Cursor.Visible = false;
				}
			}
		}

		/// <summary>
		/// Stop holding direction in states EdgeHoldDir and EdgeHoldDirToModel, switch to states EdgeXYZ and EdgeMouseAboveModel.
		/// </summary>
		public override void KeyUp(KeyboardKey key)
		{
			if (Active)
			{
				foreach (var selector in VertexSelectors)
					selector.KeyUp(key);

				if (key == KeyboardKey.LeftShift)
				{
					switch (State)
					{
						case EdgeState.EdgeHoldDirToModel:
							State = EdgeState.EdgeMouseAboveModel;
							break;
						case EdgeState.EdgeHoldDir:
							State = EdgeState.EdgeXYZ;
							break;
					}
				}
			}
		}

		internal override void SetActive(bool active)
		{
			if (!active)
			{
				State = EdgeState.Nothing;
				ModelHoverEllipse.Visible = false;
				Cursor.Visible = false;
			}
		}

		public override void Dispose()
		{
			Perspective = null;
			VertexSelectors = null;
			DirectionSelectors = null;
			ModelEdgeLine.Dispose();
			ModelHoverEllipse.Dispose();
			Cursor.Dispose();
		}

		public override void UpdateModel(Model model)
		{
			Model = model;

			foreach (var selector in VertexSelectors)
				selector.UpdateModel(model);
		}
	}
}
