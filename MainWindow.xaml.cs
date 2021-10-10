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
using GuiLogging;
using MatrixVector;
using Perspective;
using Lines;

namespace Photomatch_ProofOfConcept_WPF
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private ILogger Logger = null;
		private List<PerspectiveData> perspectives = new List<PerspectiveData>();
		private List<IMouseEvents> mouseListeners = new List<IMouseEvents>();
		private List<IScalable> scalables = new List<IScalable>();

		private static readonly double LineEndRadius = 4;
		private static readonly double LineEndGrabRadius = 8;
		private static readonly double LineStrokeThickness = 1;

		public MainWindow()
		{
			InitializeComponent();

			var multiLogger = new MultiLogger();
			multiLogger.Loggers.Add(new StatusStripLogger(StatusText));
			multiLogger.Loggers.Add(new WarningErrorGUILogger());
			Logger = multiLogger;

			MainPath.Data = geometry;
			geometry.FillRule = FillRule.Nonzero;

			MainPath.StrokeThickness = LineStrokeThickness;
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			string filePath = null;

			System.Drawing.Bitmap image = null;

			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Image Files (*.BMP;*.JPG;*.GIF;*.PNG;*.TIFF)|*.BMP;*.JPG;*.GIF;*.PNG;*.TIFF";
			openFileDialog.RestoreDirectory = true;
			if (openFileDialog.ShowDialog() ?? false)
			{
				filePath = openFileDialog.FileName;
			}

			if (filePath == null) // user closed dialog
			{
				Logger.Log("Load Image", "User closed dialog before selecting file.", LogType.Info);
			}
			else
			{
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
			}

			if (image != null)
			{
				Logger.Log("Load Image", "File loaded successfully.", LogType.Info);

				var perspective = new PerspectiveData(image);
				perspectives.Add(perspective);
				SetBitmapAsImage(image, MainImage);

				var startGuide = CreateStartGuide(new Point(image.Width / 2, image.Height / 2));

				var listeners = new List<IChangeListener>();
				listeners.Add(startGuide);

				CreateDraggableLine(Line2dToLineGeometry(perspective.LineX1), listeners,
									(value) => perspective.LineX1 = perspective.LineX1.WithStart(value.AsVector2()),
									(value) => perspective.LineX1 = perspective.LineX1.WithEnd(value.AsVector2())
				);
				CreateDraggableLine(Line2dToLineGeometry(perspective.LineX2), listeners,
									(value) => perspective.LineX2 = perspective.LineX2.WithStart(value.AsVector2()),
									(value) => perspective.LineX2 = perspective.LineX2.WithEnd(value.AsVector2())
				);
				CreateDraggableLine(Line2dToLineGeometry(perspective.LineY1), listeners,
									(value) => perspective.LineY1 = perspective.LineY1.WithStart(value.AsVector2()),
									(value) => perspective.LineY1 = perspective.LineY1.WithEnd(value.AsVector2())
				);
				CreateDraggableLine(Line2dToLineGeometry(perspective.LineY2), listeners,
									(value) => perspective.LineY2 = perspective.LineY2.WithStart(value.AsVector2()),
									(value) => perspective.LineY2 = perspective.LineY2.WithEnd(value.AsVector2())
				);
			}
		}

		private LineGeometry Line2dToLineGeometry(Line2D line)
		{
			return new LineGeometry(new Point(line.Start.X, line.Start.Y), new Point(line.End.X, line.End.Y));
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

		private void CreateDraggableLine(LineGeometry line, List<IChangeListener> changeListeners, UpdateValue<Point> updateValueStart, UpdateValue<Point> updateValueEnd)
		{
			DraggableLineGeometry draggableLine = new DraggableLineGeometry(line, LineEndRadius, updateValueStart, updateValueEnd);
			geometry.Children.Add(draggableLine.GetGeometry());
			DraggableLineEvents draggableLineEvents = new DraggableLineEvents(draggableLine, LineEndGrabRadius, MainImage);
			mouseListeners.Add(draggableLineEvents);
			scalables.Add(draggableLine);
			scalables.Add(draggableLineEvents);

			foreach (var listener in changeListeners)
			{
				draggableLine.AddListener(listener);
			}
		}

		private StartGuideGeometry CreateStartGuide(Point point)
		{
			StartGuideGeometry startGuide = new StartGuideGeometry(perspectives[0], MainCanvas);
			StartGuideEvents startGuideEvents = new StartGuideEvents(startGuide, LineEndGrabRadius, MainImage);
			mouseListeners.Add(startGuideEvents);
			scalables.Add(startGuideEvents);

			return startGuide;
		}


		// testing  stuff START

		LineGeometry lineGeometry = null;
		GeometryGroup geometry = new GeometryGroup();

		private void MainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
		{
			Point point = e.GetPosition(MainImage);
			if (point.X < 0 || point.Y < 0 || point.X >= MainImage.ActualWidth || point.Y >= MainImage.ActualHeight)
				return;

			foreach (var listener in mouseListeners)
			{
				if (listener.MouseDown(sender, e))
					break;
			}

			/*if (e.ChangedButton == MouseButton.Left)
			{
				Point eventPos = e.GetPosition(MainImage);

				lineGeometry = new LineGeometry(eventPos, eventPos);
				geometry.Children.Add(lineGeometry);
			}*/

			Logger.Log("Mouse Event", $"Mouse Down at {e.GetPosition(MainImage).X}, {e.GetPosition(MainImage).Y} by {e.ChangedButton}", LogType.Info);
		}

		private void MainCanvas_MouseUp(object sender, MouseButtonEventArgs e)
		{
			Point point = e.GetPosition(MainImage);
			if (point.X < 0 || point.Y < 0 || point.X >= MainImage.ActualWidth || point.Y >= MainImage.ActualHeight)
				return;

			foreach (var listener in mouseListeners)
			{
				if (listener.MouseUp(sender, e))
					break;
			}

			/*if (e.ChangedButton == MouseButton.Left && lineGeometry != null)
			{
				Point eventPos = e.GetPosition(MainImage);

				lineGeometry.EndPoint = eventPos;
				lineGeometry = null;
			}*/

			Logger.Log("Mouse Event", $"Mouse Up at {e.GetPosition(MainImage).X}, {e.GetPosition(MainImage).Y} by {e.ChangedButton}", LogType.Info);
		}

		private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
		{
			Point point = e.GetPosition(MainImage);
			if (point.X < 0 || point.Y < 0 || point.X >= MainImage.ActualWidth || point.Y >= MainImage.ActualHeight)
				return;

			foreach (var listener in mouseListeners)
			{
				if (listener.MouseMove(sender, e))
					break;
			}

			/*if (lineGeometry != null)
			{
				Point eventPos = e.GetPosition(MainImage);

				lineGeometry.EndPoint = eventPos;
			}*/

			Logger.Log("Mouse Event", $"Mouse Move at {e.GetPosition(MainImage).X}, {e.GetPosition(MainImage).Y} (MainCanvas) and at {e.GetPosition(MainImage).X}, {e.GetPosition(MainImage).Y} (MainImage)", LogType.Info);
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

	delegate void UpdateValue<T>(T value);

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
			if (e.ChangedButton != MouseButton.Left)
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
			if (e.ChangedButton != MouseButton.Left)
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
			if (e.ChangedButton != MouseButton.Left)
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
			if (e.ChangedButton != MouseButton.Left)
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
