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

		private Vector2 LastMouseCoord;
		private Vertex ModelDraggingVertex = null;
		private Ray2D ModelDraggingXAxis, ModelDraggingYAxis, ModelDraggingZAxis, LastRay;
		private Vertex ModelDraggingLineStart;
		private bool HoldDirection;
		private Vector3 LastDirection = new Vector3(1, 0, 0);

		public ModelCreationEdgeHandler(PerspectiveData perspective, Model model, ModelVisualization modelVisualization)
		{
			this.ModelVisualization = modelVisualization;
			this.Perspective = perspective;
			this.Model = model;

			this.Active = false;
			SetActive(Active);
		}

		public override void MouseMove(Vector2 mouseCoord)
		{
			if (Active)
			{
				LastMouseCoord = mouseCoord;

				if (ModelDraggingVertex != null)
				{
					Vertex foundPoint = ModelVisualization.GetVertexUnderMouse(mouseCoord);

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
							ModelVisualization.ModelDraggingLine.Color = ApplicationColor.Model;
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

				ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);
			}
		}

		public override void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			if (Active)
			{
				LastMouseCoord = mouseCoord;

				if (button != MouseButton.Left)
					return;

				Vertex foundPoint = ModelVisualization.GetVertexUnderMouse(mouseCoord);

				if (ModelDraggingVertex != null)
				{
					if (foundPoint != null && foundPoint != ModelDraggingVertex && !HoldDirection)
					{
						ModelDraggingVertex.Remove();
						Model.AddEdge(ModelDraggingLineStart, foundPoint);
					}

					ModelDraggingVertex = null;
					ModelVisualization.ModelDraggingLine.Color = ApplicationColor.Model;
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

				ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);
			}
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
			}
		}

		private void CancelLineCreate()
		{

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
	}
}
