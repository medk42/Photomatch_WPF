using Photomatch_ProofOfConcept_WPF.Gui.GuiControls.Helper;
using Photomatch_ProofOfConcept_WPF.Logic;
using Photomatch_ProofOfConcept_WPF.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls
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

		private readonly ModelCreationHandler ModelCreationHandler;
		private ToolState State;
		private ModelHoverEllipse ModelHoverEllipse;

		private Vertex SelectedVertex;
		private Vertex FixedVertex;
		private IEllipse SelectedEllipse;

		public CameraModelCalibrationHandler(ModelCreationHandler modelCreationHandler, Model model, PerspectiveData perspective, IWindow window, double pointGrabRadius, double pointDrawRadius)
		{
			this.ModelCreationHandler = modelCreationHandler;
			this.Model = model;
			this.Perspective = perspective;
			this.Window = window;
			this.PointGrabRadius = pointGrabRadius;
			this.PointDrawRadius = pointDrawRadius;

			this.State = ToolState.CalibrateOriginSelectPoint;
			this.ModelHoverEllipse = new ModelHoverEllipse(Model, Perspective, Window, PointGrabRadius, PointDrawRadius);
			this.ModelHoverEllipse.Active = false;

			this.SelectedEllipse = Window.CreateEllipse(new Vector2(), PointDrawRadius, ApplicationColor.Selected);
			this.SelectedEllipse.Visible = false;

			this.Active = false;
			SetActive(Active);
		}

		private void UpdateOrigin(Vector2 mouseCoord)
		{
			Perspective.Origin = Perspective.MatchScreenWorldPoint(mouseCoord, SelectedVertex.Position);
			ModelCreationHandler.UpdateDisplayedLines();
		}

		private void UpdateScale(Vector2 mouseCoord)
		{
			Vector3 originScale = Perspective.MatchScreenWorldPoints(Perspective.WorldToScreen(FixedVertex.Position), FixedVertex.Position, mouseCoord, SelectedVertex.Position);
			if (originScale.Z > 0)
			{
				Perspective.Scale = originScale.Z;
				Perspective.Origin = new Vector2(originScale.X, originScale.Y);
			}
			ModelCreationHandler.UpdateDisplayedLines();
		}

		public void MouseMove(Vector2 mouseCoord)
		{
			if (Active)
			{
				switch (State)
				{
					case ToolState.CalibrateOriginSelectPoint:
						ModelHoverEllipse.MouseEvent(mouseCoord);
						break;
					case ToolState.CalibrateOriginDraggingPoint:
						UpdateOrigin(mouseCoord);
						break;
					case ToolState.CalibrateScaleSelectPoint:
						ModelHoverEllipse.MouseEvent(mouseCoord);
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

						vertex = ModelCreationHandler.GetVertexUnderMouse(mouseCoord);
						if (vertex != null)
						{
							SelectedVertex = vertex;
							State = ToolState.CalibrateOriginDraggingPoint;
							ModelHoverEllipse.Active = false;
							UpdateOrigin(mouseCoord);
						}
						break;
					case ToolState.CalibrateScaleSelectPoint:
						if (button == MouseButton.Right)
						{
							vertex = ModelCreationHandler.GetVertexUnderMouse(mouseCoord);
							if (vertex != null)
								FixedVertex = vertex;
							SelectedEllipse.Position = Perspective.WorldToScreen(FixedVertex.Position);
						}
						else if (button == MouseButton.Left)
						{
							vertex = ModelCreationHandler.GetVertexUnderMouse(mouseCoord);
							if (vertex != null && vertex != FixedVertex)
							{
								SelectedVertex = vertex;
								State = ToolState.CalibrateScaleDraggingPoint;
								ModelHoverEllipse.Active = false;

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
						ModelHoverEllipse.Active = true;
						ModelHoverEllipse.MouseEvent(mouseCoord);
						break;
					case ToolState.CalibrateScaleDraggingPoint:
						State = ToolState.CalibrateScaleSelectPoint;
						ModelHoverEllipse.Active = true;
						ModelHoverEllipse.MouseEvent(mouseCoord);
						break;
				}
			}
		}

		private void SetActive(bool active)
		{
			ModelCreationHandler.ShowModel(active);

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
					ModelHoverEllipse.Active = true;

					SelectedEllipse.Visible = false;
					break;
				case CameraModelCalibrationTool.CalibrateScale:
					State = ToolState.CalibrateScaleSelectPoint;
					ModelHoverEllipse.Active = true;

					FixedVertex = Model.Vertices[0];
					SelectedEllipse.Visible = true;
					SelectedEllipse.Position = Perspective.WorldToScreen(FixedVertex.Position);
					break;
			}
		}
	}
}
