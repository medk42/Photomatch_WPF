using PhotomatchCore.Gui.GuiControls.Helper;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using PhotomatchCore.Logic.Model;
using PhotomatchCore.Logic.Perspective;

namespace PhotomatchCore.Gui.GuiControls
{
	class CameraModelCalibrationHandler
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

		private enum ToolState { CalibrateOriginSelectPoint, CalibrateOriginDraggingPoint, CalibrateScaleSelectPoint, CalibrateScaleDraggingPoint }

		private Model Model;
		private PerspectiveData Perspective;
		private IWindow Window;

		private double PointGrabRadius;
		private double PointDrawRadius;

		private readonly ModelVisualization ModelVisualization;
		private ToolState State;

		private Vertex SelectedVertex;
		private Vertex FixedVertex;
		private IEllipse SelectedEllipse;

		public CameraModelCalibrationHandler(ModelVisualization modelVisualization, Model model, PerspectiveData perspective, IWindow window, double pointGrabRadius, double pointDrawRadius)
		{
			this.ModelVisualization = modelVisualization;
			this.Model = model;
			this.Perspective = perspective;
			this.Window = window;
			this.PointGrabRadius = pointGrabRadius;
			this.PointDrawRadius = pointDrawRadius;

			this.State = ToolState.CalibrateOriginSelectPoint;

			this.SelectedEllipse = Window.CreateEllipse(new Vector2(), PointDrawRadius, ApplicationColor.Selected);
			this.SelectedEllipse.Visible = false;

			this.Active = false;
			SetActive(Active);
		}

		private void UpdateOrigin(Vector2 mouseCoord)
		{
			Perspective.Origin = Perspective.MatchScreenWorldPoint(mouseCoord, SelectedVertex.Position);
			ModelVisualization.UpdateDisplayedGeometry();
		}

		private void UpdateScale(Vector2 mouseCoord)
		{
			Vector3 originScale = Perspective.MatchScreenWorldPoints(Perspective.WorldToScreen(FixedVertex.Position), FixedVertex.Position, mouseCoord, SelectedVertex.Position);
			if (originScale.Z > 0)
			{
				Perspective.Scale = originScale.Z;
				Perspective.Origin = new Vector2(originScale.X, originScale.Y);
			}
			ModelVisualization.UpdateDisplayedGeometry();
		}

		public void MouseMove(Vector2 mouseCoord)
		{
			if (Active)
			{
				switch (State)
				{
					case ToolState.CalibrateOriginSelectPoint:
						ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);
						break;
					case ToolState.CalibrateOriginDraggingPoint:
						UpdateOrigin(mouseCoord);
						break;
					case ToolState.CalibrateScaleSelectPoint:
						ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);
						break;
					case ToolState.CalibrateScaleDraggingPoint:
						UpdateScale(mouseCoord);
						break;
				}
			}
		}

		public void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			if (Active)
			{
				Vertex vertex;

				switch (State)
				{
					case ToolState.CalibrateOriginSelectPoint:
						if (button != MouseButton.Left)
							return;

						vertex = ModelVisualization.GetVertexUnderMouse(mouseCoord).Item1;
						if (vertex != null)
						{
							SelectedVertex = vertex;
							State = ToolState.CalibrateOriginDraggingPoint;
							ModelVisualization.ModelHoverEllipse.Active = false;
							UpdateOrigin(mouseCoord);
						}
						break;
					case ToolState.CalibrateScaleSelectPoint:
						if (button == MouseButton.Right)
						{
							vertex = ModelVisualization.GetVertexUnderMouse(mouseCoord).Item1;
							if (vertex != null)
								FixedVertex = vertex;
							SelectedEllipse.Position = Perspective.WorldToScreen(FixedVertex.Position);
						}
						else if (button == MouseButton.Left)
						{
							vertex = ModelVisualization.GetVertexUnderMouse(mouseCoord).Item1;
							if (vertex != null && vertex != FixedVertex)
							{
								SelectedVertex = vertex;
								State = ToolState.CalibrateScaleDraggingPoint;
								ModelVisualization.ModelHoverEllipse.Active = false;

								UpdateScale(mouseCoord);
							}
						}
						break;
				}
			}
		}

		public void MouseUp(Vector2 mouseCoord, MouseButton button)
		{
			if (Active)
			{
				switch (State)
				{
					case ToolState.CalibrateOriginDraggingPoint:
						State = ToolState.CalibrateOriginSelectPoint;
						ModelVisualization.ModelHoverEllipse.Active = true;
						ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);
						break;
					case ToolState.CalibrateScaleDraggingPoint:
						State = ToolState.CalibrateScaleSelectPoint;
						ModelVisualization.ModelHoverEllipse.Active = true;
						ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);
						break;
				}
			}
		}

		private void SetActive(bool active)
		{
			ModelVisualization.ModelHoverEllipse.Active = active;

			ModelVisualization.ShowModel(active);

			if (active)
				SelectedEllipse.Visible = (State == ToolState.CalibrateScaleSelectPoint || State == ToolState.CalibrateScaleDraggingPoint);
			else
				SelectedEllipse.Visible = false;
		}

		public void CalibrationTool_Changed(CameraModelCalibrationTool newCameraModelCalibrationTool)
		{
			switch (newCameraModelCalibrationTool)
			{
				case CameraModelCalibrationTool.CalibrateOrigin:
					State = ToolState.CalibrateOriginSelectPoint;
					ModelVisualization.ModelHoverEllipse.Active = Active;

					SelectedEllipse.Visible = false;
					break;
				case CameraModelCalibrationTool.CalibrateScale:
					State = ToolState.CalibrateScaleSelectPoint;
					ModelVisualization.ModelHoverEllipse.Active = Active;

					FixedVertex = Model.Vertices[0];
					SelectedEllipse.Visible = Active;
					SelectedEllipse.Position = Perspective.WorldToScreen(FixedVertex.Position);
					break;
			}
		}

		public void UpdateModel(Model model)
		{
			Model = model;
			FixedVertex = Model.Vertices[0];
			SelectedEllipse.Position = Perspective.WorldToScreen(FixedVertex.Position);
		}

		public void UpdateDisplayedGeometry()
		{
			if (FixedVertex != null)
				SelectedEllipse.Position = Perspective.WorldToScreen(FixedVertex.Position);
		}
	}
}
