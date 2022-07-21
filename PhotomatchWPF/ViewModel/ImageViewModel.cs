using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PhotomatchCore.Gui;
using PhotomatchCore.Gui.GuiControls;
using PhotomatchCore;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using PhotomatchCore.Utilities;
using PhotomatchCore.Logic.Perspective;
using PhotomatchWPF.Helper;
using PhotomatchWPF.ViewModel.Helper;

namespace PhotomatchWPF.ViewModel
{

	/// <summary>
	/// Represents the window with an image at the View layer.
	/// </summary>
	public class ImageViewModel : BaseViewModel, IImageView, IMouseHandler, IKeyboardHandler
	{
		private static readonly double DefaultLineStrokeThickness = 2;
		private static readonly double ZoomAmount = 1.002;

		public ICommand CloseCommand { get; }

		private BitmapImage ImageSource_;
		public BitmapImage ImageSource
		{
			get => ImageSource_;
			private set
			{
				ImageSource_ = value;
				OnPropertyChanged(nameof(ImageSource));
			}
		}

		// geometry inside these objects will be drawn on screen
		public GeometryGroup XAxisLinesGeometry { get; } = new GeometryGroup();
		public GeometryGroup YAxisLinesGeometry { get; } = new GeometryGroup();
		public GeometryGroup ZAxisLinesGeometry { get; } = new GeometryGroup();
		public GeometryGroup ModelLinesGeometry { get; } = new GeometryGroup();
		public GeometryGroup SelectedLinesGeometry { get; } = new GeometryGroup();
		public GeometryGroup FaceLinesGeometry { get; } = new GeometryGroup();
		public GeometryGroup HighlightLinesGeometry { get; } = new GeometryGroup();
		public GeometryGroup VertexLinesGeometry { get; } = new GeometryGroup();
		public GeometryGroup MidpointLinesGeometry { get; } = new GeometryGroup();
		public GeometryGroup EdgepointLinesGeometry { get; } = new GeometryGroup();
		public GeometryGroup InvalidLinesGeometry { get; } = new GeometryGroup();
		public GeometryGroup NormalLinesGeometry { get; } = new GeometryGroup();
		public GeometryGroup NormalInsideLinesGeometry { get; } = new GeometryGroup();
		public GeometryGroup NormalOutsideLinesGeometry { get; } = new GeometryGroup();
		public GeometryGroup XAxisDottedLinesGeometry { get; } = new GeometryGroup();
		public GeometryGroup YAxisDottedLinesGeometry { get; } = new GeometryGroup();
		public GeometryGroup ZAxisDottedLinesGeometry { get; } = new GeometryGroup();
		public ICommand Viewbox_SizeChanged { get; private set; }
		public ICommand Image_Loaded { get; private set; }
		public ICommand MoveViewbox_Loaded { get; private set; }
		public ICommand FixedGrid_Loaded { get; private set; }

		/// <summary>
		/// Thickness of drawn lines.
		/// </summary>
		public double LineStrokeThickness
		{
			get => _LineStrokeThickness;
			private set
			{
				_LineStrokeThickness = value;
				OnPropertyChanged(nameof(LineStrokeThickness));
			}
		}
		private double _LineStrokeThickness = DefaultLineStrokeThickness;

		/// <summary>
		/// Photo scale. Change LineStrokeThickness based on scale to keep it constant. Send new scale to scalable objects. For pan&zoom.
		/// </summary>
		public double Scale
		{
			get => _Scale;
			private set
			{
				_Scale = value;
				LineStrokeThickness = DefaultLineStrokeThickness / ViewboxImageScale / Scale;
				foreach (var scalable in Scalables)
				{
					scalable.SetNewScale(ViewboxImageScale * Scale);
				}
				OnPropertyChanged(nameof(Scale));
			}
		}
		private double _Scale = 1;

		/// <summary>
		/// Translate the displayed photo and geometry - for pan&zoom.
		/// </summary>
		public Vector2 Translate
		{
			get => _Translate;
			private set
			{
				_Translate = value;
				OnPropertyChanged(nameof(Translate));
			}
		}
		private Vector2 _Translate = new Vector2();

