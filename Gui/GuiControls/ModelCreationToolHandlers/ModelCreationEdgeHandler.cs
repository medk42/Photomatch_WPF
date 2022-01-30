using Photomatch_ProofOfConcept_WPF.Gui.GuiControls.Helper;
using Photomatch_ProofOfConcept_WPF.Logic;
using System;
using System.Collections.Generic;
using System.Text;

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

		private Vector2 LastMouseCoord;
		private Vertex ModelDraggingVertex = null;
		private Ray2D ModelDraggingXAxis, ModelDraggingYAxis, ModelDraggingZAxis, LastRay;
		private Vertex ModelDraggingLineStart;
		private bool HoldDirection;
		private Vector3 LastDirection = new Vector3(1, 0, 0);

		private IEllipse EdgeHoverEllipse;
		private Edge FoundEdge;
		private Vector3 FoundEdgePoint;

		public ModelCreationEdgeHandler(PerspectiveData perspective, Model model, ModelVisualization modelVisualization, IWindow window, double pointDrawRadius, double pointGrabRadius)
		{
			this.ModelVisualization = modelVisualization;
			this.Perspective = perspective;
			this.Model = model;
			this.Window = window;

			this.PointDrawRadius = pointDrawRadius;
			this.PointGrabRadius = pointGrabRadius;

			this.EdgeHoverEllipse = Window.CreateEllipse(new Vector2(), PointDrawRadius, ApplicationColor.Highlight);
			this.EdgeHoverEllipse.Visible = false;

			this.Active = false;
			SetActive(Active);
		}

		private Tuple<Vector3, Edge> GetMidpointUnderMouse(Vector2 mouseCoord)
		{
			foreach (Edge edge in Model.Edges)
			{
				if (edge.Start == ModelDraggingVertex || edge.End == ModelDraggingVertex)
					continue;

				Vector3 midpoint = (edge.Start.Position + edge.End.Position) / 2;
				Vector2 midpointScreen = Perspective.WorldToScreen(midpoint);

				if (Window.ScreenDistance(mouseCoord, midpointScreen) < PointGrabRadius)
					return new Tuple<Vector3, Edge>(midpoint, edge);
			}

			return null;
		}

		private Tuple<Vector3, Edge> GetEdgePointUnderMouse(Vector2 mouseCoord)
		{
			foreach (Edge edge in Model.Edges)
			{
				if (edge.Start == ModelDraggingVertex || edge.End == ModelDraggingVertex)
					continue;

				Ray3D mouseRay = Perspective.ScreenToWorldRay(mouseCoord);
				Line3D edgeLine = new Line3D(edge.Start.Position, edge.End.Position);
				ClosestPoint3D closest = Intersections3D.GetRayRayClosest(mouseRay, edgeLine.AsRay());

				if (closest.RayBRelative >= 0 && closest.RayBRelative <= edgeLine.Length)
				{
					Vector3 edgeClosestPoint = closest.RayBClosest;
					Vector2 edgeClosestPointScreen = Perspective.WorldToScreen(edgeClosestPoint);

					if (Window.ScreenDistance(mouseCoord, edgeClosestPointScreen) < PointGrabRadius)
						return new Tuple<Vector3, Edge>(edgeClosestPoint, edge);
				}
			}

			return null;
		}

		public override void MouseMove(Vector2 mouseCoord)
		{
			if (Active)
			{
				LastMouseCoord = mouseCoord;

				if (ModelDraggingVertex != null)
				{
					ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);

					Vertex foundPoint = ModelVisualization.GetVertexUnderMouse(mouseCoord).Item1;
					Vector3 foundPosition = (foundPoint != null && foundPoint != ModelDraggingVertex) ? foundPoint.Position : Vector3.InvalidInstance;
					ModelVisualization.ModelHoverEllipse.Ellipse.Color = ApplicationColor.Vertex;

					FoundEdge = null;
					if (!foundPosition.Valid)
					{
						var foundTuple = GetMidpointUnderMouse(mouseCoord);
						if (foundTuple != null)
						{
							ModelVisualization.ModelHoverEllipse.Ellipse.Color = ApplicationColor.Midpoint;
							EdgeHoverEllipse.Color = ApplicationColor.Midpoint;
						}

						if (foundTuple == null)
						{
							foundTuple = GetEdgePointUnderMouse(mouseCoord);
							if (foundTuple != null)
							{
								ModelVisualization.ModelHoverEllipse.Ellipse.Color = ApplicationColor.Edgepoint;
								EdgeHoverEllipse.Color = ApplicationColor.Edgepoint;
							}
						}

						if (foundTuple != null)
						{
							FoundEdge = foundTuple.Item2;
							foundPosition = foundTuple.Item1;
							FoundEdgePoint = foundTuple.Item1;

							if (HoldDirection)
							{
								Ray3D holdRay = new Ray3D(ModelDraggingLineStart.Position, LastDirection);
								Line3D edge = new Line3D(FoundEdge.Start.Position, FoundEdge.End.Position);
								ClosestPoint3D closestPoint = Intersections3D.GetRayRayClosest(holdRay, edge.AsRay());

								if (closestPoint.Distance < 1e-6 && closestPoint.RayBRelative >= 0 && closestPoint.RayBRelative <= edge.Length)
								{
									foundPosition = closestPoint.RayBClosest;
									FoundEdgePoint = closestPoint.RayBClosest;
									EdgeHoverEllipse.Color = ApplicationColor.Edgepoint;
								}
							}

							EdgeHoverEllipse.Position = Perspective.WorldToScreen(FoundEdgePoint);
						}
						EdgeHoverEllipse.Visible = foundTuple != null;
					}
					else
					{
						EdgeHoverEllipse.Visible = false;
					}

					if (foundPosition.Valid)
					{
						if (HoldDirection)
						{
							Vector3Proj foundPositionProj = Intersections3D.ProjectVectorToRay(foundPosition, new Ray3D(ModelDraggingLineStart.Position, LastDirection));
							ModelDraggingVertex.Position = foundPositionProj.Projection;
						}
						else
						{
							ModelDraggingVertex.Position = foundPosition;
							ModelVisualization.ModelDraggingLine.Color = ApplicationColor.Model;
							LastDirection = (foundPosition - ModelDraggingLineStart.Position).Normalized();

							Vector2 startScreen = Perspective.WorldToScreen(ModelDraggingLineStart.Position);
							Vector2 endScreen = Perspective.WorldToScreen(foundPosition);

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
								ModelVisualization.ModelDraggingLine.Color = ApplicationColor.XAxis;
							}
							else
							{
								LastDirection = new Vector3(0, 0, 1);
								LastRay = ModelDraggingZAxis;
								ModelDraggingVertex.Position = Intersections3D.GetRayRayClosest(Perspective.ScreenToWorldRay(projZ.Projection), new Ray3D(ModelDraggingLineStart.Position, LastDirection)).RayBClosest;
								ModelVisualization.ModelDraggingLine.Color = ApplicationColor.ZAxis;
							}
						}
						else if (projY.Distance < projZ.Distance)
						{
							LastDirection = new Vector3(0, 1, 0);
							LastRay = ModelDraggingYAxis;
							ModelDraggingVertex.Position = Intersections3D.GetRayRayClosest(Perspective.ScreenToWorldRay(projY.Projection), new Ray3D(ModelDraggingLineStart.Position, LastDirection)).RayBClosest;
							ModelVisualization.ModelDraggingLine.Color = ApplicationColor.YAxis;
						}
						else
						{
							LastDirection = new Vector3(0, 0, 1);
							LastRay = ModelDraggingZAxis;
							ModelDraggingVertex.Position = Intersections3D.GetRayRayClosest(Perspective.ScreenToWorldRay(projZ.Projection), new Ray3D(ModelDraggingLineStart.Position, LastDirection)).RayBClosest;
							ModelVisualization.ModelDraggingLine.Color = ApplicationColor.ZAxis;
						}
					}
				}
				else
				{
					if (ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord))
						EdgeHoverEllipse.Visible = false;
					else
					{
						var foundTuple = GetMidpointUnderMouse(mouseCoord);
						EdgeHoverEllipse.Color = ApplicationColor.Midpoint;

						if (foundTuple == null)
						{
							foundTuple = GetEdgePointUnderMouse(mouseCoord);
							EdgeHoverEllipse.Color = ApplicationColor.Edgepoint;
						}

						if (foundTuple != null)
						{
							Vector2 foundMidpointScreen = Perspective.WorldToScreen(foundTuple.Item1);
							EdgeHoverEllipse.Position = foundMidpointScreen;
						}
						EdgeHoverEllipse.Visible = foundTuple != null;
					}
				}

				
			}
		}

		public override void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			if (Active)
			{
				LastMouseCoord = mouseCoord;

				if (button != MouseButton.Left)
					return;

				Vertex foundPoint = ModelVisualization.GetVertexUnderMouse(mouseCoord).Item1;

				if (ModelDraggingVertex != null)
				{
					ModelVisualization.ModelHoverEllipse.Ellipse.Color = ApplicationColor.Vertex;
					if (foundPoint != null && foundPoint != ModelDraggingVertex && !HoldDirection)
					{
						ModelDraggingVertex.ConnectedEdges[0].RemoveVerticesOnRemove = false;
						ModelDraggingVertex.Remove();
						Model.AddEdge(ModelDraggingLineStart, foundPoint);
					}

					if (FoundEdge != null)
					{
						if (HoldDirection)
						{
							Ray3D holdRay = new Ray3D(ModelDraggingLineStart.Position, LastDirection);
							Line3D edge = new Line3D(FoundEdge.Start.Position, FoundEdge.End.Position);
							ClosestPoint3D closestPoint = Intersections3D.GetRayRayClosest(holdRay, edge.AsRay());

							if (closestPoint.Distance < 1e-6 && closestPoint.RayBRelative >= 0 && closestPoint.RayBRelative <= edge.Length)
							{
								ModelDraggingVertex.ConnectedEdges[0].RemoveVerticesOnRemove = false;
								ModelDraggingVertex.Remove();
								Vertex edgeVertex = AddVertexToEdge(closestPoint.RayBClosest, FoundEdge);
								Model.AddEdge(ModelDraggingLineStart, edgeVertex);
							}
						}
						else
						{
							ModelDraggingVertex.ConnectedEdges[0].RemoveVerticesOnRemove = false;
							ModelDraggingVertex.Remove();
							Vertex edgeVertex = AddVertexToEdge(FoundEdgePoint, FoundEdge);
							Model.AddEdge(ModelDraggingLineStart, edgeVertex);
						}
					}

					ModelDraggingVertex = null;
					ModelVisualization.ModelDraggingLine.Color = ApplicationColor.Model;
				}
				else
				{
					if (foundPoint == null)
					{
						var foundTuple = GetMidpointUnderMouse(mouseCoord);

						if (foundTuple == null)
							foundTuple = GetEdgePointUnderMouse(mouseCoord);

						if (foundTuple != null)
							foundPoint = AddVertexToEdge(foundTuple.Item1, foundTuple.Item2);
					}

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

				ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);
			}
		}

		private Vertex AddVertexToEdge(Vector3 vertexPosition, Edge edge)
		{
			Vertex newVertex = Model.AddVertex(vertexPosition);
			Vertex start = edge.Start;
			Vertex end = edge.End;
			edge.RemoveVerticesOnRemove = false;
			edge.Remove();
			Model.AddEdge(start, newVertex);
			Model.AddEdge(newVertex, end);

			return newVertex;
		}

		public override void MouseUp(Vector2 mouseCoord, MouseButton button)
		{
			if (Active)
			{
				LastMouseCoord = mouseCoord;
			}
		}

		public override void KeyDown(KeyboardKey key)
		{
			if (Active)
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
		}

		public override void KeyUp(KeyboardKey key)
		{
			if (Active)
			{
				switch (key)
				{
					case KeyboardKey.LeftShift:
						HoldDirection = false;
						break;
				}
			}
		}

		internal override void SetActive(bool active)
		{
			ModelVisualization.ModelHoverEllipse.Active = active;

			if (!active)
			{
				CancelLineCreate();
				EdgeHoverEllipse.Visible = false;
			}
		}

		private void CancelLineCreate()
		{
			ModelVisualization.ModelHoverEllipse.Ellipse.Color = ApplicationColor.Vertex;

			if (ModelDraggingVertex != null)
			{
				ModelDraggingVertex.Remove();
				ModelDraggingVertex = null;
				ModelVisualization.ModelHoverEllipse.MouseEvent(LastMouseCoord);
			}
		}

		public override void Dispose()
		{
			Perspective = null;
		}

		public override void UpdateModel(Model model)
		{
			Model = model;
		}
	}
}
