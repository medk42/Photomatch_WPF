﻿using Photomatch_ProofOfConcept_WPF.Gui.GuiControls.Helper;
using Photomatch_ProofOfConcept_WPF.Gui.GuiControls.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers;
using Photomatch_ProofOfConcept_WPF.Logic;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls.ModelCreationToolHandlers
{
	public class ModelCreationEdgeHandler : BaseModelCreationToolHandler
	{
		public override ModelCreationTool ToolType => ModelCreationTool.Edge;

		private PerspectiveData Perspective;
		private Model Model;
		private ModelVisualization ModelVisualization;
		private IWindow Window;

		private double PointDrawRadius;
		private double PointGrabRadius;

		private IModelCreationEdgeHandlerDirection HoldDirectionSelector;
		private IEllipse ModelHoverEllipse;
		private ILine ModelEdgeLine;
		private IModelCreationEdgeHandlerSelector[] VertexSelectors;
		private IModelCreationEdgeHandlerDirection[] DirectionSelectors;
		private IModelCreationEdgeHandlerVertex FirstVertex;

		private IModelCreationEdgeHandlerVertex CurrentVertex;
		private ModelCreationEdgeHandlerDirectionProjection CurrentDirection;

		public ModelCreationEdgeHandler(PerspectiveData perspective, Model model, ModelVisualization modelVisualization, IWindow window, double pointDrawRadius, double pointGrabRadius)
		{
			this.ModelVisualization = modelVisualization;
			this.Perspective = perspective;
			this.Model = model;
			this.Window = window;

			this.PointDrawRadius = pointDrawRadius;
			this.PointGrabRadius = pointGrabRadius;

			this.ModelHoverEllipse = Window.CreateEllipse(new Vector2(), PointDrawRadius, ApplicationColor.Highlight);
			this.ModelHoverEllipse.Visible = false;

			this.ModelEdgeLine = Window.CreateLine(new Vector2(), new Vector2(), 0, ApplicationColor.Highlight);
			this.ModelEdgeLine.Visible = false;

			this.VertexSelectors = new IModelCreationEdgeHandlerSelector[]
			{
				new ModelCreationEdgeHandlerVertexSelector(ModelVisualization),
				new ModelCreationEdgeHandlerMidpointSelector(ModelVisualization, Model, Perspective, Window, PointGrabRadius),
				new ModelCreationEdgeHandlerEdgepointSelector(ModelVisualization, Model, Perspective)
			};

			this.DirectionSelectors = new IModelCreationEdgeHandlerDirection[]
			{
				new VectorDirection(new Vector3(1, 0, 0), Perspective, ApplicationColor.XAxis),
				new VectorDirection(new Vector3(0, 1, 0), Perspective, ApplicationColor.YAxis),
				new VectorDirection(new Vector3(0, 0, 1), Perspective, ApplicationColor.ZAxis)
			};

			this.Active = false;
			SetActive(Active);
		}

		public override void MouseMove(Vector2 mouseCoord)
		{
			if (Active)
			{
				CurrentVertex = null;
				CurrentDirection = null;
				ModelHoverEllipse.Visible = false;
				foreach (var selector in VertexSelectors)
				{
					CurrentVertex = selector.GetVertex(mouseCoord);
					if (CurrentVertex != null)
					{
						ModelHoverEllipse.Position = CurrentVertex.ScreenPosition;
						ModelHoverEllipse.Visible = true;
						ModelHoverEllipse.Color = selector.VertexColor;
						break;
					}
				}

				if (FirstVertex != null)
				{
					if (HoldDirectionSelector != null)
					{
						CurrentDirection = HoldDirectionSelector.Project(FirstVertex.WorldPosition, mouseCoord);
						if (CurrentVertex != null)
						{
							Ray3D CurrentDirectionRay = new Ray3D(FirstVertex.WorldPosition, CurrentDirection.Direction);
							Vector3Proj currentVertexProj = Intersections3D.ProjectVectorToRay(CurrentVertex.WorldPosition, CurrentDirectionRay);

							CurrentDirection = new ModelCreationEdgeHandlerDirectionProjection()
							{
								ProjectedWorld = currentVertexProj.Projection,
								DistanceWorld = currentVertexProj.Distance,
								Direction = CurrentDirection.Direction
							};

							if (!CurrentVertex.UpdateToHoldRay(CurrentDirectionRay))
							{
								CurrentVertex = null;
								ModelEdgeLine.End = Perspective.WorldToScreen(CurrentDirection.ProjectedWorld);
							}
							else
							{
								ModelHoverEllipse.Position = CurrentVertex.ScreenPosition;
								ModelHoverEllipse.Color = ApplicationColor.Edgepoint;
								ModelEdgeLine.End = CurrentVertex.ScreenPosition;
							}
						}
						else
						{
							ModelEdgeLine.End = Perspective.WorldToScreen(CurrentDirection.ProjectedWorld);
						}
						ModelEdgeLine.Color = HoldDirectionSelector.EdgeColor;
					}
					else
					{
						if (CurrentVertex != null)
						{
							ModelEdgeLine.End = CurrentVertex.ScreenPosition;
							ModelEdgeLine.Color = ApplicationColor.Highlight;
						}
						else
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
							ModelEdgeLine.End = Perspective.WorldToScreen(bestDirection.ProjectedWorld);
							ModelEdgeLine.Color = bestColor;

							CurrentDirection = bestDirection;
						}
					}
				}
			}
		}

		public override void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			if (Active)
			{
				if (FirstVertex == null)
				{
					if (CurrentVertex != null)
					{
						FirstVertex = CurrentVertex;
						ModelEdgeLine.Start = CurrentVertex.ScreenPosition;
						ModelEdgeLine.End = CurrentVertex.ScreenPosition;
						ModelEdgeLine.Visible = true;
					}
				}
				else
				{
					if (CurrentVertex == null)
						Model.AddEdge(FirstVertex.ModelVertex, Model.AddVertex(CurrentDirection.ProjectedWorld));
					else
						Model.AddEdge(FirstVertex.ModelVertex, CurrentVertex.ModelVertex);

					ModelEdgeLine.Visible = false;
					FirstVertex = null;
				}
			}
		}

		public override void KeyDown(KeyboardKey key)
		{
			if (Active)
			{
				switch (key)
				{
					case KeyboardKey.LeftShift:
						if (CurrentDirection != null)
							HoldDirectionSelector = new VectorDirection(CurrentDirection.Direction, Perspective, ApplicationColor.Highlight);
						else if (FirstVertex != null && CurrentVertex != null)
							HoldDirectionSelector = new VectorDirection((CurrentVertex.WorldPosition - FirstVertex.WorldPosition).Normalized(), Perspective, ApplicationColor.Highlight);
						break;
					case KeyboardKey.Escape:
						CancelLineCreate();
						break;
				}
			}
		}

		public override void KeyUp(KeyboardKey key)
		{
			if (Active)
			{
				switch (key)
				{
					case KeyboardKey.LeftShift:
						HoldDirectionSelector = null;
						break;
				}
			}
		}

		internal override void SetActive(bool active)
		{
			if (!active)
			{
				CancelLineCreate();
				ModelHoverEllipse.Visible = false;
			}
		}

		private void CancelLineCreate()
		{
			if (FirstVertex != null)
			{
				FirstVertex = null;
				ModelEdgeLine.Visible = false;
			}
		}

		public override void Dispose()
		{
			Perspective = null;
			VertexSelectors = null;
			DirectionSelectors = null;
			ModelEdgeLine.Dispose();
			ModelHoverEllipse.Dispose();
		}

		public override void UpdateModel(Model model)
		{
			Model = model;

			foreach (var selector in VertexSelectors)
				selector.UpdateModel(model);
		}
	}
}