		/// <summary>
		/// Reference to mouse cursor.
		/// </summary>
		public Cursor Cursor
		{
			get => _Cursor;
			private set
			{
				_Cursor = value;
				OnPropertyChanged(nameof(Cursor));
			}
		}
		private Cursor _Cursor = Cursors.Arrow;

		/// <summary>
		/// Polygons inside this collection will be drawn on screen.
		/// </summary>
		public ObservableCollection<Polygon> Polygons { get; } = new ObservableCollection<Polygon>();

		public IMouseHandler MouseHandler
		{
			get => this;
		}

		/// <summary>
		/// Image width.
		/// </summary>
		public double Width => ImageSource.Width;

		/// <summary>
		/// Image height.
		/// </summary>
		public double Height => ImageSource.Height;

		public CalibrationAxes CurrentCalibrationAxes { get; private set; }
		public InvertedAxes CurrentInvertedAxes { get; private set; }

		/// <summary>
		/// List of objects that have to be notified about new scale.
		/// </summary>
		public List<IScalable> Scalables { get; } = new List<IScalable>();

		private ILogger Logger;
		private IImageWindowActions ImageWindowActions;
		private double ViewboxImageScale = double.NaN;
		private Image ImageView;
		private Viewbox MoveViewbox;
		private Grid FixedGrid;
		private MainWindow MainWindow;

		private Vector2 OrigTranslate;
		private Vector2 OrigScreen;

		/// <summary>
		/// Set true if we are dragging the image - changes cursor.
		/// </summary>
		private bool ImageDrag
		{
			get => ImageDrag_;
			set
			{
				if (ImageDrag_ != value)
				{
					ImageDrag_ = value;

					Cursor = ImageDrag ? Cursors.Hand : Cursors.Arrow;
				}
			}
		}
		private bool ImageDrag_;

		/// <param name="imageWindow">Connection to ViewModel layer.</param>
		/// <param name="logger">Gui logger.</param>
		/// <param name="mainWindow">Reference to the main window.</param>
		public ImageViewModel(ImageWindow imageWindow, ILogger logger, MainWindow mainWindow)
		{
			CanClose = true;
			IsClosed = false;
			CloseCommand = new RelayCommand(obj => Close());
			Logger = logger;
			ImageWindowActions = imageWindow;
			MainWindow = mainWindow;

			Viewbox_SizeChanged = new RelayCommand(Viewbox_SizeChanged_);
			Image_Loaded = new RelayCommand(Image_Loaded_);
			MoveViewbox_Loaded = new RelayCommand(MoveViewbox_Loaded_);
			FixedGrid_Loaded = new RelayCommand(FixedGrid_Loaded_);

			SetupGeometry();
		}

		private void SetupGeometry()
		{
			XAxisLinesGeometry.FillRule = FillRule.Nonzero;
			YAxisLinesGeometry.FillRule = FillRule.Nonzero;
			ZAxisLinesGeometry.FillRule = FillRule.Nonzero;
			ModelLinesGeometry.FillRule = FillRule.Nonzero;
			SelectedLinesGeometry.FillRule = FillRule.Nonzero;
			FaceLinesGeometry.FillRule = FillRule.Nonzero;
			HighlightLinesGeometry.FillRule = FillRule.Nonzero;
			VertexLinesGeometry.FillRule = FillRule.Nonzero;
			MidpointLinesGeometry.FillRule = FillRule.Nonzero;
			EdgepointLinesGeometry.FillRule = FillRule.Nonzero;
			InvalidLinesGeometry.FillRule = FillRule.Nonzero;
			NormalLinesGeometry.FillRule = FillRule.Nonzero;
			NormalInsideLinesGeometry.FillRule = FillRule.Nonzero;
			NormalOutsideLinesGeometry.FillRule = FillRule.Nonzero;
			XAxisDottedLinesGeometry.FillRule = FillRule.Nonzero;
			YAxisDottedLinesGeometry.FillRule = FillRule.Nonzero;
			ZAxisDottedLinesGeometry.FillRule = FillRule.Nonzero;
		}

