using System;
using System.Collections.Generic;
using System.Text;
using PhotomatchCore.Gui.GuiControls.Helper;
using PhotomatchCore.Gui.GuiControls.ToolHandlers.Helper;
using PhotomatchCore.Logic.Perspective;
using PhotomatchCore.Utilities;

namespace PhotomatchCore.Gui.GuiControls.ToolHandlers
{
	class CameraCalibrationHandler
	{
		public delegate void CoordSystemUpdateEventHandler();
		public CoordSystemUpdateEventHandler CoordSystemUpdateEvent;

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

		private ILine LineA1, LineA2, LineB1, LineB2;
		private ILine LineX, LineY, LineZ;
		private IEllipse Origin;
		private DraggablePoints DraggablePoints;

		private PerspectiveData Perspective;
		private IWindow Window;

		private double PointGrabRadius;
		private double PointDrawRadius;

		public CameraCalibrationHandler(PerspectiveData perspective, IWindow window, double pointGrabRadius, double pointDrawRadius)
		{
			Perspective = perspective;
			Window = window;
			PointGrabRadius = pointGrabRadius;
			PointDrawRadius = pointDrawRadius;

			var origin = new Vector2();
			LineA1 = Window.CreateLine(origin, origin, PointDrawRadius, ApplicationColor.XAxis);
			LineA2 = Window.CreateLine(origin, origin, PointDrawRadius, ApplicationColor.XAxis);
			LineB1 = Window.CreateLine(origin, origin, PointDrawRadius, ApplicationColor.YAxis);
			LineB2 = Window.CreateLine(origin, origin, PointDrawRadius, ApplicationColor.YAxis);
			LineX = Window.CreateLine(origin, origin, 0, ApplicationColor.XAxis);
			LineY = Window.CreateLine(origin, origin, 0, ApplicationColor.YAxis);
			LineZ = Window.CreateLine(origin, origin, 0, ApplicationColor.ZAxis);
			Origin = Window.CreateEllipse(origin, PointDrawRadius, ApplicationColor.Vertex);

			DraggablePoints = new DraggablePoints(Window, PointGrabRadius);
			DraggablePoints.Points.Add(new ActionPoint(origin, (value) => Perspective.LineA1 = Perspective.LineA1.WithStart(value), () => Perspective.LineA1.Start));
			DraggablePoints.Points.Add(new ActionPoint(origin, (value) => Perspective.LineA1 = Perspective.LineA1.WithEnd(value), () => Perspective.LineA1.End));
			DraggablePoints.Points.Add(new ActionPoint(origin, (value) => Perspective.LineA2 = Perspective.LineA2.WithStart(value), () => Perspective.LineA2.Start));
			DraggablePoints.Points.Add(new ActionPoint(origin, (value) => Perspective.LineA2 = Perspective.LineA2.WithEnd(value), () => Perspective.LineA2.End));
			DraggablePoints.Points.Add(new ActionPoint(origin, (value) => Perspective.LineB1 = Perspective.LineB1.WithStart(value), () => Perspective.LineB1.Start));
			DraggablePoints.Points.Add(new ActionPoint(origin, (value) => Perspective.LineB1 = Perspective.LineB1.WithEnd(value), () => Perspective.LineB1.End));
			DraggablePoints.Points.Add(new ActionPoint(origin, (value) => Perspective.LineB2 = Perspective.LineB2.WithStart(value), () => Perspective.LineB2.Start));
			DraggablePoints.Points.Add(new ActionPoint(origin, (value) => Perspective.LineB2 = Perspective.LineB2.WithEnd(value), () => Perspective.LineB2.End));
			DraggablePoints.Points.Add(new ActionPoint(origin, (value) => Perspective.Origin = value, () => Perspective.Origin));

			Perspective.PerspectiveChangedEvent += UpdateDisplayedGeometry;
			UpdateDisplayedGeometry();

			Active = false;
			SetActive(Active);
		}

		public void MouseMove(Vector2 mouseCoord)
		{
			if (Active)
				DraggablePoints.MouseMove(mouseCoord);
		}

		public void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			if (Active)
				DraggablePoints.MouseDown(mouseCoord, button);
		}

		public void MouseUp(Vector2 mouseCoord, MouseButton button)
		{
			if (Active)
				DraggablePoints.MouseUp(mouseCoord, button);
		}

