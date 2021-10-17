using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using GuiInterfaces;
using Logging;
using GuiPoints;
using Perspective;
using MatrixVector;
using GuiEnums;
using Lines;

namespace GuiControls
{
	public class ImageWindow
	{
		private static readonly double PointGrabRadius = 8;
		private static readonly double PointDrawRadius = 4;

		private MasterGUI Gui;
		private ILogger Logger;
		private IWindow Window { get; }

		private PerspectiveData Perspective;
		private DraggablePoints DraggablePoints;

		private ILine LineX, LineY, LineZ;

		public ImageWindow(System.Drawing.Bitmap image, MasterGUI gui, ILogger logger)
		{
			this.Gui = gui;
			this.Logger = logger;
			this.Window = Gui.CreateImageWindow(this);

			this.Perspective = new PerspectiveData(image);
			this.DraggablePoints = new DraggablePoints(Window, PointGrabRadius);

			Window.SetImage(image);

			CreateCoordSystemLines();
			CreatePerspectiveLines();
		}

		public void MouseMove(Vector2 mouseCoord)
		{
			DraggablePoints.MouseMove(mouseCoord);
		}

		public void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			DraggablePoints.MouseDown(mouseCoord, button);
		}

		public void MouseUp(Vector2 mouseCoord, MouseButton button)
		{
			DraggablePoints.MouseUp(mouseCoord, button);
		}

		private void CreatePerspectiveLines()
		{
			var lineX1 = Window.CreateLine(Perspective.LineX1.Start, Perspective.LineX1.End, PointDrawRadius, ApplicationColor.XAxis);
			var lineX2 = Window.CreateLine(Perspective.LineX2.Start, Perspective.LineX2.End, PointDrawRadius, ApplicationColor.XAxis);
			var lineY1 = Window.CreateLine(Perspective.LineY1.Start, Perspective.LineY1.End, PointDrawRadius, ApplicationColor.YAxis);
			var lineY2 = Window.CreateLine(Perspective.LineY2.Start, Perspective.LineY2.End, PointDrawRadius, ApplicationColor.YAxis);

			AddDraggablePointsForPerspectiveLine(lineX1,
				(value) => Perspective.LineX1 = Perspective.LineX1.WithStart(value),
				(value) => Perspective.LineX1 = Perspective.LineX1.WithEnd(value));
			AddDraggablePointsForPerspectiveLine(lineX2,
				(value) => Perspective.LineX2 = Perspective.LineX2.WithStart(value),
				(value) => Perspective.LineX2 = Perspective.LineX2.WithEnd(value));
			AddDraggablePointsForPerspectiveLine(lineY1,
				(value) => Perspective.LineY1 = Perspective.LineY1.WithStart(value),
				(value) => Perspective.LineY1 = Perspective.LineY1.WithEnd(value));
			AddDraggablePointsForPerspectiveLine(lineY2,
				(value) => Perspective.LineY2 = Perspective.LineY2.WithStart(value),
				(value) => Perspective.LineY2 = Perspective.LineY2.WithEnd(value));
		}

		private void AddDraggablePointsForPerspectiveLine(ILine line, UpdateValue<Vector2> updateValueStart, UpdateValue<Vector2> updateValueEnd)
		{
			DraggablePoints.Points.Add(new ActionPoint(line.Start, (value) =>
			{
				line.Start = value;
				updateValueStart(value);
				UpdateCoordSystemLines();
			}));
			DraggablePoints.Points.Add(new ActionPoint(line.End, (value) =>
			{
				line.End = value;
				updateValueEnd(value);
				UpdateCoordSystemLines();
			}));
		}

		private void CreateCoordSystemLines()
		{
			var origin = new Vector2();
			LineX = Window.CreateLine(origin, origin, PointDrawRadius, ApplicationColor.XAxis);
			LineY = Window.CreateLine(origin, origin, PointDrawRadius, ApplicationColor.YAxis);
			LineZ = Window.CreateLine(origin, origin, PointDrawRadius, ApplicationColor.ZAxis);

			Vector2 midPicture = new Vector2(Perspective.Bitmap.Width / 2, Perspective.Bitmap.Height / 2);
			DraggablePoints.Points.Add(new ActionPoint(midPicture, (value) =>
			{
				Perspective.Origin = value;
				UpdateCoordSystemLines();
			}));

			UpdateCoordSystemLines();
		}

		private void UpdateCoordSystemLines()
		{
			Vector2 dirX = Perspective.GetXDirAt(Perspective.Origin);
			Vector2 dirY = Perspective.GetYDirAt(Perspective.Origin);
			Vector2 dirZ = Perspective.GetZDirAt(Perspective.Origin);

			LineX.Start = Perspective.Origin;
			LineY.Start = Perspective.Origin;
			LineZ.Start = Perspective.Origin;

			if (dirX.Valid && dirY.Valid && dirZ.Valid)
			{
				Vector2 imageSize = new Vector2(Perspective.Bitmap.Width, Perspective.Bitmap.Height);

				Vector2 endX = Intersections2D.GetRayInsideBoxIntersection(new Ray2D(Perspective.Origin, dirX), new Vector2(), imageSize);
				Vector2 endY = Intersections2D.GetRayInsideBoxIntersection(new Ray2D(Perspective.Origin, dirY), new Vector2(), imageSize);
				Vector2 endZ = Intersections2D.GetRayInsideBoxIntersection(new Ray2D(Perspective.Origin, dirZ), new Vector2(), imageSize);

				LineX.End = endX;
				LineY.End = endY;
				LineZ.End = endZ;
			}
			else
			{
				LineX.End = LineX.Start + new Vector2(Perspective.Bitmap.Height * 0.1, 0);
				LineY.End = LineY.Start + new Vector2(0, Perspective.Bitmap.Height * 0.1);
				LineZ.End = LineZ.Start;
			}
		}
	}

	public class MasterControl : Actions
	{
		private MasterGUI Gui;
		private ILogger Logger;
		private List<ImageWindow> Windows;

		public MasterControl(MasterGUI gui)
		{
			this.Gui = gui;
			this.Logger = gui;
			this.Windows = new List<ImageWindow>();
		}

		public void LoadImage_Pressed()
		{
			string filePath = Gui.GetImageFilePath();
			if (filePath == null)
			{
				Logger.Log("Load Image", "No file was selected.", LogType.Info);
				return;
			}

			System.Drawing.Bitmap image = null;
			try
			{
				using (var bitmap = new System.Drawing.Bitmap(filePath))
				{
					image = new System.Drawing.Bitmap(bitmap);
				}
			}
			catch (Exception ex)
			{
				if (ex is FileNotFoundException)
					Logger.Log("Load Image", "File not found.", LogType.Warning);
				else if (ex is ArgumentException)
					Logger.Log("Load Image", "Incorrect or unsupported image format.", LogType.Warning);
				else
					throw ex;
			}

			if (image != null)
			{
				Logger.Log("Load Image", "File loaded successfully.", LogType.Info);
				Windows.Add(new ImageWindow(image, Gui, Logger));
			}
		}
	}
}