		public void Close()
		{
			ImageWindowActions.Close_Clicked();
		}

		void IImageView.Close()
		{
			ImageWindowActions.Dispose();
			IsClosed = true;
		}

		public bool DisplayWarningProceedMessage(string title, string message)
		{
			return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
		}

		public void SetImage(SixLabors.ImageSharp.Image image)
		{
			SetImageSharpAsImage(image);
		}

		private void SetImageSharpAsImage(SixLabors.ImageSharp.Image imageSharp)
		{
			imageSharp.Metadata.ResolutionUnits = SixLabors.ImageSharp.Metadata.PixelResolutionUnit.PixelsPerInch;
			imageSharp.Metadata.VerticalResolution = 96;
			imageSharp.Metadata.HorizontalResolution = 96;

			var stream = new MemoryStream();
			imageSharp.Save(stream, new SixLabors.ImageSharp.Formats.Bmp.BmpEncoder());
			stream.Seek(0, SeekOrigin.Begin);

			BitmapImage imageCopy = new BitmapImage();
			imageCopy.BeginInit();
			imageCopy.StreamSource = stream;
			imageCopy.EndInit();

			ImageSource = imageCopy;
		}

		/// <summary>
		/// Calculate distance on the screen using scale.
		/// </summary>
		public double ScreenDistance(Vector2 pointA, Vector2 pointB)
		{
			if (double.IsNaN(ViewboxImageScale))
				return double.PositiveInfinity;
			else
				return Scale * ViewboxImageScale * (pointA - pointB).Magnitude;
		}

		/// <summary>
		/// Create and return a WpfLine with specified start, end and color. Add to scalables if it has drawn points in the endpoints (endRadius is non-zero).
		/// </summary>
		public ILine CreateLine(Vector2 start, Vector2 end, double endRadius, ApplicationColor color)
		{
			var wpfLine = new WpfLine(start.AsPoint(), end.AsPoint(), endRadius, this, color);

			wpfLine.SetNewScale(ViewboxImageScale * Scale);
			if (wpfLine.StartEllipse != null && wpfLine.EndEllipse != null)
				Scalables.Add(wpfLine);

			return wpfLine;
		}

		/// <summary>
		/// Create and return a WpfEllipse with specified position, radius and color. Add to scalables.
		/// </summary>
		public IEllipse CreateEllipse(Vector2 position, double radius, ApplicationColor color)
		{
			var wpfEllipse = new WpfEllipse(position.AsPoint(), radius, this, color);

			wpfEllipse.SetNewScale(ViewboxImageScale * Scale);
			Scalables.Add(wpfEllipse);

			return wpfEllipse;
		}

		/// <summary>
		/// Create and return a WpfPolygon with specified color.
		/// </summary>
		public IPolygon CreateFilledPolygon(ApplicationColor color)
		{
			return new WpfPolygon(Polygons, color);
		}

		/// <summary>
		/// Dispose of all data owned by the class.
		/// </summary>
		public void DisposeAll()
		{
			XAxisLinesGeometry.Children.Clear();
			YAxisLinesGeometry.Children.Clear();
			ZAxisLinesGeometry.Children.Clear();
			ModelLinesGeometry.Children.Clear();
			SelectedLinesGeometry.Children.Clear();
			FaceLinesGeometry.Children.Clear();
			HighlightLinesGeometry.Children.Clear();
			VertexLinesGeometry.Children.Clear();
			MidpointLinesGeometry.Children.Clear();
			EdgepointLinesGeometry.Children.Clear();
			InvalidLinesGeometry.Children.Clear();
			NormalLinesGeometry.Children.Clear();
			NormalInsideLinesGeometry.Children.Clear();
			NormalOutsideLinesGeometry.Children.Clear();
			XAxisDottedLinesGeometry.Children.Clear();
			YAxisDottedLinesGeometry.Children.Clear();
			ZAxisDottedLinesGeometry.Children.Clear();
			Scalables.Clear();
			ImageSource = null;

			IsClosed = true;
		}

