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
		private static readonly double StartGuideLineLength = 50;

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

				var startGuide = CreateStartGuide(new Point(image.Width / 2, image.Height / 2));

				var listeners = new List<IChangeListener>();
				listeners.Add(startGuide);
				listeners.Add(new PerspectiveChangeListener(perspective));

				CreateDraggableLine(perspective.LineX1, listeners);
				CreateDraggableLine(perspective.LineY1, listeners);
				CreateDraggableLine(perspective.LineX2, listeners);
				CreateDraggableLine(perspective.LineY2, listeners);

			}
		}

		private void CreateDraggableLine(LineGeometry line, List<IChangeListener> changeListeners)
		{
			DraggableLineGeometry draggableLine = new DraggableLineGeometry(line, LineEndRadius);
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
			StartGuideGeometry startGuide = new StartGuideGeometry(point, perspectives[0], LineEndRadius, StartGuideLineLength);
			geometry.Children.Add(startGuide.GetGeometry());
			StartGuideEvents startGuideEvents = new StartGuideEvents(startGuide, LineEndGrabRadius, MainImage);
			mouseListeners.Add(startGuideEvents);
			scalables.Add(startGuide);
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

	class PerspectiveChangeListener : IChangeListener
	{
		private Perspective perspective;

		public PerspectiveChangeListener(Perspective perspective)
		{
			this.perspective = perspective;
		}

		public void NotifyDataChange(object source)
		{
			perspective.RecalculateProjection();
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

		Camera camera = new Camera();

		public Perspective(BitmapImage image)
		{
			Image = image;

			ScaleLine(LineX1, image.Width, image.Height);
			ScaleLine(LineX2, image.Width, image.Height);
			ScaleLine(LineY1, image.Width, image.Height);
			ScaleLine(LineY2, image.Width, image.Height);

			RecalculateProjection();
		}

		private void ScaleLine(LineGeometry line, double xStretch, double yStretch)
		{
			line.StartPoint = new Point(line.StartPoint.X * xStretch, line.StartPoint.Y * yStretch);
			line.EndPoint = new Point(line.EndPoint.X * xStretch, line.EndPoint.Y * yStretch);
		}

		public void RecalculateProjection()
		{
			Point vanishingPointX = GetLineLineIntersection(LineX1.StartPoint, LineX1.EndPoint, LineX2.StartPoint, LineX2.EndPoint);
			Point vanishingPointY = GetLineLineIntersection(LineY1.StartPoint, LineY1.EndPoint, LineY2.StartPoint, LineY2.EndPoint);
			Point principalPoint = new Point(Image.Width / 2, Image.Height / 2);
			double viewRatio = Image.Height / Image.Width;

			camera.UpdateView(viewRatio, principalPoint, vanishingPointX, vanishingPointY);
		}

		public void Apply(Image imageGUI)
		{
			imageGUI.Source = Image;
		}

		public Vector GetXVector(Point imagePoint)
		{
			Point intersection = GetLineLineIntersection(LineX1.StartPoint, LineX1.EndPoint, LineX2.StartPoint, LineX2.EndPoint);
			Vector ret = Point.Subtract(intersection, imagePoint);
			ret.Normalize();
			return ret;
		}

		public Vector GetYVector(Point imagePoint)
		{
			Point intersection = GetLineLineIntersection(LineY1.StartPoint, LineY1.EndPoint, LineY2.StartPoint, LineY2.EndPoint);
			Vector ret = Point.Subtract(intersection, imagePoint);
			ret.Normalize();
			return ret;
		}

		public Vector GetZVector(Point imagePoint)
		{
			return default;
			/*Vector xVector = GetXVector(imagePoint);
			Vector yVector = GetYVector(imagePoint);

			Vector ret = Vector.CrossProduct(xVector, yVector);
			ret.Normalize();
			return ret;*/
		}

		/// <summary>
		/// Get the point of intersection between the ray and this object using line-line intersection (https://en.wikipedia.org/wiki/Line%E2%80%93line_intersection)
		/// </summary>
		/// <param name="StartLineA"></param>
		/// <param name="EndLineA"></param>
		/// <param name="StartLineB"></param>
		/// <param name="EndLineB"></param>
		/// <returns></returns>
		private Point GetLineLineIntersection(Point StartLineA, Point EndLineA, Point StartLineB, Point EndLineB)
		{
			double x1 = StartLineA.X;
			double y1 = StartLineA.Y;
			double x2 = EndLineA.X;
			double y2 = EndLineA.Y;

			double x3 = StartLineB.X;
			double y3 = StartLineB.Y;
			double x4 = EndLineB.X;
			double y4 = EndLineB.Y;

			double denominator = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);

			//if (denominator == 0) return null;

			double t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denominator;
			double u = ((y1 - y2) * (x1 - x3) - (x1 - x2) * (y1 - y3)) / denominator;

			return new Point(x1 + t * (x2 - x1), y1 + t * (y2 - y1));
		}
	}

	class Camera
	{
		private Matrix3x3 projection = Matrix3x3.CreateUnitMatrix();
		private Matrix3x3 projectionInverse = Matrix3x3.CreateUnitMatrix();

		public Camera() { }

		public void UpdateView(double viewRatio, Point principalPoint, Point firstVanishingPoint, Point secondVanishingPoint)
		{
			double scale = GetInstrinsicParametersScale(principalPoint, viewRatio, firstVanishingPoint, secondVanishingPoint);
			Matrix3x3 intrinsicMatrix = GetIntrinsicParametersMatrix(principalPoint, scale, viewRatio);
			Matrix3x3 intrinsicMatrixInverse = GetInvertedIntrinsicParametersMatrix(principalPoint, scale, viewRatio);
			Matrix3x3 rotationMatrix = GetRotationalMatrix(intrinsicMatrixInverse, firstVanishingPoint, secondVanishingPoint);
			Matrix3x3 rotationMatrixInverse = rotationMatrix.Transposed();
			projection = intrinsicMatrix * rotationMatrix;
			projectionInverse = rotationMatrixInverse * intrinsicMatrixInverse;
		}

		public Vector3 WorldToScreen(Vector3 worldPoint)
		{
			return projection * worldPoint;
		}

		public Vector3 ScreenToWorld(Vector3 screenPoint)
		{
			return projectionInverse * screenPoint;
		}

		public static double GetInstrinsicParametersScale (Point principalPoint, double viewRatio, Point firstVanishingPoint, Point secondVanishingPoint)
		{
			return Math.Sqrt(
				-(principalPoint.X * principalPoint.X)
				+ firstVanishingPoint.X * principalPoint.X
				+ secondVanishingPoint.X * principalPoint.X 
				- firstVanishingPoint.X * secondVanishingPoint.X
				+ (
					-(principalPoint.Y * principalPoint.Y)
					+ firstVanishingPoint.Y * principalPoint.Y
					+ secondVanishingPoint.Y * principalPoint.Y
					- firstVanishingPoint.Y * secondVanishingPoint.Y
				) / (viewRatio * viewRatio));
		}

		public static Matrix3x3 GetIntrinsicParametersMatrix (Point principalPoint, double scale, double viewRatio)
		{
			Matrix3x3 intrinsicMatrix = new Matrix3x3();

			intrinsicMatrix.A00 = scale;
			intrinsicMatrix.A11 = scale * viewRatio;
			intrinsicMatrix.A22 = 1;
			intrinsicMatrix.A02 = principalPoint.X;
			intrinsicMatrix.A12 = principalPoint.Y;

			return intrinsicMatrix;
		}

		public static Matrix3x3 GetInvertedIntrinsicParametersMatrix(Point principalPoint, double scale, double viewRatio)
		{
			Matrix3x3 intrinsicMatrixInv = new Matrix3x3();

			double scaleInv = 1 / scale;
			double viewRationInv = 1 / viewRatio;

			intrinsicMatrixInv.A00 = scaleInv;
			intrinsicMatrixInv.A11 = scaleInv * viewRationInv;
			intrinsicMatrixInv.A22 = 1;
			intrinsicMatrixInv.A02 = -principalPoint.X * scaleInv;
			intrinsicMatrixInv.A12 = -principalPoint.Y * scaleInv * viewRationInv;

			return intrinsicMatrixInv;
		}

		public static Matrix3x3 GetRotationalMatrix (Matrix3x3 invertedIntrinsicMatrix, Point firstVanishingPoint, Point secondVanishingPoint)
		{
			Matrix3x3 rotationMatrix = new Matrix3x3();

			Vector3 firstCol = (invertedIntrinsicMatrix * new Vector3(firstVanishingPoint.X, firstVanishingPoint.Y, 1 )).Normalized();
			Vector3 secondCol = (invertedIntrinsicMatrix * new Vector3(secondVanishingPoint.X, secondVanishingPoint.Y, 1)).Normalized();

			rotationMatrix.A00 = firstCol.X;
			rotationMatrix.A10 = firstCol.Y;
			rotationMatrix.A20 = firstCol.Z;

			rotationMatrix.A01 = secondCol.X;
			rotationMatrix.A11 = secondCol.Y;
			rotationMatrix.A21 = secondCol.Z;

			rotationMatrix.A02 = Math.Sqrt(1 - firstCol.X * firstCol.X - secondCol.X * secondCol.X);
			rotationMatrix.A12 = Math.Sqrt(1 - firstCol.Y * firstCol.Y - secondCol.Y * secondCol.Y);
			rotationMatrix.A22 = Math.Sqrt(1 - firstCol.Z * firstCol.Z - secondCol.Z * secondCol.Z);

			return rotationMatrix;
		}
	}

	struct Matrix3x3
	{
		public double A00 { get; set; }
		public double A01 { get; set; }
		public double A02 { get; set; }
		public double A10 { get; set; }
		public double A11 { get; set; }
		public double A12 { get; set; }
		public double A20 { get; set; }
		public double A21 { get; set; }
		public double A22 { get; set; }

		public static Matrix3x3 CreateUnitMatrix()
		{
			return new Matrix3x3() { A00 = 1, A11 = 1, A22 = 1 };
		}

		public static Vector3 operator *(Matrix3x3 matrix, Vector3 vector)
		{
			Vector3 result = new Vector3();

			result.X = matrix.A00 * vector.X + matrix.A01 * vector.Y + matrix.A02 * vector.Z;
			result.Y = matrix.A10 * vector.X + matrix.A11 * vector.Y + matrix.A12 * vector.Z;
			result.Z = matrix.A20 * vector.X + matrix.A21 * vector.Y + matrix.A22 * vector.Z;

			return result;
		}

		public static Matrix3x3 operator *(Matrix3x3 matrixA, Matrix3x3 matrixB)
		{
			Matrix3x3 result = new Matrix3x3();

			result.A00 = matrixA.A00 * matrixB.A00 + matrixA.A01 * matrixB.A10 + matrixA.A02 * matrixB.A20;
			result.A10 = matrixA.A10 * matrixB.A00 + matrixA.A11 * matrixB.A10 + matrixA.A12 * matrixB.A20;
			result.A20 = matrixA.A20 * matrixB.A00 + matrixA.A21 * matrixB.A10 + matrixA.A22 * matrixB.A20;

			result.A01 = matrixA.A00 * matrixB.A01 + matrixA.A01 * matrixB.A11 + matrixA.A02 * matrixB.A21;
			result.A11 = matrixA.A10 * matrixB.A01 + matrixA.A11 * matrixB.A11 + matrixA.A12 * matrixB.A21;
			result.A21 = matrixA.A20 * matrixB.A01 + matrixA.A21 * matrixB.A11 + matrixA.A22 * matrixB.A21;

			result.A02 = matrixA.A00 * matrixB.A02 + matrixA.A01 * matrixB.A12 + matrixA.A02 * matrixB.A22;
			result.A12 = matrixA.A10 * matrixB.A02 + matrixA.A11 * matrixB.A12 + matrixA.A12 * matrixB.A22;
			result.A22 = matrixA.A20 * matrixB.A02 + matrixA.A21 * matrixB.A12 + matrixA.A22 * matrixB.A22;

			return result;
		}

		public Matrix3x3 Transposed()
		{
			Matrix3x3 result = new Matrix3x3();

			result.A00 = A00;
			result.A01 = A10;
			result.A02 = A20;

			result.A10 = A01;
			result.A11 = A11;
			result.A12 = A21;

			result.A20 = A02;
			result.A21 = A12;
			result.A22 = A22;

			return result;
		}
	}

	struct Vector3
	{
		public Vector3(double X, double Y, double Z)
		{
			this.X = X;
			this.Y = Y;
			this.Z = Z;
		}

		public double X { get; set; }
		public double Y { get; set; }
		public double Z { get; set; }

		public double MagnitudeSquared => X * X + Y * Y + Z * Z;

		public double Magnitude => Math.Sqrt(MagnitudeSquared);

		public Vector3 Normalized()
		{
			double mag = this.Magnitude;
			return new Vector3() { X = this.X / mag, Y = this.Y / mag, Z = this.Z / mag };
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
		private List<IChangeListener> changeListeners = new List<IChangeListener>();

		public Point StartPoint
		{
			get => line.StartPoint;
			set
			{
				line.StartPoint = value;
				startPointEllipse.Center = value;
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
				NotifyListeners();
			}
		}

		public void AddListener(IChangeListener listener)
		{
			changeListeners.Add(listener);
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

	class StartGuideGeometry : IScalable, IChangeListener
	{
		private GeometryGroup geometry = new GeometryGroup();
		private LineGeometry lineX = new LineGeometry();
		private LineGeometry lineY = new LineGeometry();
		private EllipseGeometry startPointEllipse = new EllipseGeometry();
		private double endRadius;
		private double lineLength;
		private double currentScale = 1;
		private Perspective perspective;

		private Vector xDirection;
		private Vector yDirection;

		public Point StartPoint
		{
			get => startPointEllipse.Center;
			set
			{
				startPointEllipse.Center = value;

				lineX.StartPoint = value;
				lineY.StartPoint = value;

				ResetPerspective();
				SetLines();
			}
		}

		public StartGuideGeometry(Point startPoint, Perspective perspective, double endRadius, double lineLength)
		{
			this.perspective = perspective;
			this.endRadius = endRadius;
			this.lineLength = lineLength;

			this.startPointEllipse.RadiusX = endRadius;
			this.startPointEllipse.RadiusY = endRadius;

			StartPoint = startPoint;

			geometry.Children.Add(startPointEllipse);
			geometry.Children.Add(lineX);
			geometry.Children.Add(lineY);
		}

		public Geometry GetGeometry()
		{
			return geometry;
		}

		public void SetNewScale(double scale)
		{
			startPointEllipse.RadiusX = endRadius / scale;
			startPointEllipse.RadiusY = endRadius / scale;

			currentScale = scale;
			SetLines();
		}

		private void ResetPerspective()
		{
			xDirection = perspective.GetXVector(lineX.StartPoint);
			yDirection = perspective.GetYVector(lineY.StartPoint);
		}

		private void SetLines()
		{
			lineX.EndPoint = Point.Add(lineX.StartPoint, Vector.Multiply(lineLength / currentScale, xDirection));
			lineY.EndPoint = Point.Add(lineY.StartPoint, Vector.Multiply(lineLength / currentScale, yDirection));
		}

		public void NotifyDataChange(object source)
		{
			if (source.GetType() == typeof(DraggableLineGeometry))
			{
				ResetPerspective();
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

		private Vector? draggingOffset = null;

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
				startGuide.StartPoint = Point.Add(eventPos, draggingOffset.Value);
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
			Vector offset = Point.Subtract(startGuide.StartPoint, eventPos);
			if (offset.LengthSquared < endMouseRadiusSquared / currentScaleSquared)
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