		public void UpdateDisplayedGeometry()
		{
			LineA1.Start = Perspective.LineA1.Start;
			LineA1.End = Perspective.LineA1.End;
			LineA2.Start = Perspective.LineA2.Start;
			LineA2.End = Perspective.LineA2.End;
			LineB1.Start = Perspective.LineB1.Start;
			LineB1.End = Perspective.LineB1.End;
			LineB2.Start = Perspective.LineB2.Start;
			LineB2.End = Perspective.LineB2.End;

			UpdateCalibrationAxesColors();

			if (Perspective.Scale > 0)
			{
				LineX.Visible = Active;
				LineY.Visible = Active;
				LineZ.Visible = Active;
				Origin.Visible = Active;

				Vector2 dirX = Perspective.GetXDirAt(Perspective.Origin);
				Vector2 dirY = Perspective.GetYDirAt(Perspective.Origin);
				Vector2 dirZ = Perspective.GetZDirAt(Perspective.Origin);

				LineX.Start = Perspective.Origin;
				LineY.Start = Perspective.Origin;
				LineZ.Start = Perspective.Origin;
				Origin.Position = Perspective.Origin;

				if (dirX.Valid && dirY.Valid && dirZ.Valid)
				{
					Vector2 imageSize = new Vector2(Perspective.Image.Width, Perspective.Image.Height);

					Vector2 endX = Intersections2D.GetRayInsideBoxIntersection(new Ray2D(Perspective.Origin, dirX), new Vector2(), imageSize);
					Vector2 endY = Intersections2D.GetRayInsideBoxIntersection(new Ray2D(Perspective.Origin, dirY), new Vector2(), imageSize);
					Vector2 endZ = Intersections2D.GetRayInsideBoxIntersection(new Ray2D(Perspective.Origin, dirZ), new Vector2(), imageSize);

					LineX.End = endX.Valid ? endX : Perspective.Origin;
					LineY.End = endY.Valid ? endY : Perspective.Origin;
					LineZ.End = endZ.Valid ? endZ : Perspective.Origin;
				}
				else
				{
					LineX.End = LineX.Start + new Vector2(Perspective.Image.Height * 0.1, 0);
					LineY.End = LineY.Start + new Vector2(0, Perspective.Image.Height * 0.1);
					LineZ.End = LineZ.Start;
				}
			}
			else
			{
				LineX.Visible = false;
				LineY.Visible = false;
				LineZ.Visible = false;
				Origin.Visible = false;
			}

			foreach (IPoint point in DraggablePoints.Points)
				((ActionPoint)point).UpdateSelf();

			Window.DisplayInvertedAxes(Perspective.CalibrationAxes, Perspective.InvertedAxes);
			Window.DisplayCalibrationAxes(Perspective.CalibrationAxes);

			CoordSystemUpdateEvent?.Invoke();
		}
		private void UpdateCalibrationAxesColors()
		{
			Tuple<ApplicationColor, ApplicationColor> colors = GetColorsFromCalibrationAxes(Perspective.CalibrationAxes);
			if (LineA1 != null)
			{
				LineA1.Color = colors.Item1;
				LineA2.Color = colors.Item1;
				LineB1.Color = colors.Item2;
				LineB2.Color = colors.Item2;
			}
		}

		private Tuple<ApplicationColor, ApplicationColor> GetColorsFromCalibrationAxes(CalibrationAxes axes)
		{
			ApplicationColor colorA, colorB;

			switch (axes)
			{
				case CalibrationAxes.XY:
					colorA = ApplicationColor.XAxis;
					colorB = ApplicationColor.YAxis;
					break;
				case CalibrationAxes.YX:
					colorA = ApplicationColor.YAxis;
					colorB = ApplicationColor.XAxis;
					break;
				case CalibrationAxes.XZ:
					colorA = ApplicationColor.XAxis;
					colorB = ApplicationColor.ZAxis;
					break;
				case CalibrationAxes.ZX:
					colorA = ApplicationColor.ZAxis;
					colorB = ApplicationColor.XAxis;
					break;
				case CalibrationAxes.YZ:
					colorA = ApplicationColor.YAxis;
					colorB = ApplicationColor.ZAxis;
					break;
				case CalibrationAxes.ZY:
					colorA = ApplicationColor.ZAxis;
					colorB = ApplicationColor.YAxis;
					break;
				default:
					throw new Exception("Unexpected switch case.");
			}

			return new Tuple<ApplicationColor, ApplicationColor>(colorA, colorB);
		}

		private void SetActive(bool active)
		{
			LineA1.Visible = active;
			LineA2.Visible = active;
			LineB1.Visible = active;
			LineB2.Visible = active;

			LineX.Visible = active;
			LineY.Visible = active;
			LineZ.Visible = active;
			Origin.Visible = active;
		}

		public void Dispose()
		{
			Perspective = null;
		}

		public void CalibrationAxes_Changed(CalibrationAxes calibrationAxes) => Perspective.CalibrationAxes = calibrationAxes;
		public void InvertedAxes_Changed(InvertedAxes invertedAxes) => Perspective.InvertedAxes = invertedAxes;
	}
}