		/// <summary>
		/// Convert from System.Windows.Input.MouseButton to PhotomatchCore.Gui.MouseButton.
		/// </summary>
		private PhotomatchCore.Gui.MouseButton? GetMouseButton(System.Windows.Input.MouseButton button, int clickCount)
		{
			if (button == System.Windows.Input.MouseButton.Left)
				return clickCount == 1 ? PhotomatchCore.Gui.MouseButton.Left : PhotomatchCore.Gui.MouseButton.DoubleLeft;
			else if (button == System.Windows.Input.MouseButton.Right)
				return clickCount == 1 ? PhotomatchCore.Gui.MouseButton.Right : PhotomatchCore.Gui.MouseButton.DoubleRight;
			else if (button == System.Windows.Input.MouseButton.Middle)
				return clickCount == 1 ? PhotomatchCore.Gui.MouseButton.Middle : PhotomatchCore.Gui.MouseButton.DoubleMiddle;
			else
				return null;
		}

		/// <summary>
		/// Handle drag&drop or pass MouseDown to ViewModel.
		/// </summary>
		public void MouseDown(object sender, MouseButtonEventArgs e)
		{
			PhotomatchCore.Gui.MouseButton? button = GetMouseButton(e.ChangedButton, e.ClickCount);
			if (!button.HasValue)
				return;

			if (button.Value == PhotomatchCore.Gui.MouseButton.Middle)
				ImageDrag = true;

			if (ImageDrag)
			{
				if (FixedGrid != null)
				{
					OrigScreen = e.GetPosition(FixedGrid).AsVector2();
					OrigTranslate = Translate;
				}
			}
			else if (ImageView != null)
			{
				Point point = e.GetPosition(ImageView);

				if (point.X < 0 || point.Y < 0 || point.X >= Width || point.Y >= Height)
					return;

				ImageWindowActions.MouseDown(point.AsVector2(), button.Value);
			}
		}

		/// <summary>
		/// Handle drag&drop or pass MouseUp to ViewModel.
		/// </summary>
		public void MouseUp(object sender, MouseButtonEventArgs e)
		{
			PhotomatchCore.Gui.MouseButton? button = GetMouseButton(e.ChangedButton, e.ClickCount);
			if (!button.HasValue)
				return;

			if (ImageDrag)
			{
				if (button.Value == PhotomatchCore.Gui.MouseButton.Middle)
					ImageDrag = false;
			}
			else if (ImageView != null)
			{
				Point point = e.GetPosition(ImageView);

				if (point.X < 0 || point.Y < 0 || point.X >= Width || point.Y >= Height)
					return;

				ImageWindowActions.MouseUp(point.AsVector2(), button.Value);
			}
		}

		/// <summary>
		/// Handle drag&drop or pass MouseMove to ViewModel.
		/// </summary>
		public void MouseMove(object sender, MouseEventArgs e)
		{
			if (ImageDrag)
			{
				if (FixedGrid != null)
				{
					Vector2 v = OrigScreen - e.GetPosition(FixedGrid).AsVector2();
					Translate = new Vector2(OrigTranslate.X - v.X, OrigTranslate.Y - v.Y);
				}
			}
			else if (ImageView != null)
			{
				Point point = e.GetPosition(ImageView);

				if (point.X < 0 || point.Y < 0 || point.X >= Width || point.Y >= Height)
					return;

				ImageWindowActions.MouseMove(point.AsVector2());
			}
		}

		public void MouseEnter(object sender, MouseEventArgs e) { }

		/// <summary>
		/// Handle drag&drop or pass MouseUp to ViewModel (so that there are no weird issues when mouse leaves 
		/// the window pressed down, and then returns).
		/// </summary>
		public void MouseLeave(object sender, MouseEventArgs e)
		{
			if (ImageDrag)
				ImageDrag = false;
			else if (ImageView != null)
			{
				Point point = e.GetPosition(ImageView);

				point = new Point(Math.Clamp(point.X, 0, Width - 1), Math.Clamp(point.X, 0, Height - 1));
				ImageWindowActions.MouseUp(point.AsVector2(), PhotomatchCore.Gui.MouseButton.Left);
				ImageWindowActions.MouseUp(point.AsVector2(), PhotomatchCore.Gui.MouseButton.Middle);
				ImageWindowActions.MouseUp(point.AsVector2(), PhotomatchCore.Gui.MouseButton.Right);
			}
		}

