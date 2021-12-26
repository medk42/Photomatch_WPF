using GuiControls;
using GuiEnums;
using GuiInterfaces;
using Logging;
using MatrixVector;
using Photomatch_ProofOfConcept_WPF.WPF.Helper;
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
using WpfExtensions;
using WpfGuiElements;
using WpfInterfaces;

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

		private ILogger Logger;
		private ImageWindow ImageWindow;
		private double ViewboxImageScale;
		private List<IScalable> scalables = new List<IScalable>();
		private Image ImageView;

		public ImageViewModel(ImageWindow imageWindow, ILogger logger)
        {
            this.CanClose = true;
            this.IsClosed = false;
            this.CloseCommand = new RelayCommand(obj => Close());
			this.Logger = logger;
			this.ImageWindow = imageWindow;

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

			var wpfLine = new WpfLine(start.AsPoint(), end.AsPoint(), endRadius);
			wpfLine.SetNewScale(ViewboxImageScale);
			geometry.Children.Add(wpfLine.Line);
			if (wpfLine.StartEllipse != null && wpfLine.EndEllipse != null)
			{
				geometry.Children.Add(wpfLine.StartEllipse);
				geometry.Children.Add(wpfLine.EndEllipse);
				scalables.Add(wpfLine);
			}

			return wpfLine;
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
			ImageWindow.MouseUp(point.AsVector2(), button.Value);

			Logger.Log($"Mouse Event (\"{Title}\")", $"Mouse Up at {point.X}, {point.Y} by {e.ChangedButton}", LogType.Info);
		}

		public void MouseMove(object sender, MouseEventArgs e)
		{
			if (ImageView == null)
				return;

			Point point = e.GetPosition(ImageView);
			ImageWindow.MouseMove(point.AsVector2());

			Logger.Log($"Mouse Event (\"{Title}\")", $"Mouse Move at {point.X}, {point.Y}", LogType.Info);
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
	}
}
