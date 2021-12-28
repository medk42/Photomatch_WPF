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
	public class WpfGuiElement
	{
		protected GeometryGroup GetGeometry(ApplicationColor color, ImageViewModel imageViewModel)
		{
			GeometryGroup geometry;

			switch (color)
			{
				case ApplicationColor.XAxis:
					geometry = imageViewModel.XAxisLinesGeometry;
					break;
				case ApplicationColor.YAxis:
					geometry = imageViewModel.YAxisLinesGeometry;
					break;
				case ApplicationColor.ZAxis:
					geometry = imageViewModel.ZAxisLinesGeometry;
					break;
				case ApplicationColor.Model:
					geometry = imageViewModel.ModelLinesGeometry;
					break;
				default:
					throw new ArgumentException("Unknown application color.");
			}

			return geometry;
		}
	}

	public class WpfLine : WpfGuiElement, ILine, IScalable
	{
		public LineGeometry Line { get; }
		public EllipseGeometry StartEllipse { get; }
		public EllipseGeometry EndEllipse { get; }

		private double EndRadius;
		private ImageViewModel ImageViewModel;

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

		private ApplicationColor Color_;
		public ApplicationColor Color
		{
			get => Color_;
			set
			{
				if (Color_ != value)
				{
					RemoveOldColor(Color_);
					AddNewColor(value);
					Color_ = value;
				}
			}
		}

		public WpfLine(Point Start, Point End, double endRadius, ImageViewModel imageViewModel, ApplicationColor color)
		{
			Line = new LineGeometry();
			Line.StartPoint = Start;
			Line.EndPoint = End;

			EndRadius = endRadius;
			ImageViewModel = imageViewModel;
			Color_ = color;

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

		private void RemoveOldColor(ApplicationColor color)
		{
			GeometryGroup geometry = GetGeometry(color, ImageViewModel);
			geometry.Children.Remove(Line);
			if (StartEllipse != null && EndEllipse != null)
			{
				geometry.Children.Remove(StartEllipse);
				geometry.Children.Remove(EndEllipse);
			}
		}

		private void AddNewColor(ApplicationColor color)
		{
			GeometryGroup geometry = GetGeometry(color, ImageViewModel);

			geometry.Children.Add(Line);
			if (StartEllipse != null && EndEllipse != null)
			{
				geometry.Children.Add(StartEllipse);
				geometry.Children.Add(EndEllipse);
			}
		}
	}

	public class WpfEllipse : WpfGuiElement, IEllipse, IScalable
	{
		public EllipseGeometry Ellipse { get; }

		private double Radius;
		private ImageViewModel ImageViewModel;

		public Vector2 Position
		{
			get => Ellipse.Center.AsVector2();
			set
			{
				var point = value.AsPoint();
				Ellipse.Center = point;
			}
		}

		private bool Visible_ = true;
		public bool Visible
		{
			get => Visible_;
			set
			{
				if (Visible_ != value)
				{
					if (Visible_)
					{
						RemoveOldColor(Color);
					}
					else
					{
						AddNewColor(Color);
					}

					Visible_ = value;
				}
			}
		}

		private ApplicationColor Color_;
		public ApplicationColor Color
		{
			get => Color_;
			set
			{
				if (Color_ != value)
				{
					if (Visible)
					{
						RemoveOldColor(Color_);
						AddNewColor(value);
					}
					Color_ = value;
				}
			}
		}

		public WpfEllipse(Point position, double radius, ImageViewModel imageViewModel, ApplicationColor color)
		{
			Radius = radius;
			ImageViewModel = imageViewModel;
			Color_ = color;

			Ellipse = new EllipseGeometry(position, Radius, Radius);
			AddNewColor(Color);
		}

		public void SetNewScale(double scale)
		{
			Ellipse.RadiusX = Radius / scale;
			Ellipse.RadiusY = Radius / scale;
		}

		private void RemoveOldColor(ApplicationColor color)
		{
			GeometryGroup geometry = GetGeometry(color, ImageViewModel);
			geometry.Children.Remove(Ellipse);
		}

		private void AddNewColor(ApplicationColor color)
		{
			GeometryGroup geometry = GetGeometry(color, ImageViewModel);
			geometry.Children.Add(Ellipse);
		}
	}
}
