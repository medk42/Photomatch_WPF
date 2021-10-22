using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Interop;

using Logging;
using MatrixVector;

using WpfLogging;
using WpfInterfaces;
using WpfGuiElements;
using WpfExtensions;

using GuiInterfaces;
using GuiControls;
using GuiEnums;

namespace Photomatch_ProofOfConcept_WPF
{
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

		private double DpiScale;

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

			SetDpiScale();
		}

		private void SetDpiScale()
		{
			var source = new HwndSource(new HwndSourceParameters());
			Matrix matrix = source.CompositionTarget.TransformToDevice;
			if (matrix.M11 != matrix.M22)
				throw new Exception($"DPI Scale differs for X ({matrix.M11}x) and Y ({matrix.M22}x)");
			DpiScale = matrix.M11;
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
			Point pointATranslated = MainImage.TranslatePoint(pointA.AsPoint(DpiScale), MyMainWindow);
			Point pointBTranslated = MainImage.TranslatePoint(pointB.AsPoint(DpiScale), MyMainWindow);
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

			var wpfLine = new WpfLine(start.AsPoint(DpiScale), end.AsPoint(DpiScale), endRadius, DpiScale);
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

		private GuiEnums.MouseButton? GetMouseButton(System.Windows.Input.MouseButton button)
		{
			if (button == System.Windows.Input.MouseButton.Left)
				return GuiEnums.MouseButton.Left;
			else if (button == System.Windows.Input.MouseButton.Right)
				return GuiEnums.MouseButton.Right;
			else if (button == System.Windows.Input.MouseButton.Middle)
				return GuiEnums.MouseButton.Middle;
			else
				return null;
		}

		private void MainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
		{
			GuiEnums.MouseButton? button = GetMouseButton(e.ChangedButton);
			if (!button.HasValue)
				return;

			Point point = e.GetPosition(MainImage);
			if (point.X < 0 || point.Y < 0 || point.X >= MainImage.ActualWidth || point.Y >= MainImage.ActualHeight)
				return;

			ImageWindow.MouseDown(point.AsVector2(DpiScale), button.Value);

			Logger.Log("Mouse Event", $"Mouse Down at {e.GetPosition(MainImage).X}, {e.GetPosition(MainImage).Y} by {e.ChangedButton}", LogType.Info);
		}

		private void MainCanvas_MouseUp(object sender, MouseButtonEventArgs e)
		{
			GuiEnums.MouseButton? button = GetMouseButton(e.ChangedButton);
			if (!button.HasValue)
				return;

			Point point = e.GetPosition(MainImage);
			if (point.X < 0 || point.Y < 0 || point.X >= MainImage.ActualWidth || point.Y >= MainImage.ActualHeight)
				return;

			ImageWindow.MouseUp(point.AsVector2(DpiScale), button.Value);

			Logger.Log("Mouse Event", $"Mouse Up at {e.GetPosition(MainImage).X}, {e.GetPosition(MainImage).Y} by {e.ChangedButton}", LogType.Info);
		}

		private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
		{
			Point point = e.GetPosition(MainImage);
			if (point.X < 0 || point.Y < 0 || point.X >= MainImage.ActualWidth || point.Y >= MainImage.ActualHeight)
				return;

			ImageWindow.MouseMove(point.AsVector2(DpiScale));

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
}