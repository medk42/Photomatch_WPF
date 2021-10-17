using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Logging;
using WpfLogging;
using MatrixVector;
using Perspective;
using Lines;

namespace Photomatch_ProofOfConcept_WPF
{
	public enum MouseButton { Left, Right, Middle }

	public enum ApplicationColor { XAxis, YAxis, ZAxis, Model }

	public delegate void UpdateValue<T>(T value);

	public interface IPoint
	{
		public Vector2 Position { get; set; }
	}

	public class ActionPoint : IPoint
	{
		private Vector2 position_;
		public Vector2 Position
		{
			get => position_;
			set
			{
				position_ = value;
				UpdateValue(position_);
			}
		}

		private UpdateValue<Vector2> UpdateValue;

		public ActionPoint(Vector2 position, UpdateValue<Vector2> updateValue)
		{
			this.UpdateValue = updateValue;
			this.Position = position;
		}
	}

	public class DraggablePoints
	{
		public List<IPoint> Points { get; } = new List<IPoint>();

		private IPoint CurrentPoint = null;
		private Vector2 DraggingOffset;
		private IWindow Window;
		private double MaxMouseDistance;

		public DraggablePoints(IWindow window, double maxMouseDistance)
		{
			this.Window = window;
			this.MaxMouseDistance = maxMouseDistance;
		}

		public void MouseMove(Vector2 mouseCoord)
		{
			if (CurrentPoint != null)
			{
				CurrentPoint.Position = mouseCoord + DraggingOffset;
			}
		}

		public void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			if (button == MouseButton.Left)
			{
				foreach (IPoint p in Points)
				{
					if (Window.ScreenDistance(mouseCoord, p.Position) < MaxMouseDistance)
					{
						DraggingOffset = p.Position - mouseCoord;
						CurrentPoint = p;
						break;
					}
				}
			}
		}

