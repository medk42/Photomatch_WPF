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

namespace Photomatch_ProofOfConcept_WPF
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private ILogger Logger = null;
		private List<Perspective> perspectives = new List<Perspective>();
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
			BitmapImage image = null;

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
					image = new BitmapImage(new Uri(filePath));
				}
				catch (Exception ex)
				{
					if (ex is FileNotFoundException)
						Logger.Log("Load Image", "File not found.", LogType.Warning);
					else if (ex is NotSupportedException)
						Logger.Log("Load Image", "Incorrect or unsupported image format.", LogType.Warning);
					else
						throw ex;
				}
			}

			if (image != null)
			{
				Logger.Log("Load Image", "File loaded successfully.", LogType.Info);

				var perspective = new Perspective(image);
				perspectives.Add(perspective);
				perspective.Apply(MainImage);

				CreateDraggableLine(perspective.LineX1);
				CreateDraggableLine(perspective.LineY1);
				CreateDraggableLine(perspective.LineX2);
				CreateDraggableLine(perspective.LineY2);
			}
		}

		private void CreateDraggableLine(LineGeometry line)
		{
			DraggableLineGeometry draggableLine = new DraggableLineGeometry(line, LineEndRadius);
			geometry.Children.Add(draggableLine.GetGeometry());
			DraggableLineEvents draggableLineEvents = new DraggableLineEvents(draggableLine, LineEndGrabRadius, MainImage);
			mouseListeners.Add(draggableLineEvents);
			scalables.Add(draggableLine);
			scalables.Add(draggableLineEvents);
		}


		// testing  stuff START

		LineGeometry lineGeometry = null;
		GeometryGroup geometry = new GeometryGroup();

		private void MainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
		{
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

		private void MainViewbox_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Logger.Log("Size Change Event", $"{MainImage.RenderSize} => {MainViewbox.RenderSize}", LogType.Info);

			Matrix transform = GetRectToRectTransform(new Rect(MainImage.RenderSize), new Rect(MainImage.TranslatePoint(new Point(0, 0), MainPath), MainViewbox.RenderSize));
			geometry.Transform = new MatrixTransform(transform);

			double scale = MainViewbox.ActualHeight / MainImage.ActualHeight;
			foreach (var scalable in scalables)
			{
				scalable.SetNewScale(scale);
			}

			Logger.Log("Size Change Event", $"{MainImage.RenderSize} => {MainViewbox.RenderSize} with scale {scale}x", LogType.Info);
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

	enum LogType
	{
		Error, Warning, Info
	}

	interface ILogger
	{
		void Log(string title, string message, LogType type);
	}

	class MultiLogger : ILogger
	{
		public List<ILogger> Loggers { get; } = new List<ILogger>();

		public void Log(string title, string message, LogType type)
		{
			foreach (var logger in Loggers)
			{
				logger.Log(title, message, type);
			}
		}
	}

	class WarningErrorGUILogger : ILogger
	{
		public void Log(string title, string message, LogType type)
		{
			switch (type)
			{
				case LogType.Error:
					MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
					break;
				case LogType.Warning:
					MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
					break;
				case LogType.Info:
					break;
				default:
					throw new Exception("Unknown enum type");
			}
		}
	}

	class StatusStripLogger : ILogger
	{
		private readonly TextBlock StatusLabel = null;
		private System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();

		public StatusStripLogger(TextBlock strip)
		{
			StatusLabel = strip;

			timer.Interval = new TimeSpan(hours: 0, minutes: 0, seconds: 5);
			timer.Tick += Timer_Tick;
		}

		public void Log(string title, string message, LogType type)
		{
			switch (type)
			{
				case LogType.Error:
					message = $"ERROR ({title}): {message}";
					break;
				case LogType.Warning:
					message = $"WARNING ({title}): {message}";
					break;
				case LogType.Info:
					message = $"INFO ({title}): {message}";
					break;
				default:
					throw new Exception("Unknown enum type");
			}

			StatusLabel.Text = message;
			timer.Stop();
			timer.Start();
		}

		private void Timer_Tick(object sender, EventArgs e)
		{
			StatusLabel.Text = "";
			timer.Stop();
		}
	}

	class Perspective // should contain image and perspective data
	{
		public BitmapImage Image { get; }

		public LineGeometry LineX1 { get; } = new LineGeometry(new Point(0.52, 0.19), new Point(0.76, 0.28));
		public LineGeometry LineX2 { get; } = new LineGeometry(new Point(0.35, 0.67), new Point(0.46, 0.82));
		public LineGeometry LineY1 { get; } = new LineGeometry(new Point(0.27, 0.31), new Point(0.48, 0.21));
		public LineGeometry LineY2 { get; } = new LineGeometry(new Point(0.55, 0.78), new Point(0.71, 0.68));

		public Perspective(BitmapImage image)
		{
			Image = image;

			ScaleLine(LineX1, image.Width, image.Height);
			ScaleLine(LineX2, image.Width, image.Height);
			ScaleLine(LineY1, image.Width, image.Height);
			ScaleLine(LineY2, image.Width, image.Height);
		}

		private void ScaleLine(LineGeometry line, double xStretch, double yStretch)
		{
			line.StartPoint = new Point(line.StartPoint.X * xStretch, line.StartPoint.Y * yStretch);
			line.EndPoint = new Point(line.EndPoint.X * xStretch, line.EndPoint.Y * yStretch);
		}

		public void Apply(Image imageGUI)
		{
			imageGUI.Source = Image;
		}
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

		public Point StartPoint
		{
			get => line.StartPoint;
			set
			{
				line.StartPoint = value;
				startPointEllipse.Center = value;
			}
		}

		public Point EndPoint
		{
			get => line.EndPoint;
			set
			{
				line.EndPoint = value;
				endPointEllipse.Center = value;
			}
		}

		public DraggableLineGeometry(LineGeometry line, double endRadius)
		{
			this.line = line;
			this.endRadius = endRadius;

			startPointEllipse = new EllipseGeometry(this.line.StartPoint, endRadius, endRadius);
			endPointEllipse = new EllipseGeometry(this.line.EndPoint, endRadius, endRadius);

			geometry.Children.Add(this.line);
			geometry.Children.Add(startPointEllipse);
			geometry.Children.Add(endPointEllipse);
		}

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
}
