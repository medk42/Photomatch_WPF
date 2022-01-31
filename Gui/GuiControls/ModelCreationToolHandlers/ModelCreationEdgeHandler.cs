using Photomatch_ProofOfConcept_WPF.Gui.GuiControls.Helper;
using Photomatch_ProofOfConcept_WPF.Logic;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls.ModelCreationToolHandlers
{
	public interface IModelCreationEdgeHandlerSelector
	{
		ApplicationColor VertexColor { get; }
		IModelCreationEdgeHandlerVertex GetVertex(Vector2 mouseCoord);
		void UpdateModel(Model model);
	}

	public interface IModelCreationEdgeHandlerVertex
	{
		Vector2 ScreenPosition { get; }
		Vector3 WorldPosition { get; }
		Vertex ModelVertex { get; }
		bool UpdateToHoldRay(Ray3D holdRay);
	}

	public interface IModelCreationEdgeHandlerDirection
	{
		ApplicationColor EdgeColor { get; }
		ModelCreationEdgeHandlerDirectionProjection Project(Vector3 from, Vector2 mouseCoord);
	}

	public class ModelCreationEdgeHandlerDirectionProjection
	{
		public Vector3 ProjectedWorld { get; set; }
		public Vector2 ProjectedScreen { get; set; }
		public double DistanceWorld { get; set; }
		public double DistanceScreen { get; set; }
		public Vector3 Direction { get; set; }
	}

	public class MainAxis : IModelCreationEdgeHandlerDirection
	{
		public ApplicationColor EdgeColor { get; private set; }

		private Vector3 Direction;
		private PerspectiveData Perspetive;

		public MainAxis(Vector3 direction, PerspectiveData perspetive, ApplicationColor axisColor)
		{
			this.Direction = direction;
			this.Perspetive = perspetive;
			this.EdgeColor = axisColor;
		}

		public ModelCreationEdgeHandlerDirectionProjection Project(Vector3 from, Vector2 mouseCoord)
		{
			Ray2D screenRay = new Line2D(Perspetive.WorldToScreen(from), Perspetive.WorldToScreen(from + Direction)).AsRay();
			Vector2Proj screenProject = Intersections2D.ProjectVectorToRay(mouseCoord, screenRay);

			ClosestPoint3D worldClosestPoint = Intersections3D.GetRayRayClosest(new Ray3D(from, Direction), Perspetive.ScreenToWorldRay(mouseCoord));

			return new ModelCreationEdgeHandlerDirectionProjection()
			{
				ProjectedScreen = screenProject.Projection,
				DistanceScreen = screenProject.Distance,
				ProjectedWorld = worldClosestPoint.RayAClosest,
				DistanceWorld = worldClosestPoint.Distance,
				Direction = Direction
			};
		}
	}

	public class ModelCreationEdgeHandlerVertexSelector : IModelCreationEdgeHandlerSelector
	{
		private class SelectedVertex : IModelCreationEdgeHandlerVertex
		{
			public Vector2 ScreenPosition { get; set; }

			public Vector3 WorldPosition { get; set; }

			public Vertex ModelVertex { get; set; }

			public bool UpdateToHoldRay(Ray3D holdRay)
			{
				Vector3Proj worldPosProject = Intersections3D.ProjectVectorToRay(WorldPosition, holdRay);

				if (worldPosProject.Distance <= 1e-6)
				{
					return true;
				}

				return false;
			}
		}
		public ApplicationColor VertexColor => ApplicationColor.Vertex;

		private ModelVisualization ModelVisualization;

		public ModelCreationEdgeHandlerVertexSelector(ModelVisualization modelVisualization)
		{
			this.ModelVisualization = modelVisualization;
		}

		public IModelCreationEdgeHandlerVertex GetVertex(Vector2 mouseCoord)
		{
			var vertexTuple = ModelVisualization.GetVertexUnderMouse(mouseCoord);
			if (vertexTuple.Item1 != null)
				return new SelectedVertex()
				{
					ScreenPosition = vertexTuple.Item2,
					WorldPosition = vertexTuple.Item1.Position,
					ModelVertex = vertexTuple.Item1
				};
			else
				return null;
		}

		public void UpdateModel(Model model) { }
	}

	public class SelectedEdgepoint : IModelCreationEdgeHandlerVertex
	{
		private Model Model;
		private PerspectiveData Perspective;

		private Edge Edge;
		private Vector3 Edgepoint;
		private Vertex Vertex;

		public SelectedEdgepoint(Edge edge, Vector3 edgepoint, Model model, PerspectiveData perspective)
		{
			this.Edge = edge;
			this.Edgepoint = edgepoint;
			this.Model = model;
			this.Perspective = perspective;
		}

		public Vector2 ScreenPosition => Perspective.WorldToScreen(Edgepoint);

		public Vector3 WorldPosition => Edgepoint;

		public Vertex ModelVertex
		{
			get
			{
				if (Vertex == null)
					Vertex = Model.AddVertexToEdge(Edgepoint, Edge);
				return Vertex;
			}
		}

		public bool UpdateToHoldRay(Ray3D holdRay)
		{
			Line3D edge = new Line3D(Edge.Start.Position, Edge.End.Position);
			ClosestPoint3D closestPoint = Intersections3D.GetRayRayClosest(holdRay, edge.AsRay());

			if (closestPoint.Distance < 1e-6 && closestPoint.RayBRelative >= 0 && closestPoint.RayBRelative <= edge.Length)
			{
				Edgepoint = closestPoint.RayBClosest;
				return true;
			}

			return false;
		}
	}

	public class ModelCreationEdgeHandlerMidpointSelector : IModelCreationEdgeHandlerSelector
	{
		public ApplicationColor VertexColor => ApplicationColor.Midpoint;

		private ModelVisualization ModelVisualization;
		private Model Model;
		private PerspectiveData Perspective;
		private IWindow Window;

		private double PointGrabRadius;

		public ModelCreationEdgeHandlerMidpointSelector(ModelVisualization modelVisualization, Model model, PerspectiveData perspective, IWindow window, double pointGrabRadius)
		{
			this.ModelVisualization = modelVisualization;
			this.Model = model;
			this.Perspective = perspective;
			this.Window = window;
			this.PointGrabRadius = pointGrabRadius;
		}

		public IModelCreationEdgeHandlerVertex GetVertex(Vector2 mouseCoord)
		{
			var edgeTuple = ModelVisualization.GetEdgeUnderMouse(mouseCoord);
			if (edgeTuple != null)
			{
				Edge edge = edgeTuple.Item1;
				Vector3 midpoint = (edge.Start.Position + edge.End.Position) / 2;
				if (Window.ScreenDistance(mouseCoord, Perspective.WorldToScreen(midpoint)) < PointGrabRadius)
					return new SelectedEdgepoint(edge, midpoint, Model, Perspective);
				else
					return null;
			}
			else
			{
				return null;
			}
		}

		public void UpdateModel(Model model)
		{
			this.Model = model;
		}
	}

	public class ModelCreationEdgeHandlerEdgepointSelector : IModelCreationEdgeHandlerSelector
	{
		public ApplicationColor VertexColor => ApplicationColor.Edgepoint;

		private ModelVisualization ModelVisualization;
		private Model Model;
		private PerspectiveData Perspective;

		public ModelCreationEdgeHandlerEdgepointSelector(ModelVisualization modelVisualization, Model model, PerspectiveData perspective)
		{
			this.ModelVisualization = modelVisualization;
			this.Model = model;
			this.Perspective = perspective;
		}

		public IModelCreationEdgeHandlerVertex GetVertex(Vector2 mouseCoord)
		{
			var edgeTuple = ModelVisualization.GetEdgeUnderMouse(mouseCoord);
			if (edgeTuple != null)
			{
				Edge edge = edgeTuple.Item1;

				Ray3D mouseRay = Perspective.ScreenToWorldRay(mouseCoord);
				Line3D edgeLine = new Line3D(edge.Start.Position, edge.End.Position);
				ClosestPoint3D closest = Intersections3D.GetRayRayClosest(mouseRay, edgeLine.AsRay());
				Vector3 edgeClosestPoint = closest.RayBClosest;

				return new SelectedEdgepoint(edge, edgeClosestPoint, Model, Perspective);
			}
			else
			{
				return null;
			}
		}

		public void UpdateModel(Model model)
		{
			this.Model = model;
		}
	}

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
				new MainAxis(new Vector3(1, 0, 0), Perspective, ApplicationColor.XAxis),
				new MainAxis(new Vector3(0, 1, 0), Perspective, ApplicationColor.YAxis),
				new MainAxis(new Vector3(0, 0, 1), Perspective, ApplicationColor.ZAxis)
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
							HoldDirectionSelector = new MainAxis(CurrentDirection.Direction, Perspective, ApplicationColor.Highlight);
						else if (FirstVertex != null && CurrentVertex != null)
							HoldDirectionSelector = new MainAxis((CurrentVertex.WorldPosition - FirstVertex.WorldPosition).Normalized(), Perspective, ApplicationColor.Highlight);
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
