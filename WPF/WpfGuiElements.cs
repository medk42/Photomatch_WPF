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
using Lines;

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
		public LineGeometry Line { get; private set; }
		public EllipseGeometry StartEllipse { get; private set; }
		public EllipseGeometry EndEllipse { get; private set; }

		private double EndRadius;
		private ImageViewModel ImageViewModel;

		private Vector2 Start_;
		public Vector2 Start
		{
			get => Start_; 
			set
			{
				Start_ = value;
				UpdateDrawnLine();
			}
		}

		private Vector2 End_;
		public Vector2 End
		{
			get => End_;
			set
			{
				End_ = value;
				UpdateDrawnLine();
			}
		}

		private bool OutsideView_ = false;
		private bool Visible_ = true;
		public bool Visible
		{
			get => Visible_;
			set
			{
				if (Visible_ != value)
				{
					if (!OutsideView_)
					{
						if (Visible_)
						{
							RemoveOldColor(Color);
						}
						else
						{
							AddNewColor(Color);
						}
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

		public WpfLine(Point Start, Point End, double endRadius, ImageViewModel imageViewModel, ApplicationColor color)
		{
			Line = new LineGeometry();

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

			this.Start = Start.AsVector2();
			this.End = End.AsVector2();
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

		private void UpdateDrawnLine()
		{
			Vector2 startVector = LimitPointRayToImageBox(Start, End);
			Vector2 endVector = LimitPointRayToImageBox(End, Start);

			if (!startVector.Valid || !endVector.Valid)
			{
				if (Visible)
					RemoveOldColor(Color);
				OutsideView_ = true;
				return;
			}
			else if (OutsideView_)
			{
				if (Visible)
					AddNewColor(Color);
				OutsideView_ = false;
			}

			Point start = startVector.AsPoint();
			Point end = endVector.AsPoint();

			if (Line != null)
			{
				Line.StartPoint = start;
				Line.EndPoint = end;
			}

			if (StartEllipse != null)
				StartEllipse.Center = start;
			if (EndEllipse != null)
				EndEllipse.Center = end;
		}

		private Vector2 LimitPointRayToImageBox(Vector2 point, Vector2 rayOrigin)
		{
			if (point.X >= ImageViewModel.Width || point.Y >= ImageViewModel.Height || point.X < 0 || point.Y < 0)
				return Intersections2D.GetRayInsideBoxIntersection(new Line2D(rayOrigin, point).AsRay(), new Vector2(), new Vector2(ImageViewModel.Width, ImageViewModel.Height));

			return point;
		}

		private void RemoveOldColor(ApplicationColor color)
		{
			GeometryGroup geometry = GetGeometry(color, ImageViewModel);
			if (Line != null)
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

			if (Line != null)
				geometry.Children.Add(Line);
			if (StartEllipse != null && EndEllipse != null)
			{
				geometry.Children.Add(StartEllipse);
				geometry.Children.Add(EndEllipse);
			}
		}

		public void Dispose()
		{
			RemoveOldColor(Color);
			Line = null;
			StartEllipse = null;
			EndEllipse = null;
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
