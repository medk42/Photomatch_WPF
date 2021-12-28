﻿using GuiControls;
using GuiEnums;
using GuiInterfaces;
using Logging;
using MatrixVector;
using Photomatch_ProofOfConcept_WPF.WPF.Helper;
using Perspective;
using WpfExtensions;
using WpfGuiElements;
using WpfInterfaces;

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

namespace Photomatch_ProofOfConcept_WPF.WPF.ViewModel
{
	public class ImageViewModel : BaseViewModel, IWindow, IMouseHandler
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
		private double ViewboxImageScale;
		private List<IScalable> scalables = new List<IScalable>();
		private Image ImageView;
		private MainWindow MainWindow;

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
			scalables.Clear(); 
			ImageSource = null;

			IsClosed = true;
		}

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

		public void MouseDown(object sender, MouseButtonEventArgs e)
		{
			GuiEnums.MouseButton? button = GetMouseButton(e.ChangedButton);
			if (!button.HasValue)
				return;

			if (ImageView == null)
				return;

			Point point = e.GetPosition(ImageView);

			if (point.X < 0 || point.Y < 0 || point.X >= Width || point.Y >= Height)
				return;

			ImageWindow.MouseDown(point.AsVector2(), button.Value);

			Logger.Log($"Mouse Event (\"{Title}\")", $"Mouse Down at {point.X}, {point.Y} by {e.ChangedButton}", LogType.Info);
		}

		public void MouseUp(object sender, MouseButtonEventArgs e)
		{
			GuiEnums.MouseButton? button = GetMouseButton(e.ChangedButton);
			if (!button.HasValue)
				return;

			if (ImageView == null)
				return;

			Point point = e.GetPosition(ImageView);

			if (point.X < 0 || point.Y < 0 || point.X >= Width || point.Y >= Height)
				return;

			ImageWindow.MouseUp(point.AsVector2(), button.Value);

			Logger.Log($"Mouse Event (\"{Title}\")", $"Mouse Up at {point.X}, {point.Y} by {e.ChangedButton}", LogType.Info);
		}

		public void MouseMove(object sender, MouseEventArgs e)
		{
			if (ImageView == null)
				return;

			Point point = e.GetPosition(ImageView);

			if (point.X < 0 || point.Y < 0 || point.X >= Width || point.Y >= Height)
				return;

			ImageWindow.MouseMove(point.AsVector2());

			Logger.Log($"Mouse Event (\"{Title}\")", $"Mouse Move at {point.X}, {point.Y}", LogType.Info);
		}

		public void MouseEnter(object sender, MouseEventArgs e) { }

		public void MouseLeave(object sender, MouseEventArgs e)
		{
			if (ImageView == null)
				return;

			Point point = e.GetPosition(ImageView);

			point = new Point(Math.Clamp(point.X, 0, Width - 1), Math.Clamp(point.X, 0, Height - 1));
			ImageWindow.MouseUp(point.AsVector2(), GuiEnums.MouseButton.Left);
			ImageWindow.MouseUp(point.AsVector2(), GuiEnums.MouseButton.Middle);
			ImageWindow.MouseUp(point.AsVector2(), GuiEnums.MouseButton.Right);

			Logger.Log($"Mouse Event (\"{Title}\")", $"Mouse Leave at {point.X}, {point.Y}", LogType.Info);
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

			Logger.Log($"Viewbox Size Event (\"{Title}\")", $"Size change to {viewboxSize.Width}, {viewboxSize.Height}, Image {ImageSource.Width}, {ImageSource.Height}, Scale {viewboxSize.Height / ImageSource.Height}", LogType.Info);
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
	}
}