		/// <summary>
		/// Handle pan&zoom. Pass MouseMove to ViewModel (mouse can move to a different part of the photo on zoom).
		/// </summary>
		public void MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (MoveViewbox != null)
			{
				double zoom = Math.Pow(ZoomAmount, e.Delta);

				Vector2 relative = e.GetPosition(MoveViewbox).AsVector2();
				Vector2 absolute = relative * Scale + Translate;

				Scale *= zoom;

				Translate = absolute - relative * Scale;

				if (ImageDrag && FixedGrid != null)
				{
					OrigScreen = e.GetPosition(FixedGrid).AsVector2();
					OrigTranslate = Translate;
				}
			}

			if (!ImageDrag && ImageView != null)
			{
				Point point = e.GetPosition(ImageView);

				if (point.X < 0 || point.Y < 0 || point.X >= Width || point.Y >= Height)
					return;

				ImageWindowActions.MouseMove(point.AsVector2());
			}


		}

		/// <summary>
		/// Update geometry scale if ViewBox size changed.
		/// </summary>
		private void Viewbox_SizeChanged_(object args)
		{
			SizeChangedEventArgs sizeArgs = args as SizeChangedEventArgs;
			if (sizeArgs == null)
			{
				throw new ArgumentException("args is not of type SizeChangedEventArgs");
			}

			Size viewboxSize = sizeArgs.NewSize;
			ViewboxImageScale = viewboxSize.Height / ImageSource.Height;
			LineStrokeThickness = DefaultLineStrokeThickness / ViewboxImageScale / Scale;

			foreach (var scalable in Scalables)
			{
				scalable.SetNewScale(ViewboxImageScale * Scale);
			}
		}

		/// <summary>
		/// Set ImageView to loaded image.
		/// </summary>
		private void Image_Loaded_(object obj)
		{
			Image image = obj as Image;
			if (image == null)
			{
				throw new ArgumentException("obj is not of type Image");
			}

			ImageView = image;
		}

		/// <summary>
		/// Set FixedGrid to loaded Grid.
		/// </summary>
		private void FixedGrid_Loaded_(object obj) => FixedGrid = obj as Grid;

		/// <summary>
		/// Set MoveViewbox to loaded Viewbox.
		/// </summary>
		private void MoveViewbox_Loaded_(object obj) => MoveViewbox = obj as Viewbox;

		public void DisplayCalibrationAxes(CalibrationAxes calibrationAxes)
		{
			CurrentCalibrationAxes = calibrationAxes;
			MainWindow.DisplayCalibrationAxes(this, calibrationAxes);
		}

		public void CalibrationAxes_Changed(CalibrationAxes calibrationAxes)
		{
			ImageWindowActions.CalibrationAxes_Changed(calibrationAxes);
		}

		public void DisplayInvertedAxes(CalibrationAxes calibrationAxes, InvertedAxes invertedAxes)
		{
			CurrentInvertedAxes = invertedAxes;
			MainWindow.DisplayInvertedAxes(this, calibrationAxes, invertedAxes);
		}

		public void InvertedAxes_Changed(InvertedAxes invertedAxes)
		{
			ImageWindowActions.InvertedAxes_Changed(invertedAxes);
		}

		/// <summary>
		/// Pass KeyUp to ViewModel layer.
		/// </summary>
		public void KeyUp(object sender, KeyEventArgs e)
		{
			KeyboardKey? key = e.Key.AsKeyboardKey();
			if (key.HasValue)
				ImageWindowActions.KeyUp(key.Value);
		}

		/// <summary>
		/// Pass KeyDown to ViewModel layer.
		/// </summary>
		public void KeyDown(object sender, KeyEventArgs e)
		{
			KeyboardKey? key = e.Key.AsKeyboardKey();
			if (key.HasValue)
				ImageWindowActions.KeyDown(key.Value);
		}
	}
}
