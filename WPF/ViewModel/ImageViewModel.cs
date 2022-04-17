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

using Photomatch_ProofOfConcept_WPF.WPF.Helper;
using Photomatch_ProofOfConcept_WPF.Gui;
using Photomatch_ProofOfConcept_WPF.Logic;
using Photomatch_ProofOfConcept_WPF.Utilities;
using Photomatch_ProofOfConcept_WPF.Gui.GuiControls;
using Photomatch_ProofOfConcept_WPF;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace Photomatch_ProofOfConcept_WPF.WPF.ViewModel
{
	public class ImageViewModel : BaseViewModel, IWindow, IMouseHandler, IKeyboardHandler
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

		private double _LineStrokeThickness = DefaultLineStrokeThickness;
		public double LineStrokeThickness
		{
			get => _LineStrokeThickness;
			private set
			{
				_LineStrokeThickness = value;
				OnPropertyChanged(nameof(LineStrokeThickness));
			}
		}

		private double _Scale = 1;
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

		private Vector2 _Translate = new Vector2();
		public Vector2 Translate
		{
			get => _Translate;
			private set
			{
				_Translate = value;
				OnPropertyChanged(nameof(Translate));
			}
		}

		private Cursor _Cursor = Cursors.Arrow;
		public Cursor Cursor
		{
			get => _Cursor;
			private set
			{
				_Cursor = value;
				OnPropertyChanged(nameof(Cursor));
			}
		}

		public ObservableCollection<Polygon> Polygons { get; } = new ObservableCollection<Polygon>();

		public IMouseHandler MouseHandler
		{
			get => this;
		}

		public double Width => ImageSource.Width;

		public double Height => ImageSource.Height;

		public CalibrationAxes CurrentCalibrationAxes { get; private set; }
		public InvertedAxes CurrentInvertedAxes { get; private set; }
		public List<IScalable> Scalables { get; } = new List<IScalable>();

		private ILogger Logger;
		private ImageWindow ImageWindow;
		private double ViewboxImageScale = double.NaN;
		private Image ImageView;
		private Viewbox MoveViewbox;
		private Grid FixedGrid;
		private MainWindow MainWindow;

		private Vector2 OrigTranslate;
		private Vector2 OrigScreen;

		private bool ImageDrag_;
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

		public ImageViewModel(ImageWindow imageWindow, ILogger logger, MainWindow mainWindow)
        {
            this.CanClose = true;
            this.IsClosed = false;
            this.CloseCommand = new RelayCommand(obj => Close());
			this.Logger = logger;
			this.ImageWindow = imageWindow;
			this.MainWindow = mainWindow;

			this.Viewbox_SizeChanged = new RelayCommand(Viewbox_SizeChanged_);
			this.Image_Loaded = new RelayCommand(Image_Loaded_);
			this.MoveViewbox_Loaded = new RelayCommand(MoveViewbox_Loaded_);
			this.FixedGrid_Loaded = new RelayCommand(FixedGrid_Loaded_);

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
			ImageWindow.Close_Clicked();
        }

		void IWindow.Close()
		{
			ImageWindow.Dispose();
			this.IsClosed = true;
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

		public double ScreenDistance(Vector2 pointA, Vector2 pointB)
		{
			if (double.IsNaN(ViewboxImageScale))
				return double.PositiveInfinity;
			else
				return Scale * ViewboxImageScale * (pointA - pointB).Magnitude;
		}

		public ILine CreateLine(Vector2 start, Vector2 end, double endRadius, ApplicationColor color)
		{
			var wpfLine = new WpfLine(start.AsPoint(), end.AsPoint(), endRadius, this, color);

			wpfLine.SetNewScale(ViewboxImageScale * Scale);
			if (wpfLine.StartEllipse != null && wpfLine.EndEllipse != null)
				Scalables.Add(wpfLine);

			return wpfLine;
		}

		public IEllipse CreateEllipse(Vector2 position, double radius, ApplicationColor color)
		{
			var wpfEllipse = new WpfEllipse(position.AsPoint(), radius, this, color);

			wpfEllipse.SetNewScale(ViewboxImageScale * Scale);
			Scalables.Add(wpfEllipse);				

			return wpfEllipse;
		}

		public IPolygon CreateFilledPolygon(ApplicationColor color)
		{
			return new WpfPolygon(Polygons, color);
		}

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

		private Gui.MouseButton? GetMouseButton(System.Windows.Input.MouseButton button, int clickCount)
		{
			if (button == System.Windows.Input.MouseButton.Left)
				return clickCount == 1 ? Gui.MouseButton.Left : Gui.MouseButton.DoubleLeft;
			else if (button == System.Windows.Input.MouseButton.Right)
				return clickCount == 1 ? Gui.MouseButton.Right : Gui.MouseButton.DoubleRight;
			else if (button == System.Windows.Input.MouseButton.Middle)
				return clickCount == 1 ? Gui.MouseButton.Middle : Gui.MouseButton.DoubleMiddle;
			else
				return null;
		}

		public void MouseDown(object sender, MouseButtonEventArgs e)
		{
			Gui.MouseButton? button = GetMouseButton(e.ChangedButton, e.ClickCount);
			if (!button.HasValue)
				return;

			if (button.Value == Gui.MouseButton.Middle)
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

				ImageWindow.MouseDown(point.AsVector2(), button.Value);
			}
		}

		public void MouseUp(object sender, MouseButtonEventArgs e)
		{
			Gui.MouseButton? button = GetMouseButton(e.ChangedButton, e.ClickCount);
			if (!button.HasValue)
				return;

			if (ImageDrag)
			{
				if (button.Value == Gui.MouseButton.Middle)
					ImageDrag = false;
			}
			else if (ImageView != null)
			{
				Point point = e.GetPosition(ImageView);

				if (point.X < 0 || point.Y < 0 || point.X >= Width || point.Y >= Height)
					return;

				ImageWindow.MouseUp(point.AsVector2(), button.Value);
			}
		}

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

				ImageWindow.MouseMove(point.AsVector2());
			}
		}

		public void MouseEnter(object sender, MouseEventArgs e) { }

		public void MouseLeave(object sender, MouseEventArgs e)
		{
			if (ImageDrag)
				ImageDrag = false;
			else if (ImageView != null)
			{
				Point point = e.GetPosition(ImageView);

				point = new Point(Math.Clamp(point.X, 0, Width - 1), Math.Clamp(point.X, 0, Height - 1));
				ImageWindow.MouseUp(point.AsVector2(), Gui.MouseButton.Left);
				ImageWindow.MouseUp(point.AsVector2(), Gui.MouseButton.Middle);
				ImageWindow.MouseUp(point.AsVector2(), Gui.MouseButton.Right);
			}
		}

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

				ImageWindow.MouseMove(point.AsVector2());
			}

			
		}

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

		private void Image_Loaded_(object obj)
		{
			Image image = obj as Image;
			if (image == null)
			{
				throw new ArgumentException("obj is not of type Image");
			}
			
			ImageView = image;
		}

		private void FixedGrid_Loaded_(object obj) => FixedGrid = obj as Grid;

		private void MoveViewbox_Loaded_(object obj) => MoveViewbox = obj as Viewbox;

		public void DisplayCalibrationAxes(CalibrationAxes calibrationAxes)
		{
			CurrentCalibrationAxes = calibrationAxes;
			MainWindow.DisplayCalibrationAxes(calibrationAxes);
		}

		public void CalibrationAxes_Changed(CalibrationAxes calibrationAxes)
		{
			ImageWindow.CalibrationAxes_Changed(calibrationAxes);
		}

		public void DisplayInvertedAxes(CalibrationAxes calibrationAxes, InvertedAxes invertedAxes)
		{
			CurrentInvertedAxes = invertedAxes;
			MainWindow.DisplayInvertedAxes(calibrationAxes, invertedAxes);
		}

		public void InvertedAxes_Changed(InvertedAxes invertedAxes)
		{
			ImageWindow.InvertedAxes_Changed(invertedAxes);
		}

		public void KeyUp(object sender, KeyEventArgs e)
		{
			KeyboardKey? key = e.Key.AsKeyboardKey();
			if (key.HasValue)
				ImageWindow.KeyUp(key.Value);
		}

		public void KeyDown(object sender, KeyEventArgs e)
		{
			KeyboardKey? key = e.Key.AsKeyboardKey();
			if (key.HasValue)
				ImageWindow.KeyDown(key.Value);
		}
	}
}
