using System;
using System.Collections.Generic;
using System.Text;

using Photomatch_ProofOfConcept_WPF.Gui.GuiControls.Helper;
using Photomatch_ProofOfConcept_WPF.Logic;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls
{
	class ModelCreationHandler
	{
		private bool Active_;
		public bool Active
		{
			get => Active_;
			set
			{
				if (value != Active_)
				{
					Active_ = value;
					SetActive(Active_);
				}
			}
		}

		private Model Model;

		private List<Tuple<ILine, Edge, EdgeEventListener>> ModelLines = new List<Tuple<ILine, Edge, EdgeEventListener>>();
		private ModelCreationTool ModelCreationTool;
		private Vector2 LastMouseCoord;
		private ModelHoverEllipse ModelHoverEllipse;

		private Vertex ModelDraggingVertex = null;
		private Ray2D ModelDraggingXAxis, ModelDraggingYAxis, ModelDraggingZAxis, LastRay;
		private Vertex ModelDraggingLineStart;
		private ILine ModelDraggingLine;
		private bool HoldDirection;
		private Vector3 LastDirection = new Vector3(1, 0, 0);

		private PerspectiveData Perspective;
		private IWindow Window;

		private double PointGrabRadius;
		private double PointDrawRadius;

		public ModelCreationHandler(Model model, PerspectiveData perspective, IWindow window, double pointGrabRadius, double pointDrawRadius)
		{
			this.Model = model;
			this.Perspective = perspective;
			this.Window = window;
			this.PointGrabRadius = pointGrabRadius;
			this.PointDrawRadius = pointDrawRadius;
			this.ModelHoverEllipse = new ModelHoverEllipse(Model, Perspective, Window, PointGrabRadius, PointDrawRadius);

			CreateModelLines();

			this.Active = false;
			SetActive(Active);
		}

		public void MouseMove(Vector2 mouseCoord)
		{
			if (Active)
			{
				this.LastMouseCoord = mouseCoord;

				switch (ModelCreationTool)
				{
					case ModelCreationTool.Delete:
						this.ModelHoverEllipse.MouseEvent(mouseCoord);
						break;
					case ModelCreationTool.Edge:
						MouseMoveEdge(mouseCoord);
						this.ModelHoverEllipse.MouseEvent(mouseCoord);
						break;
					default:
						throw new Exception("Unknown switch case.");
				}
			}
		}

		public void MouseDown(Vector2 mouseCoord, MouseButton button)
		{

			if (Active)
			{
				this.LastMouseCoord = mouseCoord;

				switch (ModelCreationTool)
				{
					case ModelCreationTool.Delete:
						MouseDownDelete(mouseCoord, button);
						this.ModelHoverEllipse.MouseEvent(mouseCoord);
						break;
					case ModelCreationTool.Edge:
						MouseDownEdge(mouseCoord, button);
						this.ModelHoverEllipse.MouseEvent(mouseCoord);
						break;
					default:
						throw new Exception("Unknown switch case.");
				}
			}
		}

		private void MouseDownDelete(Vector2 mouseCoord, MouseButton button)
		{
			if (button != MouseButton.Left)
				return;

			Vertex foundPoint = GetVertexUnderMouse(mouseCoord);

			if (foundPoint != null)
				foundPoint.Remove();
		}

		private void MouseMoveEdge(Vector2 mouseCoord)
		{
			if (ModelDraggingVertex != null)
			{
				Vertex foundPoint = GetVertexUnderMouse(mouseCoord);

				if (foundPoint != null && foundPoint != ModelDraggingVertex)
				{
					if (HoldDirection)
					{
						Vector3Proj foundPointProj = Intersections3D.ProjectVectorToRay(foundPoint.Position, new Ray3D(ModelDraggingLineStart.Position, LastDirection));
						ModelDraggingVertex.Position = foundPointProj.Projection;
					}
					else
					{
						ModelDraggingVertex.Position = foundPoint.Position;
						ModelDraggingLine.Color = ApplicationColor.Model;
						LastDirection = (foundPoint.Position - ModelDraggingLineStart.Position).Normalized();

						Vector2 startScreen = Perspective.WorldToScreen(ModelDraggingLineStart.Position);
						Vector2 endScreen = Perspective.WorldToScreen(foundPoint.Position);

						LastRay = new Ray2D(startScreen, (endScreen - startScreen));
					}
				}
				else if (HoldDirection)
				{
					Vector2Proj mouseProj = Intersections2D.ProjectVectorToRay(mouseCoord, LastRay);
					ModelDraggingVertex.Position = Intersections3D.GetRayRayClosest(Perspective.ScreenToWorldRay(mouseProj.Projection), new Ray3D(ModelDraggingLineStart.Position, LastDirection)).RayBClosest;
				}
				else
				{
					Vector2Proj projX = Intersections2D.ProjectVectorToRay(mouseCoord, ModelDraggingXAxis);
					Vector2Proj projY = Intersections2D.ProjectVectorToRay(mouseCoord, ModelDraggingYAxis);
					Vector2Proj projZ = Intersections2D.ProjectVectorToRay(mouseCoord, ModelDraggingZAxis);

					if (projX.Distance < projY.Distance)
					{
						if (projX.Distance < projZ.Distance)
						{
							LastDirection = new Vector3(1, 0, 0);
							LastRay = ModelDraggingXAxis;
							ModelDraggingVertex.Position = Intersections3D.GetRayRayClosest(Perspective.ScreenToWorldRay(projX.Projection), new Ray3D(ModelDraggingLineStart.Position, LastDirection)).RayBClosest;
							ModelDraggingLine.Color = ApplicationColor.XAxis;
						}
						else
						{
							LastDirection = new Vector3(0, 0, 1);
							LastRay = ModelDraggingZAxis;
							ModelDraggingVertex.Position = Intersections3D.GetRayRayClosest(Perspective.ScreenToWorldRay(projZ.Projection), new Ray3D(ModelDraggingLineStart.Position, LastDirection)).RayBClosest;
							ModelDraggingLine.Color = ApplicationColor.ZAxis;
						}
					}
					else if (projY.Distance < projZ.Distance)
					{
						LastDirection = new Vector3(0, 1, 0);
						LastRay = ModelDraggingYAxis;
						ModelDraggingVertex.Position = Intersections3D.GetRayRayClosest(Perspective.ScreenToWorldRay(projY.Projection), new Ray3D(ModelDraggingLineStart.Position, LastDirection)).RayBClosest;
						ModelDraggingLine.Color = ApplicationColor.YAxis;
					}
					else
					{
						LastDirection = new Vector3(0, 0, 1);
						LastRay = ModelDraggingZAxis;
						ModelDraggingVertex.Position = Intersections3D.GetRayRayClosest(Perspective.ScreenToWorldRay(projZ.Projection), new Ray3D(ModelDraggingLineStart.Position, LastDirection)).RayBClosest;
						ModelDraggingLine.Color = ApplicationColor.ZAxis;
					}
				}
			}
		}

		private void MouseDownEdge(Vector2 mouseCoord, MouseButton button)
		{
			if (button != MouseButton.Left)
				return;

			Vertex foundPoint = GetVertexUnderMouse(mouseCoord);

			if (ModelDraggingVertex != null)
			{
				if (foundPoint != null && foundPoint != ModelDraggingVertex && !HoldDirection)
				{
					ModelDraggingVertex.Remove();
					Model.AddEdge(ModelDraggingLineStart, foundPoint);
				}

				ModelDraggingVertex = null;
				ModelDraggingLine.Color = ApplicationColor.Model;
			}
			else
			{
				if (foundPoint != null)
				{
					Vector2 screenPos = Perspective.WorldToScreen(foundPoint.Position);

					ModelDraggingVertex = Model.AddVertex(foundPoint.Position);
					ModelDraggingXAxis = new Ray2D(screenPos, Perspective.GetXDirAt(screenPos));
					ModelDraggingYAxis = new Ray2D(screenPos, Perspective.GetYDirAt(screenPos));
					ModelDraggingZAxis = new Ray2D(screenPos, Perspective.GetZDirAt(screenPos));
					ModelDraggingLineStart = foundPoint;

					Model.AddEdge(foundPoint, ModelDraggingVertex);
				}
			}
		}

		public Vertex GetVertexUnderMouse(Vector2 mouseCoord)
		{

			foreach (Vertex point in Model.Vertices)
			{
				Vector2 pointPos = Perspective.WorldToScreen(point.Position);
				if (Window.ScreenDistance(mouseCoord, pointPos) < PointGrabRadius)
				{
					return point;
				}
			}

			return null;
		}

		private void CancelLineCreate()
		{
			if (ModelDraggingVertex != null)
			{
				ModelDraggingVertex.Remove();
				ModelDraggingVertex = null;
				this.ModelHoverEllipse.MouseEvent(LastMouseCoord);
			}
		}

		public void MouseUp(Vector2 mouseCoord, MouseButton button) { }

		public void KeyDown(KeyboardKey key)
		{
			switch (key)
			{
				case KeyboardKey.LeftShift:
					HoldDirection = true;
					break;
				case KeyboardKey.Escape:
					CancelLineCreate();
					break;
			}
		}

		public void KeyUp(KeyboardKey key)
		{
			switch (key)
			{
				case KeyboardKey.LeftShift:
					HoldDirection = false;
					break;
			}
		}

		private void EdgeRemoved(Edge edge)
		{
			edge.EdgeRemovedEvent -= EdgeRemoved;
			var tuple = ModelLines.Find((edgeTuple) => edgeTuple.Item2 == edge);

			edge.StartPositionChangedEvent -= tuple.Item3.StartPositionChanged;
			edge.EndPositionChangedEvent -= tuple.Item3.EndPositionChanged;

			tuple.Item1.Dispose();
			ModelLines.Remove(tuple);
		}

		private void EdgeAdderHelper(Edge edge)
		{
			Vector2 start = Perspective.WorldToScreen(edge.Start.Position);
			Vector2 end = Perspective.WorldToScreen(edge.End.Position);
			ILine windowLine = Window.CreateLine(start, end, 0, ApplicationColor.Model);
			EdgeEventListener edgeEventListener = new EdgeEventListener(windowLine, Perspective);
			edge.StartPositionChangedEvent += edgeEventListener.StartPositionChanged;
			edge.EndPositionChangedEvent += edgeEventListener.EndPositionChanged;
			edge.EdgeRemovedEvent += EdgeRemoved;
			ModelLines.Add(new Tuple<ILine, Edge, EdgeEventListener>(windowLine, edge, edgeEventListener));
			ModelDraggingLine = windowLine;
		}

		private void CreateModelLines()
		{
			Model.AddEdgeEvent += EdgeAdderHelper;

			foreach (Edge line in Model.Edges)
				EdgeAdderHelper(line);
		}

		private void SetActive(bool active)
		{
			ShowModel(active);
			if (!active)
			{
				CancelLineCreate();
			}
		}

		public void ShowModel(bool show)
		{
			foreach (var lineTuple in ModelLines)
				lineTuple.Item1.Visible = show;
		}

		public void UpdateDisplayedLines()
		{
			foreach (var lineTuple in ModelLines)
			{
				lineTuple.Item1.Start = Perspective.WorldToScreen(lineTuple.Item2.Start.Position);
				lineTuple.Item1.End = Perspective.WorldToScreen(lineTuple.Item2.End.Position);
			}
		}

		public void Dispose()
		{
			foreach (var lineTuple in ModelLines)
			{
				lineTuple.Item2.StartPositionChangedEvent -= lineTuple.Item3.StartPositionChanged;
				lineTuple.Item2.EndPositionChangedEvent -= lineTuple.Item3.EndPositionChanged;
				lineTuple.Item2.EdgeRemovedEvent -= EdgeRemoved;
			}

			ModelLines.Clear();

			Model.AddEdgeEvent -= EdgeAdderHelper;

			Perspective = null;
		}

		public void CreationTool_Changed(ModelCreationTool newModelCreationTool)
		{
			this.ModelCreationTool = newModelCreationTool;
		}
	}
}
