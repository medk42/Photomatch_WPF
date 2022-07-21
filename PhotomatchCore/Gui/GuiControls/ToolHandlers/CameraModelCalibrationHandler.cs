using PhotomatchCore.Gui.GuiControls.Helper;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using PhotomatchCore.Logic.Model;
using PhotomatchCore.Logic.Perspective;

namespace PhotomatchCore.Gui.GuiControls.ToolHandlers
{

	/// <summary>
	/// Class for handling camera model calibration.
	/// </summary>
	class CameraModelCalibrationHandler
	{
		/// <summary>
		/// Get/set true if the handler is currently being used and is displayed, false otherwise.
		/// </summary>
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
		private bool Active_;

		/// <summary>
		/// Enum containing the 4 states of the handler (for 2 tools, each with 2 states - calibrate origin/scale and select a point/drag it).
		/// </summary>
		private enum ToolState { CalibrateOriginSelectPoint, CalibrateOriginDraggingPoint, CalibrateScaleSelectPoint, CalibrateScaleDraggingPoint }

		private Model Model;
		private PerspectiveData Perspective;
		private IImageView Window;

		private double PointGrabRadius;
		private double PointDrawRadius;

		private readonly ModelVisualization ModelVisualization;
		private ToolState State;

		private Vertex SelectedVertex;
		private Vertex FixedVertex;
		private IEllipse SelectedEllipse;

		/// <summary>
		/// Parameters modelVisualization, perspective and window need to be from the same ImageWindow.
		/// </summary>
		/// <param name="modelVisualization">Handler displays the model.</param>
		/// <param name="model">Scale tool needs to get a reference to the first vertex (origin).</param>
		/// <param name="perspective">Perspective to change.</param>
		/// <param name="window">To display a point above the reference vertex.</param>
		/// <param name="pointGrabRadius">Screen distance in pixels, from which a vertex/edge can be selected.</param>
		/// <param name="pointDrawRadius">The radius of drawn vertices in pixels on screen.</param>
		public CameraModelCalibrationHandler(ModelVisualization modelVisualization, Model model, PerspectiveData perspective, IImageView window, double pointGrabRadius, double pointDrawRadius)
		{
			ModelVisualization = modelVisualization;
			Model = model;
			Perspective = perspective;
			Window = window;
			PointGrabRadius = pointGrabRadius;
			PointDrawRadius = pointDrawRadius;

			State = ToolState.CalibrateOriginSelectPoint;

			SelectedEllipse = Window.CreateEllipse(new Vector2(), PointDrawRadius, ApplicationColor.Selected);
			SelectedEllipse.Visible = false;

			Active = false;
			SetActive(Active);
		}

		/// <summary>
		/// Move the origin from PerspectiveData so that SelectedVertex is above mouseCoord (if possible).
		/// </summary>
		private void UpdateOrigin(Vector2 mouseCoord)
		{
			Perspective.Origin = Perspective.MatchScreenWorldPoint(mouseCoord, SelectedVertex.Position);
			ModelVisualization.UpdateDisplayedGeometry();
		}

		/// <summary>
		/// Move the origin and change the scale from PerspectiveData so that SelectedVertex is closest 
		/// to mouseCoord and FixedVertex stays at the same position (if possible).
		/// </summary>
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

		/// <summary>
		/// Display a point above vertex under mouse when selecting a point, update origin/scale when dragging a point.
		/// </summary>
		/// <param name="mouseCoord"></param>
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

		/// <summary>
		/// When selecting a point, for calibrate origin select point, for calibrate scale
		/// select point or reference point based on which mouse button was pressed.
		/// </summary>
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

		/// <summary>
		/// If we are dragging a point, stop.
		/// </summary>
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
				SelectedEllipse.Visible = State == ToolState.CalibrateScaleSelectPoint || State == ToolState.CalibrateScaleDraggingPoint;
			else
				SelectedEllipse.Visible = false;
		}

		/// <summary>
		/// Select tool, calibrate origin or scale. 
		/// </summary>
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

		/// <summary>
		/// Update model to model passed by parameter.
		/// </summary>
		public void UpdateModel(Model model)
		{
			Model = model;
			FixedVertex = Model.Vertices[0];
			SelectedEllipse.Position = Perspective.WorldToScreen(FixedVertex.Position);
		}

		/// <summary>
		/// Update positions of all geometry on screen.
		/// </summary>
		public void UpdateDisplayedGeometry()
		{
			if (FixedVertex != null)
				SelectedEllipse.Position = Perspective.WorldToScreen(FixedVertex.Position);
		}
	}
}