		public void MouseUp(Vector2 mouseCoord, MouseButton button)
		{
			if (button == MouseButton.Left)
			{
				CurrentPoint = null;
			}
		}
	}

	public interface ILine
	{
		Vector2 Start { get; set; }
		Vector2 End { get; set; }
	}

	public interface MasterGUI : ILogger
	{
		string GetImageFilePath();
		IWindow CreateImageWindow(ImageWindow imageWindow);
	}

	public interface IWindow
	{
		void SetImage(System.Drawing.Bitmap image);
		double ScreenDistance(Vector2 pointA, Vector2 pointB);
		ILine CreateLine(Vector2 start, Vector2 end, double endRadius, ApplicationColor color);
	}

	public interface Actions
	{
		void LoadImage_Pressed();
	}

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

	public class WpfLine : ILine, IScalable
	{
		public LineGeometry Line { get; }
		public EllipseGeometry StartEllipse { get; }
		public EllipseGeometry EndEllipse { get; }

		private double EndRadius;

		public WpfLine(Point Start, Point End, double endRadius)
		{
			Line = new LineGeometry();
			Line.StartPoint = Start;
			Line.EndPoint = End;

			EndRadius = endRadius;

			if (endRadius != 0)
			{
				StartEllipse = new EllipseGeometry(Start, endRadius, endRadius);
				EndEllipse = new EllipseGeometry(End, endRadius, endRadius);
			}
			else
			{
				StartEllipse = null;
				EndEllipse = null;
			}
		}

		public Vector2 Start
		{
			get => Line.StartPoint.AsVector2();
			set
			{
				Line.StartPoint = value.AsPoint();
				if (StartEllipse != null)
					StartEllipse.Center = value.AsPoint();
			}
		}

		public Vector2 End
		{
			get => Line.EndPoint.AsVector2();
			set
			{
				Line.EndPoint = value.AsPoint();
				if (EndEllipse != null)
					EndEllipse.Center = value.AsPoint();
			}
		}

		public void SetNewScale(double scale)
		{
			StartEllipse.RadiusX = EndRadius / scale;
			StartEllipse.RadiusY = EndRadius / scale;
			EndEllipse.RadiusX = EndRadius / scale;
			EndEllipse.RadiusY = EndRadius / scale;
		}
	}

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, MasterGUI, IWindow
	{
		private static readonly double LineStrokeThickness = 2;

		private MasterControl AppControl;
		private Actions ActionListener;
		private ILogger Logger = null;
		private ImageWindow ImageWindow;

		private GeometryGroup XAxisLinesGeometry = new GeometryGroup();
		private GeometryGroup YAxisLinesGeometry = new GeometryGroup();
		private GeometryGroup ZAxisLinesGeometry = new GeometryGroup();
		private GeometryGroup ModelLinesGeometry = new GeometryGroup();

		private List<IScalable> scalables = new List<IScalable>();

		public MainWindow()
		{
			InitializeComponent();

			AppControl = new MasterControl(this);
			ActionListener = AppControl;

			var multiLogger = new MultiLogger();
			multiLogger.Loggers.Add(new StatusStripLogger(StatusText));
			multiLogger.Loggers.Add(new WarningErrorGUILogger());
			Logger = multiLogger;

			SetUpPathGeometry(XAxisLines, XAxisLinesGeometry);
			SetUpPathGeometry(YAxisLines, YAxisLinesGeometry);
			SetUpPathGeometry(ZAxisLines, ZAxisLinesGeometry);
			SetUpPathGeometry(ModelLines, ModelLinesGeometry);
		}

		private void SetUpPathGeometry(System.Windows.Shapes.Path path, GeometryGroup geometry)
		{
			path.Data = geometry;
			path.StrokeThickness = LineStrokeThickness;

			geometry.FillRule = FillRule.Nonzero;
		}

		public void Log(string title, string message, LogType type)
		{
			Logger.Log(title, message, type);
		}

		private string GetFilePath(string filter)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = filter;
			openFileDialog.RestoreDirectory = true;
			if (openFileDialog.ShowDialog() ?? false)
			{
				return openFileDialog.FileName;
			}

			return null;
		}

		public string GetImageFilePath()
		{
			return GetFilePath("Image Files (*.BMP;*.JPG;*.GIF;*.PNG;*.TIFF)|*.BMP;*.JPG;*.GIF;*.PNG;*.TIFF");
		}

		public IWindow CreateImageWindow(ImageWindow imageWindow)
		{
			// TODO this will be used when multiple windows are implemented
			ImageWindow = imageWindow;
			return this;
		}

		public void SetImage(System.Drawing.Bitmap image)
		{
			// TODO id will be used when multiple windows are implemented
			SetBitmapAsImage(image, MainImage);
		}

		private void SetBitmapAsImage(System.Drawing.Bitmap bitmap, Image image)
		{
			var stream = new MemoryStream();
			bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
			stream.Seek(0, SeekOrigin.Begin);

			BitmapImage imageCopy = new BitmapImage();
			imageCopy.BeginInit();
			imageCopy.StreamSource = stream;
			imageCopy.EndInit();

			image.Source = imageCopy;
		}

		public double ScreenDistance(Vector2 pointA, Vector2 pointB)
		{
			// TODO id will be used when multiple windows are implemented
			Point pointATranslated = MainImage.TranslatePoint(pointA.AsPoint(), MyMainWindow);
			Point pointBTranslated = MainImage.TranslatePoint(pointB.AsPoint(), MyMainWindow);
			return (pointATranslated - pointBTranslated).Length;
		}

		public ILine CreateLine(Vector2 start, Vector2 end, double endRadius, ApplicationColor color)
		{
			GeometryGroup geometry;

			switch (color)
			{
				case ApplicationColor.XAxis:
					geometry = XAxisLinesGeometry;
					break;
				case ApplicationColor.YAxis:
					geometry = YAxisLinesGeometry;
					break;
				case ApplicationColor.ZAxis:
					geometry = ZAxisLinesGeometry;
					break;
				case ApplicationColor.Model:
					geometry = ModelLinesGeometry;
					break;
				default:
					throw new ArgumentException("Unknown application color.");
			}

			var wpfLine = new WpfLine(start.AsPoint(), end.AsPoint(), endRadius);
			geometry.Children.Add(wpfLine.Line);
			if (wpfLine.StartEllipse != null && wpfLine.EndEllipse != null)
			{
				geometry.Children.Add(wpfLine.StartEllipse);
				geometry.Children.Add(wpfLine.EndEllipse);
				scalables.Add(wpfLine);
			}

			return wpfLine;
		}

		private void LoadImage_Click(object sender, RoutedEventArgs e) => ActionListener.LoadImage_Pressed();

		private MouseButton? GetMouseButton(System.Windows.Input.MouseButton button)
		{
			if (button == System.Windows.Input.MouseButton.Left)
				return MouseButton.Left;
			else if (button == System.Windows.Input.MouseButton.Right)
				return MouseButton.Right;
			else if (button == System.Windows.Input.MouseButton.Middle)
				return MouseButton.Middle;
			else
				return null;
		}

		private void MainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
		{
			MouseButton? button = GetMouseButton(e.ChangedButton);
			if (!button.HasValue)
				return;

			Point point = e.GetPosition(MainImage);
			if (point.X < 0 || point.Y < 0 || point.X >= MainImage.ActualWidth || point.Y >= MainImage.ActualHeight)
				return;

			ImageWindow.MouseDown(point.AsVector2(), button.Value);

			Logger.Log("Mouse Event", $"Mouse Down at {e.GetPosition(MainImage).X}, {e.GetPosition(MainImage).Y} by {e.ChangedButton}", LogType.Info);
		}

		private void MainCanvas_MouseUp(object sender, MouseButtonEventArgs e)
		{
			MouseButton? button = GetMouseButton(e.ChangedButton);
			if (!button.HasValue)
				return;

			Point point = e.GetPosition(MainImage);
			if (point.X < 0 || point.Y < 0 || point.X >= MainImage.ActualWidth || point.Y >= MainImage.ActualHeight)
				return;

			ImageWindow.MouseUp(point.AsVector2(), button.Value);

			Logger.Log("Mouse Event", $"Mouse Up at {e.GetPosition(MainImage).X}, {e.GetPosition(MainImage).Y} by {e.ChangedButton}", LogType.Info);
		}

		private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
		{
			Point point = e.GetPosition(MainImage);
			if (point.X < 0 || point.Y < 0 || point.X >= MainImage.ActualWidth || point.Y >= MainImage.ActualHeight)
				return;

			ImageWindow.MouseMove(point.AsVector2());

			Point afterTranslate = MainImage.TranslatePoint(point, MyMainWindow);
			Logger.Log("Mouse Event", $"Mouse Move at {e.GetPosition(MainImage).X}, {e.GetPosition(MainImage).Y} (MainCanvas) and at {e.GetPosition(MainImage).X}, {e.GetPosition(MainImage).Y} (MainImage) which is at {afterTranslate.X}, {afterTranslate.Y} (translated to MainWindow)", LogType.Info);
		}

		private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdateGeometryTransform();

			double scale = MainViewbox.ActualHeight / MainImage.ActualHeight;
			Logger.Log($"Size Change Event ({sender.GetType().Name})", $"{MainImage.RenderSize} => {MainViewbox.RenderSize} with scale {scale}x", LogType.Info);
		}
		private void MainViewbox_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdateGeometryTransform();

			double scale = MainViewbox.ActualHeight / MainImage.ActualHeight;
			foreach (var scalable in scalables)
			{
				scalable.SetNewScale(scale);
			}

			Logger.Log($"Size Change Event ({sender.GetType().Name})", $"{MainImage.RenderSize} => {MainViewbox.RenderSize} with scale {scale}x", LogType.Info);
		}

		private void UpdateGeometryTransform()
		{
			Matrix transform = GetRectToRectTransform(new Rect(MainImage.RenderSize), new Rect(MainImage.TranslatePoint(new Point(0, 0), XAxisLines), MainViewbox.RenderSize));
			MatrixTransform matrixTransform = new MatrixTransform(transform);
			XAxisLinesGeometry.Transform = matrixTransform;
			YAxisLinesGeometry.Transform = matrixTransform;
			ZAxisLinesGeometry.Transform = matrixTransform;
			ModelLinesGeometry.Transform = matrixTransform;
		}

		/// <summary>
		/// Source: https://stackoverflow.com/questions/724139/invariant-stroke-thickness-of-path-regardless-of-the-scale
		/// </summary>
		private static Matrix GetRectToRectTransform(Rect from, Rect to)
		{
			Matrix transform = Matrix.Identity;
			transform.Translate(-from.X, -from.Y);
			transform.Scale(to.Width / from.Width, to.Height / from.Height);
			transform.Translate(to.X, to.Y);

			return transform;
		}
	}

	static class PointExtensions
	{
		public static Vector2 AsVector2(this Point p) => new Vector2(p.X, p.Y);
	}

	static class Vector2Extensions
	{
		public static Point AsPoint(this Vector2 v) => new Point(v.X, v.Y);
	}

	interface IScalable
	{
		void SetNewScale(double scale);
	}
}