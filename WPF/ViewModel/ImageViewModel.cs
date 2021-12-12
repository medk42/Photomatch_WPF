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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfExtensions;

namespace Photomatch_ProofOfConcept_WPF.WPF.ViewModel
{
	public class ImageViewModel : IWindow, IMouseHandler
	{
		public Action CloseCommand { get; }
		public bool IsClosed { get; set; }
		public bool CanClose { get; set; }
		public string Title { get; set; }

		public BitmapImage ImageSource { get; private set; }

		public GeometryGroup XAxisLinesGeometry { get; private set; } = new GeometryGroup();
		public GeometryGroup YAxisLinesGeometry { get; private set; } = new GeometryGroup();
		public GeometryGroup ZAxisLinesGeometry { get; private set; } = new GeometryGroup();
		public GeometryGroup ModelLinesGeometry { get; private set; } = new GeometryGroup();

		public static double LineStrokeThickness { get; } = 2;

		public IMouseHandler MouseHandler
		{
			get => this;
		}

		private ILogger Logger;
		private ImageWindow ImageWindow;

		public ImageViewModel(ImageWindow imageWindow, ILogger logger)
        {
            this.CanClose = true;
            this.IsClosed = false;
            this.CloseCommand = () => Close();
			this.Logger = logger;
			this.ImageWindow = imageWindow;

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
			return (pointA - pointB).Magnitude;
			throw new NotImplementedException();
		}

		class L : ILine // TODO delete when rewriting CreateLine?
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

			Image image = sender as Image;
			if (image == null)
				return;

			Point point = e.GetPosition(image);
			ImageWindow.MouseDown(point.AsVector2(), button.Value);

			Logger.Log($"Mouse Event (\"{Title}\")", $"Mouse Down at {point.X}, {point.Y} by {e.ChangedButton}", LogType.Info);
		}

		public void MouseUp(object sender, MouseButtonEventArgs e)
		{
			GuiEnums.MouseButton? button = GetMouseButton(e.ChangedButton);
			if (!button.HasValue)
				return;

			Image image = sender as Image;
			if (image == null)
				return;

			Point point = e.GetPosition(image);
			ImageWindow.MouseUp(point.AsVector2(), button.Value);

			Logger.Log($"Mouse Event (\"{Title}\")", $"Mouse Up at {point.X}, {point.Y} by {e.ChangedButton}", LogType.Info);
		}

		public void MouseMove(object sender, MouseEventArgs e)
		{
			Image image = sender as Image;
			if (image == null)
				return;

			Point point = e.GetPosition(image);
			ImageWindow.MouseMove(point.AsVector2());

			Logger.Log($"Mouse Event (\"{Title}\")", $"Mouse Move at {point.X}, {point.Y}", LogType.Info);
		}


		/*
		private List<IScalable> scalables = new List<IScalable>(); 
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
		}*/


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
