﻿using Microsoft.Win32;
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
			double scale = Camera.GetInstrinsicParametersScale(principalPoint, viewRatio, vanishingPointX, vanishingPointY);
			MatrixDouble intrinsicMatrix = Camera.GetIntrinsicParametersMatrix(principalPoint, scale, viewRatio);
			MatrixDouble intrinsicMatrixInverted = Camera.GetIntrinsicParametersMatrix(principalPoint, scale, viewRatio);
			MatrixDouble rotationMatrix = Camera.GetRotationalMatrix(intrinsicMatrixInverted, vanishingPointX, vanishingPointY);
			MatrixDouble rotationMatrixInverted = rotationMatrix.Transposed();
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
		private MatrixDouble projection = MatrixDouble.CreateUnitMatrix(3, 3);
		private MatrixDouble intrinsicMatrix = MatrixDouble.CreateUnitMatrix(3, 3);
		private MatrixDouble rotationMatrix = MatrixDouble.CreateUnitMatrix(3, 3);

		private MatrixDouble projectionInverse = MatrixDouble.CreateUnitMatrix(3, 3);
		private MatrixDouble intrinsicMatrixInverse = MatrixDouble.CreateUnitMatrix(3, 3);
		private MatrixDouble rotationMatrixInverse = MatrixDouble.CreateUnitMatrix(3, 3);

		private VectorDouble tempVector = new VectorDouble(3);

		public Camera() { }

		public void UpdateView(double viewRatio, Point principalPoint, Point vanishingPointX, Point vanishingPointY)
		{
			double scale = GetInstrinsicParametersScale(principalPoint, viewRatio, vanishingPointX, vanishingPointY);
			GetIntrinsicParametersMatrix(principalPoint, scale, viewRatio, intrinsicMatrix);
			GetIntrinsicParametersMatrix(principalPoint, scale, viewRatio, intrinsicMatrixInverse);
			GetRotationalMatrix(intrinsicMatrixInverse, vanishingPointX, vanishingPointY, rotationMatrix);
			rotationMatrix.Transposed(rotationMatrixInverse);
			MatrixDouble.MultiplyWithResult(intrinsicMatrix, rotationMatrix, projection);
			MatrixDouble.MultiplyWithResult(rotationMatrixInverse, intrinsicMatrixInverse, projectionInverse);
		}

		public VectorDouble WorldToScreen(VectorDouble point)
		{

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

		public static MatrixDouble GetIntrinsicParametersMatrix (Point principalPoint, double scale, double viewRatio, MatrixDouble? intrinsicMatrixPossible = null)
		{
			if (!intrinsicMatrixPossible.HasValue)
				intrinsicMatrixPossible = new MatrixDouble(3, 3);
			MatrixDouble intrinsicMatrix = intrinsicMatrixPossible.Value;
			intrinsicMatrix[0, 0] = scale;
			intrinsicMatrix[1, 1] = scale * viewRatio;
			intrinsicMatrix[2, 2] = 1;
			intrinsicMatrix[0, 2] = principalPoint.X;
			intrinsicMatrix[1, 2] = principalPoint.Y;

			return intrinsicMatrix;
		}

		public static MatrixDouble GetInvertedIntrinsicParametersMatrix(Point principalPoint, double scale, double viewRatio, MatrixDouble? intrinsicMatrixInvPossible = null)
		{
			if (!intrinsicMatrixInvPossible.HasValue)
				intrinsicMatrixInvPossible = new MatrixDouble(3, 3);
			MatrixDouble intrinsicMatrixInv = intrinsicMatrixInvPossible.Value;
			double scaleInv = 1 / scale;
			double viewRationInv = 1 / viewRatio;
			intrinsicMatrixInv[0, 0] = scaleInv;
			intrinsicMatrixInv[1, 1] = scaleInv * viewRationInv;
			intrinsicMatrixInv[2, 2] = 1;
			intrinsicMatrixInv[0, 2] = -principalPoint.X * scaleInv;
			intrinsicMatrixInv[1, 2] = -principalPoint.Y * scaleInv * viewRationInv;

			return intrinsicMatrixInv;
		}

		public static MatrixDouble GetRotationalMatrix (MatrixDouble invertedIntrinsicMatrix, Point firstVanishingPoint, Point secondVanishingPoint, MatrixDouble? rotationMatrixPossible = null)
		{
			if (!rotationMatrixPossible.HasValue)
				rotationMatrixPossible = new MatrixDouble(3, 3);

			MatrixDouble rotationMatrix = rotationMatrixPossible.Value;
			VectorDouble firstCol = (invertedIntrinsicMatrix * new VectorDouble(new double[] { firstVanishingPoint.X, firstVanishingPoint.Y, 1 })).Normalize();
			VectorDouble secondCol = (invertedIntrinsicMatrix * new VectorDouble(new double[] { secondVanishingPoint.X, secondVanishingPoint.Y, 1 })).Normalize();

			rotationMatrix[0, 0] = firstCol[0];
			rotationMatrix[1, 0] = firstCol[1];
			rotationMatrix[2, 0] = firstCol[2];

			rotationMatrix[0, 1] = secondCol[0];
			rotationMatrix[1, 1] = secondCol[1];
			rotationMatrix[2, 1] = secondCol[2];

			rotationMatrix[0, 2] = Math.Sqrt(1 - firstCol[0] * firstCol[0] - secondCol[0] * secondCol[0]);
			rotationMatrix[1, 2] = Math.Sqrt(1 - firstCol[1] * firstCol[1] - secondCol[1] * secondCol[1]);
			rotationMatrix[2, 2] = Math.Sqrt(1 - firstCol[2] * firstCol[2] - secondCol[2] * secondCol[2]);

			return rotationMatrix;
		}
	}

	struct MatrixDouble
	{
		private double[,] data;

		public double this[int row, int column]
		{
			get => data[row, column];
			set => data[row, column] = value;
		}

		public int Rows => data.GetLength(0);

		public int Columns => data.GetLength(1);

		public MatrixDouble(int rows, int columns)
		{
			data = new double[rows, columns];
		}

		public static MatrixDouble CreateUnitMatrix(int rows, int columns)
		{
			MatrixDouble unitMatrix = new MatrixDouble(rows, columns);
			for (int row = 0; row < rows; row++)
			{
				for (int col = 0; col < columns; col++)
				{
					unitMatrix[row, col] = (row == col) ? 1 : 0;
				}
			}

			return unitMatrix;
		}

		public VectorDouble this[int row] // TODO delete?
		{
			set
			{
				if (value.Length != Columns)
					throw new ArgumentException("New row needs to have the same length.");
				for (int i = 0; i < value.Length; i++)
					this[row, i] = value[i];
			}
		}

		public static VectorDouble operator *(MatrixDouble matrix, VectorDouble vector)
		{
			VectorDouble result = new VectorDouble(matrix.Rows);
			MultiplyWithResult(matrix, vector, result);
			return result;
		}

		public static void MultiplyWithResult(MatrixDouble matrix, VectorDouble vector, VectorDouble storeResult)
		{
			if (matrix.Columns != vector.Length || matrix.Rows != storeResult.Length)
				throw new ArgumentException("matrix-vector dimension mismatch");

			for (int row = 0; row < matrix.Rows; row++)
			{
				for (int col = 0; col < matrix.Columns; col++)
				{
					storeResult[row] += matrix[row, col] * vector[col];
				}
			}
		}

		public static void MultiplyWithResult(MatrixDouble matrixA, MatrixDouble matrixB, MatrixDouble storeResult)
		{
			if (matrixA.Columns != matrixB.Rows || storeResult.Rows != matrixA.Rows || matrixB.Columns != storeResult.Columns)
				throw new ArgumentException("Incorrect dimensions of input matrices");

			for (int row = 0; row < storeResult.Rows; row++)
			{
				for (int col = 0; col < storeResult.Columns; col++)
				{
					storeResult[row, col] = 0;
					for (int i = 0; i < matrixA.Columns; i++)
					{
						storeResult[row, col] += matrixA[row, i] * matrixB[i, col];
					}
				}
			}
		}

		public MatrixDouble Transposed(MatrixDouble? transposedMatrixPossible = null)
		{
			if (!transposedMatrixPossible.HasValue)
				transposedMatrixPossible = new MatrixDouble(this.Columns, this.Rows);
			else if (transposedMatrixPossible.Value.Rows != this.Columns || transposedMatrixPossible.Value.Columns != this.Rows)
				throw new ArgumentException("Existing matrix has wrong dimensions!");

			MatrixDouble transposedMatrix = transposedMatrixPossible.Value;
			for (int row = 0; row < this.Rows; row++)
			{
				for (int col = 0; col < this.Columns; col++)
				{
					transposedMatrix[col, row] = data[row, col];
				}
			}

			return transposedMatrix;
		}
	}

	struct VectorDouble // TODO delete?
	{
		private double[] data;

		public double this[int index]
		{
			get => data[index];
			set => data[index] = value;
		}

		public int Length => data.Length;

		public double MagnitudeSquared
		{
			get
			{
				double squaredSum = 0;
				for (int i = 0; i < Length; i++)
					squaredSum += data[i] * data[i];
				return squaredSum;
			}
		}

		public double Magnitude => Math.Sqrt(MagnitudeSquared);

		public VectorDouble(int length)
		{
			data = new double[length];
		}

		public VectorDouble(double[] data)
		{
			this.data = data;
		}

		public VectorDouble Normalize()
		{
			double mag = this.Magnitude;

			for (int i = 0; i < this.Length; i++)
				data[i] /= mag;

			return this;
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
