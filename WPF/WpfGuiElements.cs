using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

using MatrixVector;
using WpfExtensions;
using WpfInterfaces;
using GuiInterfaces;
using GuiEnums;
using Photomatch_ProofOfConcept_WPF.WPF.ViewModel;

namespace WpfGuiElements
{
	public class WpfLine : ILine, IScalable
	{
		public LineGeometry Line { get; }
		public EllipseGeometry StartEllipse { get; }
		public EllipseGeometry EndEllipse { get; }

		private double EndRadius;
		private ImageViewModel ImageViewModel;
		private ApplicationColor Color;

		public WpfLine(Point Start, Point End, double endRadius, ImageViewModel imageViewModel, ApplicationColor color)
		{
			Line = new LineGeometry();
			Line.StartPoint = Start;
			Line.EndPoint = End;

			EndRadius = endRadius;
			Color = color;
			ImageViewModel = imageViewModel;

			if (endRadius != 0)
			{
				StartEllipse = new EllipseGeometry(Start, endRadius, endRadius);
				EndEllipse = new EllipseGeometry(End, endRadius, endRadius);
			}
			else
			{
				StartEllipse = null;
				EndEllipse = null;
			}

			AddNewColor(Color);
		}

		public Vector2 Start
		{
			get => Line.StartPoint.AsVector2();
			set
			{
				var point = value.AsPoint();
				Line.StartPoint = point;
				if (StartEllipse != null)
					StartEllipse.Center = point;
			}
		}

		public Vector2 End
		{
			get => Line.EndPoint.AsVector2();
			set
			{
				var point = value.AsPoint();
				Line.EndPoint = point;
				if (EndEllipse != null)
					EndEllipse.Center = point;
			}
		}

		public void SetNewScale(double scale)
		{
			if (StartEllipse != null)
			{
				StartEllipse.RadiusX = EndRadius / scale;
				StartEllipse.RadiusY = EndRadius / scale;
			}
			if (EndEllipse != null)
			{
				EndEllipse.RadiusX = EndRadius / scale;
				EndEllipse.RadiusY = EndRadius / scale;
			}
		}

		public void SetColor(ApplicationColor color)
		{
			if (Color != color)
			{
				RemoveOldColor(Color);
				AddNewColor(color);
				Color = color;
			}
		}

		private void RemoveOldColor(ApplicationColor color)
		{
			GeometryGroup geometry = GetGeometry(color);
			geometry.Children.Remove(Line);
			if (StartEllipse != null && EndEllipse != null)
			{
				geometry.Children.Remove(StartEllipse);
				geometry.Children.Remove(EndEllipse);
			}
		}

		private void AddNewColor(ApplicationColor color)
		{
			GeometryGroup geometry = GetGeometry(color);

			geometry.Children.Add(Line);
			if (StartEllipse != null && EndEllipse != null)
			{
				geometry.Children.Add(StartEllipse);
				geometry.Children.Add(EndEllipse);
			}
		}

		private GeometryGroup GetGeometry(ApplicationColor color)
		{
			GeometryGroup geometry;

			switch (color)
			{
				case ApplicationColor.XAxis:
					geometry = ImageViewModel.XAxisLinesGeometry;
					break;
				case ApplicationColor.YAxis:
					geometry = ImageViewModel.YAxisLinesGeometry;
					break;
				case ApplicationColor.ZAxis:
					geometry = ImageViewModel.ZAxisLinesGeometry;
					break;
				case ApplicationColor.Model:
					geometry = ImageViewModel.ModelLinesGeometry;
					break;
				default:
					throw new ArgumentException("Unknown application color.");
			}

			return geometry;
		}
	}
}
