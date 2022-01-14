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

namespace Photomatch_ProofOfConcept_WPF.WPF.ViewModel
{
	public class ImageViewModel : BaseViewModel, IWindow, IMouseHandler, IKeyboardHandler
	{
		private static readonly double DefaultLineStrokeThickness = 2;

		public ICommand CloseCommand { get; }

		private bool CanClose_;
		public bool CanClose
		{
			get => CanClose_;
			set
			{
				if (CanClose_ != value)
				{
					CanClose_ = value;
					OnPropertyChanged(nameof(CanClose));
				}
			}
		}

		private string Title_;
		public string Title
		{
			get => Title_;
			set
			{
				if (Title_ != value)
				{
					Title_ = value;
					OnPropertyChanged(nameof(Title));
				}
			}
		}

		private bool _IsClosed;
		public bool IsClosed
		{
			get => _IsClosed;
			private set
			{
				if (_IsClosed != value)
				{
					_IsClosed = value;
					OnPropertyChanged(nameof(IsClosed));
				}
			}
		}

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
		public ICommand Viewbox_SizeChanged { get; private set; }
		public ICommand Image_Loaded { get; private set; }

		private double _LineStrokeThickness = DefaultLineStrokeThickness;
		public double LineStrokeThickness {
			get => _LineStrokeThickness;
			private set
			{
				_LineStrokeThickness = value;
				OnPropertyChanged(nameof(LineStrokeThickness));
			}
		}

		public IMouseHandler MouseHandler
		{
			get => this;
		}

		public double Width => ImageSource.Width;

		public double Height => ImageSource.Height;

		public CalibrationAxes CurrentCalibrationAxes { get; private set; }
		public InvertedAxes CurrentInvertedAxes { get; private set; }

		private ILogger Logger;
		private ImageWindow ImageWindow;
		private double ViewboxImageScale = double.NaN;
		private List<IScalable> scalables = new List<IScalable>();
		private Image ImageView;
		private MainWindow MainWindow;
		private HashSet<Key> PressedKeys = new HashSet<Key>();

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

			SetupGeometry();
		}

		private void SetupGeometry()
		{
			XAxisLinesGeometry.FillRule = FillRule.Nonzero;
			YAxisLinesGeometry.FillRule = FillRule.Nonzero;
			ZAxisLinesGeometry.FillRule = FillRule.Nonzero;
			ModelLinesGeometry.FillRule = FillRule.Nonzero;
			SelectedLinesGeometry.FillRule = FillRule.Nonzero;
		}

        public void Close()
        {
			ImageWindow.Dispose();
			this.IsClosed = true;
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
				return ViewboxImageScale * (pointA - pointB).Magnitude;
		}

		public ILine CreateLine(Vector2 start, Vector2 end, double endRadius, ApplicationColor color)
		{
			var wpfLine = new WpfLine(start.AsPoint(), end.AsPoint(), endRadius, this, color);

			wpfLine.SetNewScale(ViewboxImageScale);
			if (wpfLine.StartEllipse != null && wpfLine.EndEllipse != null)
				scalables.Add(wpfLine);

			return wpfLine;
		}

		public IEllipse CreateEllipse(Vector2 position, double radius, ApplicationColor color)
		{
			var wpfEllipse = new WpfEllipse(position.AsPoint(), radius, this, color);

			wpfEllipse.SetNewScale(ViewboxImageScale);
			scalables.Add(wpfEllipse);				

			return wpfEllipse;
		}

		public void DisposeAll()
		{
			XAxisLinesGeometry.Children.Clear();
			YAxisLinesGeometry.Children.Clear();
			ZAxisLinesGeometry.Children.Clear();
			ModelLinesGeometry.Children.Clear();
			SelectedLinesGeometry.Children.Clear();
			scalables.Clear(); 
			ImageSource = null;

			IsClosed = true;
		}

		private Gui.MouseButton? GetMouseButton(System.Windows.Input.MouseButton button)
		{
			if (button == System.Windows.Input.MouseButton.Left)
				return Gui.MouseButton.Left;
			else if (button == System.Windows.Input.MouseButton.Right)
				return Gui.MouseButton.Right;
			else if (button == System.Windows.Input.MouseButton.Middle)
				return Gui.MouseButton.Middle;
			else
				return null;
		}

		public void MouseDown(object sender, MouseButtonEventArgs e)
		{
			Gui.MouseButton? button = GetMouseButton(e.ChangedButton);
			if (!button.HasValue)
				return;

			if (ImageView == null)
				return;

			Point point = e.GetPosition(ImageView);

			if (point.X < 0 || point.Y < 0 || point.X >= Width || point.Y >= Height)
				return;

			ImageWindow.MouseDown(point.AsVector2(), button.Value);
		}

		public void MouseUp(object sender, MouseButtonEventArgs e)
		{
			Gui.MouseButton? button = GetMouseButton(e.ChangedButton);
			if (!button.HasValue)
				return;

			if (ImageView == null)
				return;

			Point point = e.GetPosition(ImageView);

			if (point.X < 0 || point.Y < 0 || point.X >= Width || point.Y >= Height)
				return;

			ImageWindow.MouseUp(point.AsVector2(), button.Value);
		}

		public void MouseMove(object sender, MouseEventArgs e)
		{
			if (ImageView == null)
				return;

			Point point = e.GetPosition(ImageView);

			if (point.X < 0 || point.Y < 0 || point.X >= Width || point.Y >= Height)
				return;

			ImageWindow.MouseMove(point.AsVector2());
		}

		public void MouseEnter(object sender, MouseEventArgs e) { }

		public void MouseLeave(object sender, MouseEventArgs e)
		{
			if (ImageView == null)
				return;

			Point point = e.GetPosition(ImageView);

			point = new Point(Math.Clamp(point.X, 0, Width - 1), Math.Clamp(point.X, 0, Height - 1));
			ImageWindow.MouseUp(point.AsVector2(), Gui.MouseButton.Left);
			ImageWindow.MouseUp(point.AsVector2(), Gui.MouseButton.Middle);
			ImageWindow.MouseUp(point.AsVector2(), Gui.MouseButton.Right);
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

			foreach (var scalable in scalables)
			{
				scalable.SetNewScale(ViewboxImageScale);
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
			PressedKeys.Remove(e.Key);

			switch (e.Key)
			{
				case Key.LeftShift:
					ImageWindow.KeyUp(KeyboardKey.LeftShift);
					break;
			}

			Logger.Log($"Keyboard event ({Title})", $"Key Up: {e.Key}", LogType.Info);
		}

		public void KeyDown(object sender, KeyEventArgs e)
		{
			if (!PressedKeys.Contains(e.Key))
			{
				PressedKeys.Add(e.Key);

				switch (e.Key)
				{
					case Key.LeftShift:
						ImageWindow.KeyDown(KeyboardKey.LeftShift);
						break;
					case Key.Escape:
						ImageWindow.KeyDown(KeyboardKey.Escape);
						break;
				}

				Logger.Log($"Keyboard event ({Title})", $"Key Down: {e.Key}", LogType.Info);
			}
		}
	}
}
