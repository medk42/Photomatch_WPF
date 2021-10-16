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
			DraggablePoints.Points.Add(new ActionPoint(line.Start, (value) => {
				line.Start = value;
				updateValueStart(value);
				UpdateCoordSystemLines();
			}));
			DraggablePoints.Points.Add(new ActionPoint(line.End, (value) => {
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
			DraggablePoints.Points.Add(new ActionPoint(midPicture, (value) => {
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

	public class WpfLine : ILine
	{
		public Line Line { get; }

		public WpfLine(Point Start, Point End, Brush color)
		{
			Line = new Line();
			Line.X1 = Start.X;
			Line.Y1 = Start.Y;
			Line.X2 = End.X;
			Line.Y2 = End.Y;
			Line.Stroke = color;
		}

		public Vector2 Start
		{
			get => new Vector2(Line.X1, Line.Y1);
			set
			{
				Line.X1 = value.X;
				Line.Y1 = value.Y;
			}
		}

		public Vector2 End
		{
			get => new Vector2(Line.X2, Line.Y2);
			set
			{
				Line.X2 = value.X;
				Line.Y2 = value.Y;
			}
		}
	}

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, MasterGUI, IWindow
	{
		private List<IScalable> scalables = new List<IScalable>(); // ????

		private static readonly double LineEndRadius = 4;
		private static readonly double LineEndGrabRadius = 8;
		private static readonly double LineStrokeThickness = 1;

		private MasterControl AppControl;
		private Actions ActionListener;
		private ILogger Logger = null;
		private ImageWindow ImageWindow;

		public MainWindow()
		{
			InitializeComponent();

			AppControl = new MasterControl(this);
			ActionListener = AppControl;

			var multiLogger = new MultiLogger();
			multiLogger.Loggers.Add(new StatusStripLogger(StatusText));
			multiLogger.Loggers.Add(new WarningErrorGUILogger());
			Logger = multiLogger;

			/*MainPath.Data = geometry;
			geometry.FillRule = FillRule.Nonzero;

			MainPath.StrokeThickness = LineStrokeThickness;*/
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
			Point pointATranslated = MainImage.TranslatePoint(Vector2ToPoint(pointA), MyMainWindow);
			Point pointBTranslated = MainImage.TranslatePoint(Vector2ToPoint(pointB), MyMainWindow);
			return (pointATranslated - pointBTranslated).Length;
		}

		private Point Vector2ToPoint(Vector2 vect)
		{
			return new Point(vect.X, vect.Y);
		}

		public ILine CreateLine(Vector2 start, Vector2 end, double endRadius, ApplicationColor color)
		{
			// TODO missing endRadius
			Brush brush;

			switch (color)
			{
				case ApplicationColor.XAxis:
					brush = Brushes.Red;
					break;
				case ApplicationColor.YAxis:
					brush = Brushes.Green;
					break;
				case ApplicationColor.ZAxis:
					brush = Brushes.Blue;
					break;
				case ApplicationColor.Model:
					brush = Brushes.Gray;
					break;
				default:
					throw new ArgumentException("Unknown application color.");
			}

			var wpfLine = new WpfLine(Vector2ToPoint(start), Vector2ToPoint(end), brush);
			MainCanvas.Children.Add(wpfLine.Line);
			return wpfLine;
		}

		private void LoadImage_Click(object sender, RoutedEventArgs e)
		{
			ActionListener.LoadImage_Pressed();
			return;

			
		}

		/*var startGuide = CreateStartGuide(new Point(image.Width / 2, image.Height / 2));
		 * 
		 * private StartGuideGeometry CreateStartGuide(Point point)
		{
			StartGuideGeometry startGuide = new StartGuideGeometry(perspectives[0], MainCanvas);
			StartGuideEvents startGuideEvents = new StartGuideEvents(startGuide, LineEndGrabRadius, MainImage);
			mouseListeners.Add(startGuideEvents);
			scalables.Add(startGuideEvents);

			return startGuide;
		}*/

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

		// testing  stuff START

		LineGeometry lineGeometry = null;
		GeometryGroup geometry = new GeometryGroup();

		private void UpdateGeometryTransform()
		{
			Matrix transform = GetRectToRectTransform(new Rect(MainImage.RenderSize), new Rect(MainImage.TranslatePoint(new Point(0, 0), MainPath), MainViewbox.RenderSize));
			geometry.Transform = new MatrixTransform(transform);
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

	interface IScalable
	{
		void SetNewScale(double scale);
	}

	class DraggableLineGeometry : IScalable
	{
		private GeometryGroup geometry = new GeometryGroup();
		private LineGeometry line;
		private EllipseGeometry startPointEllipse;
		private EllipseGeometry endPointEllipse;
		private double endRadius;
		private List<IChangeListener> changeListeners = new List<IChangeListener>();

		private UpdateValue<Point> updateValueStart;
		private UpdateValue<Point> updateValueEnd;

		public Point StartPoint
		{
			get => line.StartPoint;
			set
			{
				line.StartPoint = value;
				startPointEllipse.Center = value;
				updateValueStart(value);
				NotifyListeners();
			}
		}

		public Point EndPoint
		{
			get => line.EndPoint;
			set
			{
				line.EndPoint = value;
				endPointEllipse.Center = value;
				updateValueEnd(value);
				NotifyListeners();
			}
		}
		
		public void AddListener(IChangeListener listener)
		{
			changeListeners.Add(listener);
		}

		public DraggableLineGeometry(LineGeometry line, double endRadius, UpdateValue<Point> updateValueStart, UpdateValue<Point> updateValueEnd)
		{
			this.line = line;
			this.endRadius = endRadius;

			startPointEllipse = new EllipseGeometry(this.line.StartPoint, endRadius, endRadius);
			endPointEllipse = new EllipseGeometry(this.line.EndPoint, endRadius, endRadius);

			geometry.Children.Add(this.line);
			geometry.Children.Add(startPointEllipse);
			geometry.Children.Add(endPointEllipse);

			this.updateValueStart = updateValueStart;
			this.updateValueEnd = updateValueEnd;
		}

		public DraggableLineGeometry(LineGeometry line, double endRadius) : this(line, endRadius, (val) => { }, (val) => { }) { }

		public Geometry GetGeometry()
		{
			return geometry;
		}

		public void SetNewScale(double scale)
		{
			startPointEllipse.RadiusX = endRadius / scale;
			startPointEllipse.RadiusY = endRadius / scale;
			endPointEllipse.RadiusX = endRadius / scale;
			endPointEllipse.RadiusY = endRadius / scale;
		}

		private void NotifyListeners()
		{
			foreach (var listener in changeListeners)
			{
				listener.NotifyDataChange(this);
			}
		}
	}


	/// <summary>
	/// Bool return value means whether object wants to stop other object from getting the event (if there are two movable objects on top of each other, we only want to move one)
	/// </summary>
	interface IMouseEvents
	{
		bool MouseDown(object sender, MouseButtonEventArgs e);
		bool MouseUp(object sender, MouseButtonEventArgs e);
		bool MouseMove(object sender, MouseEventArgs e);
	}

	class DraggableLineEvents : IMouseEvents, IScalable
	{
		private DraggableLineGeometry line;
		private double endMouseRadiusSquared;
		private double currentScaleSquared = 1;
		private IInputElement positionRelativeToElement;

		private Vector? draggingStartOffset = null;
		private Vector? draggingEndOffset = null;

		public DraggableLineEvents(DraggableLineGeometry line, double endMouseRadius, IInputElement positionRelativeToElement)
		{
			this.line = line;
			this.endMouseRadiusSquared = endMouseRadius * endMouseRadius;
			this.positionRelativeToElement = positionRelativeToElement;
		}

		public bool MouseMove(object sender, MouseEventArgs e)
		{
			if (draggingStartOffset != null)
			{
				Point eventPos = e.GetPosition(positionRelativeToElement);
				line.StartPoint = Point.Add(eventPos, draggingStartOffset.Value);
				return true;
			}

			if (draggingEndOffset != null)
			{
				Point eventPos = e.GetPosition(positionRelativeToElement);
				line.EndPoint = Point.Add(eventPos, draggingEndOffset.Value);
				return true;
			}

			return false;
		}

		public bool MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != System.Windows.Input.MouseButton.Left)
				return false;

			if (draggingStartOffset != null)
			{
				draggingStartOffset = null;
				return true;
			}

			if (draggingEndOffset != null)
			{
				draggingEndOffset = null;
				return true;
			}

			return false;
		}

		public bool MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != System.Windows.Input.MouseButton.Left)
				return false;
			
			Point eventPos = e.GetPosition(positionRelativeToElement);

			// check start point
			Vector startOffset = Point.Subtract(line.StartPoint, eventPos);
			if (startOffset.LengthSquared < endMouseRadiusSquared / currentScaleSquared)
			{
				draggingStartOffset = startOffset;
				return true;
			}


			// check end point
			Vector endOffset = Point.Subtract(line.EndPoint, eventPos);
			if (endOffset.LengthSquared < endMouseRadiusSquared / currentScaleSquared)
			{
				draggingEndOffset = endOffset;
				return true;
			}

			return false;
		}

		public void SetNewScale(double scale)
		{
			currentScaleSquared = scale * scale;
		}
	}

	interface IChangeListener
	{
		void NotifyDataChange(object source);
	}

	class StartGuideGeometry : IChangeListener
	{
		private Line lineX = new Line();
		private Line lineY = new Line();
		private Line lineZ = new Line();

		private Canvas canvas;
		private PerspectiveData perspective;

		public Vector2 StartPoint
		{
			get => perspective.Origin;
			set
			{
				perspective.Origin = value;

				lineX.X1 = perspective.Origin.X;
				lineX.Y1 = perspective.Origin.Y;
				lineY.X1 = perspective.Origin.X;
				lineY.Y1 = perspective.Origin.Y;
				lineZ.X1 = perspective.Origin.X;
				lineZ.Y1 = perspective.Origin.Y;

				SetLines();
			}
		}

		public StartGuideGeometry(PerspectiveData perspective, Canvas canvas)
		{
			this.perspective = perspective;
			this.canvas = canvas;

			StartPoint = perspective.Origin;

			lineX.Stroke = Brushes.Red;
			lineY.Stroke = Brushes.Green;
			lineZ.Stroke = Brushes.Blue;

			canvas.Children.Add(lineX);
			canvas.Children.Add(lineY);
			canvas.Children.Add(lineZ);
		}

		private void SetLines()
		{
			Vector2 dirX = perspective.GetXDirAt(StartPoint);
			Vector2 dirY = perspective.GetYDirAt(StartPoint);
			Vector2 dirZ = perspective.GetZDirAt(StartPoint);

			if (dirX.Valid && dirY.Valid && dirZ.Valid)
			{
				Vector2 endX = GetRayCanvasBorderIntersection(new Ray2D(StartPoint, dirX));
				Vector2 endY = GetRayCanvasBorderIntersection(new Ray2D(StartPoint, dirY));
				Vector2 endZ = GetRayCanvasBorderIntersection(new Ray2D(StartPoint, dirZ));

				lineX.X2 = endX.X;
				lineX.Y2 = endX.Y;
				lineY.X2 = endY.X;
				lineY.Y2 = endY.Y;
				lineZ.X2 = endZ.X;
				lineZ.Y2 = endZ.Y;
			}
			else
			{
				lineX.X2 = lineX.X1 + perspective.Bitmap.Height * 0.1;
				lineX.Y2 = lineX.Y1;
				lineY.X2 = lineY.X1;
				lineY.Y2 = lineY.Y1 + perspective.Bitmap.Height * 0.1;
				lineZ.X2 = lineZ.X1;
				lineZ.Y2 = lineZ.Y1;
			}
		}

		private Vector2 GetRayCanvasBorderIntersection(Ray2D ray)
		{
			Vector2 topLeft = new Vector2(0, 0);
			Vector2 topRight= new Vector2(perspective.Bitmap.Width, 0);
			Vector2 bottomLeft = new Vector2(0, perspective.Bitmap.Height);
			Vector2 bottomRight = new Vector2(perspective.Bitmap.Width, perspective.Bitmap.Height);

			Line2D top = new Line2D(topLeft, topRight);
			Line2D bottom = new Line2D(bottomLeft, bottomRight);
			Line2D left = new Line2D(topLeft, bottomLeft);
			Line2D right = new Line2D(topRight, bottomRight);

			Line2D rayLine = ray.AsLine();
			
			if (ray.Start.Y >= 0)
			{
				var intersection = Intersections2D.GetLineLineIntersection(rayLine, top);
				if (intersection.LineARelative >= 0 && (intersection.LineBRelative >= 0 && intersection.LineBRelative <= 1))
					return intersection.Intersection;
			}

			if (ray.Start.Y < perspective.Bitmap.Height)
			{
				var intersection = Intersections2D.GetLineLineIntersection(rayLine, bottom);
				if (intersection.LineARelative >= 0 && (intersection.LineBRelative >= 0 && intersection.LineBRelative <= 1))
					return intersection.Intersection;
			}
			
			if (ray.Start.X >= 0)
			{
				var intersection = Intersections2D.GetLineLineIntersection(rayLine, left);
				if (intersection.LineARelative >= 0 && (intersection.LineBRelative >= 0 && intersection.LineBRelative <= 1))
					return intersection.Intersection;
			}
			
			if (ray.Start.X < perspective.Bitmap.Width)
			{
				var intersection = Intersections2D.GetLineLineIntersection(rayLine, right);
				if (intersection.LineARelative >= 0 && (intersection.LineBRelative >= 0 && intersection.LineBRelative <= 1))
					return intersection.Intersection;
			}

			return ray.Start;
		}

		public void NotifyDataChange(object source)
		{
			if (source.GetType() == typeof(DraggableLineGeometry))
			{
				SetLines();
			}
		}
	}

	class StartGuideEvents : IMouseEvents, IScalable
	{
		private StartGuideGeometry startGuide;
		private double endMouseRadiusSquared;
		private double currentScaleSquared = 1;
		private IInputElement positionRelativeToElement;

		private Vector2? draggingOffset = null;

		public StartGuideEvents(StartGuideGeometry startGuide, double endMouseRadius, IInputElement positionRelativeToElement)
		{
			this.startGuide = startGuide;
			this.endMouseRadiusSquared = endMouseRadius * endMouseRadius;
			this.positionRelativeToElement = positionRelativeToElement;
		}

		public bool MouseMove(object sender, MouseEventArgs e)
		{
			if (draggingOffset != null)
			{
				Point eventPos = e.GetPosition(positionRelativeToElement);
				startGuide.StartPoint = eventPos.AsVector2() + draggingOffset.Value;
				return true;
			}

			return false;
		}

		public bool MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != System.Windows.Input.MouseButton.Left)
				return false;

			if (draggingOffset != null)
			{
				draggingOffset = null;
				return true;
			}

			return false;
		}

		public bool MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != System.Windows.Input.MouseButton.Left)
				return false;

			Point eventPos = e.GetPosition(positionRelativeToElement);
			Vector2 offset = startGuide.StartPoint - eventPos.AsVector2();
			if (offset.MagnitudeSquared < endMouseRadiusSquared / currentScaleSquared)
			{
				draggingOffset = offset;
				return true;
			}

			return false;
		}

		public void SetNewScale(double scale)
		{
			currentScaleSquared = scale * scale;
		}
	}
}
