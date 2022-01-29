using System;
using System.Collections.Generic;
using System.Text;

using Photomatch_ProofOfConcept_WPF.Logic;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls
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
		private DraggablePoints DraggablePoints;

		private PerspectiveData Perspective;
		private IWindow Window;

		private double PointGrabRadius;
		private double PointDrawRadius;

		public CameraCalibrationHandler(PerspectiveData perspective, IWindow window, double pointGrabRadius, double pointDrawRadius)
		{
			this.Perspective = perspective;
			this.Window = window;
			this.PointGrabRadius = pointGrabRadius;
			this.PointDrawRadius = pointDrawRadius;

			this.DraggablePoints = new DraggablePoints(Window, PointGrabRadius);
			Window.DisplayCalibrationAxes(Perspective.CalibrationAxes);
			Window.DisplayInvertedAxes(Perspective.CalibrationAxes, Perspective.InvertedAxes);
			CreateCoordSystemLines();
			CreatePerspectiveLines();

			this.Active = false;
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

		private void CreatePerspectiveLines()
		{
			Tuple<ApplicationColor, ApplicationColor> colors = GetColorsFromCalibrationAxes(Perspective.CalibrationAxes);

			LineA1 = Window.CreateLine(Perspective.LineA1.Start, Perspective.LineA1.End, PointDrawRadius, colors.Item1);
			LineA2 = Window.CreateLine(Perspective.LineA2.Start, Perspective.LineA2.End, PointDrawRadius, colors.Item1);
			LineB1 = Window.CreateLine(Perspective.LineB1.Start, Perspective.LineB1.End, PointDrawRadius, colors.Item2);
			LineB2 = Window.CreateLine(Perspective.LineB2.Start, Perspective.LineB2.End, PointDrawRadius, colors.Item2);

			AddDraggablePointsForPerspectiveLine(LineA1,
				(value) => Perspective.LineA1 = Perspective.LineA1.WithStart(value), () => Perspective.LineA1.Start,
				(value) => Perspective.LineA1 = Perspective.LineA1.WithEnd(value), () => Perspective.LineA1.End);
			AddDraggablePointsForPerspectiveLine(LineA2,
				(value) => Perspective.LineA2 = Perspective.LineA2.WithStart(value), () => Perspective.LineA2.Start,
				(value) => Perspective.LineA2 = Perspective.LineA2.WithEnd(value), () => Perspective.LineA2.End);
			AddDraggablePointsForPerspectiveLine(LineB1,
				(value) => Perspective.LineB1 = Perspective.LineB1.WithStart(value), () => Perspective.LineB1.Start,
				(value) => Perspective.LineB1 = Perspective.LineB1.WithEnd(value), () => Perspective.LineB1.End);
			AddDraggablePointsForPerspectiveLine(LineB2,
				(value) => Perspective.LineB2 = Perspective.LineB2.WithStart(value), () => Perspective.LineB2.Start,
				(value) => Perspective.LineB2 = Perspective.LineB2.WithEnd(value), () => Perspective.LineB2.End);
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

		private void AddDraggablePointsForPerspectiveLine(ILine line, UpdateValue<Vector2> updateValueStart, GetValue<Vector2> getValueStart, UpdateValue<Vector2> updateValueEnd, GetValue<Vector2> getValueEnd)
		{
			DraggablePoints.Points.Add(new ActionPoint(line.Start, (value) =>
			{
				line.Start = value;
				updateValueStart(value);
				UpdateCoordSystemLines();
			}, getValueStart));
			DraggablePoints.Points.Add(new ActionPoint(line.End, (value) =>
			{
				line.End = value;
				updateValueEnd(value);
				UpdateCoordSystemLines();
			}, getValueEnd));
		}

		private void CreateCoordSystemLines()
		{
			var origin = new Vector2();
			LineX = Window.CreateLine(origin, origin, PointDrawRadius, ApplicationColor.XAxis);
			LineY = Window.CreateLine(origin, origin, PointDrawRadius, ApplicationColor.YAxis);
			LineZ = Window.CreateLine(origin, origin, PointDrawRadius, ApplicationColor.ZAxis);

			DraggablePoints.Points.Add(new ActionPoint(Perspective.Origin, (value) =>
			{
				Perspective.Origin = value;
				UpdateCoordSystemLines();
			}, () => Perspective.Origin));

			UpdateCoordSystemLines();
		}

		internal void UpdateDisplayedGeometry()
		{
			Vector2 dirX = Perspective.GetXDirAt(Perspective.Origin);
			Vector2 dirY = Perspective.GetYDirAt(Perspective.Origin);
			Vector2 dirZ = Perspective.GetZDirAt(Perspective.Origin);

			LineX.Start = Perspective.Origin;
			LineY.Start = Perspective.Origin;
			LineZ.Start = Perspective.Origin;

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

			foreach (IPoint point in DraggablePoints.Points)
				((ActionPoint)point).UpdateSelf();
		}

		private void UpdateCoordSystemLines()
		{
			UpdateDisplayedGeometry();
			CoordSystemUpdateEvent?.Invoke();
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

			if (active)
				UpdateCoordSystemLines();
		}

		public void Dispose()
		{
			Perspective = null;
		}

		public void CalibrationAxes_Changed(CalibrationAxes calibrationAxes)
		{
			Perspective.CalibrationAxes = calibrationAxes;
			Window.DisplayCalibrationAxes(Perspective.CalibrationAxes);
			Window.DisplayInvertedAxes(Perspective.CalibrationAxes, Perspective.InvertedAxes);

			UpdateCoordSystemLines();

			Tuple<ApplicationColor, ApplicationColor> colors = GetColorsFromCalibrationAxes(Perspective.CalibrationAxes);
			LineA1.Color = colors.Item1;
			LineA2.Color = colors.Item1;
			LineB1.Color = colors.Item2;
			LineB2.Color = colors.Item2;
		}

		public void InvertedAxes_Changed(InvertedAxes invertedAxes)
		{
			Perspective.InvertedAxes = invertedAxes;
			Window.DisplayInvertedAxes(Perspective.CalibrationAxes, Perspective.InvertedAxes);

			UpdateCoordSystemLines();
		}
	}
}
