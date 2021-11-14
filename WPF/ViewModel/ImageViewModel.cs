using GuiControls;
using GuiEnums;
using GuiInterfaces;
using MatrixVector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Photomatch_ProofOfConcept_WPF.WPF.ViewModel
{
	public class ImageViewModel : IWindow
	{
        public Action CloseCommand { get; }
        public bool IsClosed { get; set; }
        public bool CanClose { get; set; }
        public string Title { get; set; }

		public BitmapImage ImageSource { get; private set; }

        public ImageViewModel(ImageWindow imageWindow)
        {
            this.CanClose = true;
            this.IsClosed = false;
            CloseCommand = () => Close();
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
	}
}
