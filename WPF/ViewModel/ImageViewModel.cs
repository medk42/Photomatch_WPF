using GuiControls;
using GuiEnums;
using GuiInterfaces;
using Logging;
using MatrixVector;
using Photomatch_ProofOfConcept_WPF.WPF.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Photomatch_ProofOfConcept_WPF.WPF.ViewModel
{
	public class ImageViewModel : IWindow, IMouseHandler
	{
		public Action CloseCommand { get; }
		public bool IsClosed { get; set; }
		public bool CanClose { get; set; }
		public string Title { get; set; }

		public BitmapImage ImageSource { get; private set; }

		public IMouseHandler MouseHandler
		{
			get => this;
		}

		private ILogger Logger;

        public ImageViewModel(ImageWindow imageWindow, ILogger logger)
        {
            this.CanClose = true;
            this.IsClosed = false;
            this.CloseCommand = () => Close();
			this.Logger = logger;
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
			return (pointA - pointB).Magnitude;
			throw new NotImplementedException();
		}

		class L : ILine
		{
			public Vector2 Start { get; set; }
			public Vector2 End { get; set; }
		}

		public ILine CreateLine(Vector2 start, Vector2 end, double endRadius, ApplicationColor color)
		{
			return new L() { Start = start, End = end };
			throw new NotImplementedException();
		}

		public void DisposeAll()
		{
			return;
			throw new NotImplementedException();
		}

		public void MouseDown(object sender, MouseButtonEventArgs e)
		{
			Logger.Log("Mouse Event", $"Mouse Down by {e.ChangedButton}", LogType.Info);
		}

		public void MouseUp(object sender, MouseButtonEventArgs e)
		{
			Logger.Log("Mouse Event", $"Mouse Up by {e.ChangedButton}", LogType.Info);
		}

		public void MouseMove(object sender, MouseEventArgs e)
		{
			Logger.Log("Mouse Event", $"Mouse Move", LogType.Info);
		}


		/*
		public void SetImage(SixLabors.ImageSharp.Image image)
		{
			// TODO id will be used when multiple windows are implemented
			SetImageSharpAsImage(image, MainImage);
		}

		private void SetImageSharpAsImage(SixLabors.ImageSharp.Image imageSharp, Image image)
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

			image.Source = imageCopy;
		}

		public double ScreenDistance(Vector2 pointA, Vector2 pointB)
		{
			// TODO id will be used when multiple windows are implemented
			Point pointATranslated = MainImage.TranslatePoint(pointA.AsPoint(), MyMainWindow);
			Point pointBTranslated = MainImage.TranslatePoint(pointB.AsPoint(), MyMainWindow);
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

			var wpfLine = new WpfLine(start.AsPoint(), end.AsPoint(), endRadius);
			wpfLine.SetNewScale(MainViewbox.ActualHeight / MainImage.ActualHeight);
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
			MainImage.Source = null;
		}*/
	}
}
