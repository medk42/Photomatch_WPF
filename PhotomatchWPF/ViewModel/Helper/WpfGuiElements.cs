using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using PhotomatchCore.Gui;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using PhotomatchCore.Utilities;

namespace PhotomatchWPF.ViewModel.Helper
{

	/// <summary>
	/// Abstract class containing useful methods for other gui elements.
	/// </summary>
	public abstract class WpfGuiElement
	{
		/// <summary>
		/// Set color of the GUI element (by removing geometry from the geometry group representing old color and adding it to the one representing new color).
		/// </summary>
		public virtual ApplicationColor Color
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
		private protected ApplicationColor Color_;

		/// <summary>
		/// Set visibility of the GUI element (set to false to remove geometry from its current geometry group or true to add it to the geometry group based on Color).
		/// </summary>
		public virtual bool Visible
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
		private protected bool Visible_ = true;

		/// <summary>
		/// Get geometry group from specified ImageViewModel for a specified color.
		/// </summary>
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
				case ApplicationColor.Selected:
					geometry = imageViewModel.SelectedLinesGeometry;
					break;
				case ApplicationColor.Face:
					geometry = imageViewModel.FaceLinesGeometry;
					break;
				case ApplicationColor.Highlight:
					geometry = imageViewModel.HighlightLinesGeometry;
					break;
				case ApplicationColor.Vertex:
					geometry = imageViewModel.VertexLinesGeometry;
					break;
				case ApplicationColor.Midpoint:
					geometry = imageViewModel.MidpointLinesGeometry;
					break;
				case ApplicationColor.Edgepoint:
					geometry = imageViewModel.EdgepointLinesGeometry;
					break;
				case ApplicationColor.Invalid:
					geometry = imageViewModel.InvalidLinesGeometry;
					break;
				case ApplicationColor.NormalLine:
					geometry = imageViewModel.NormalLinesGeometry;
					break;
				case ApplicationColor.NormalInside:
					geometry = imageViewModel.NormalInsideLinesGeometry;
					break;
				case ApplicationColor.NormalOutside:
					geometry = imageViewModel.NormalOutsideLinesGeometry;
					break;
				case ApplicationColor.XAxisDotted:
					geometry = imageViewModel.XAxisDottedLinesGeometry;
					break;
				case ApplicationColor.YAxisDotted:
					geometry = imageViewModel.YAxisDottedLinesGeometry;
					break;
				case ApplicationColor.ZAxisDotted:
					geometry = imageViewModel.ZAxisDottedLinesGeometry;
					break;
				default:
					throw new ArgumentException("Unknown application color.");
			}

			return geometry;
		}

		/// <summary>
		/// Remove geometry from the geometry group specified by color.
		/// </summary>
		private protected abstract void RemoveOldColor(ApplicationColor color);

		/// <summary>
		/// Add geometry to the geometry group specified by color.
		/// </summary>
		private protected abstract void AddNewColor(ApplicationColor color);
	}

	public static class ApplicationColorBrushes
	{
		private static SolidColorBrush FaceBrush = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255));

		/// <summary>
		/// Get brush for a specified color.
		/// </summary>
		public static Brush GetColor(ApplicationColor color)
		{
			switch (color)
			{
				case ApplicationColor.XAxis:
					return Brushes.Red;
				case ApplicationColor.YAxis:
					return Brushes.Green;
				case ApplicationColor.ZAxis:
					return Brushes.Blue;
				case ApplicationColor.Model:
					return Brushes.Cyan;
				case ApplicationColor.Selected:
					return Brushes.Orange;
				case ApplicationColor.Face:
					return FaceBrush;
				case ApplicationColor.Highlight:
					return Brushes.SpringGreen;
				case ApplicationColor.Vertex:
					return Brushes.Black;
				case ApplicationColor.Midpoint:
					return Brushes.Gold;
				case ApplicationColor.Edgepoint:
					return Brushes.White;
				case ApplicationColor.Invalid:
					return Brushes.Red;
				case ApplicationColor.NormalLine:
					return Brushes.Black;
				case ApplicationColor.NormalInside:
					return Brushes.Red;
				case ApplicationColor.NormalOutside:
					return Brushes.Green;
				case ApplicationColor.XAxisDotted:
					return Brushes.Red;
				case ApplicationColor.YAxisDotted:
					return Brushes.Green;
				case ApplicationColor.ZAxisDotted:
					return Brushes.Blue;
				default:
					throw new ArgumentException("Unknown application color.");
			}
		}
	}

	/// <summary>
	/// Class implementing a visualization of a polygon in WPF.
	/// </summary>
	public class WpfPolygon : IPolygon
	{
		private ApplicationColor Color_;
		public ApplicationColor Color
		{
			get => Color_;
			set
			{
				if (value != Color_)
				{
					Color_ = value;
					SetColor(value);
				}
			}
		}

		private bool Visible_ = true;
		public bool Visible
		{
			get => Visible_;
			set
			{
				if (value != Visible_)
				{
					Visible_ = value;
					if (value)
						Polygon.Visibility = Visibility.Visible;
					else
						Polygon.Visibility = Visibility.Collapsed;
				}
			}
		}

		public int Count => Polygon.Points.Count;

		public Vector2 this[int i]
		{
			get => Polygon.Points[i].AsVector2();
			set => Polygon.Points[i] = value.AsPoint();
		}

		private ObservableCollection<Polygon> Polygons;
		private Polygon Polygon;

		/// <summary>
		/// Create a polygon.
		/// </summary>
		/// <param name="polygons">Reference to a collection which contains displayed polygons.</param>
		/// <param name="color">Chosen color of the polygon.</param>
		public WpfPolygon(ObservableCollection<Polygon> polygons, ApplicationColor color)
		{
			Polygons = polygons;

			Polygon = new Polygon();
			Polygons.Add(Polygon);

			Color_ = color;
			SetColor(Color_);
		}

		/// <summary>
		/// Change the fill color of the polygon.
		/// </summary>
		private void SetColor(ApplicationColor color)
		{
			Polygon.Fill = ApplicationColorBrushes.GetColor(color);
		}

		public void Add(Vector2 vertex)
		{
			Polygon.Points.Add(vertex.AsPoint());
		}

		public void Remove(int index)
		{
			Polygon.Points.RemoveAt(index);
		}

		public void Dispose()
		{
			Polygons.Remove(Polygon);
			Polygons = null;
			Polygon = null;
		}
	}

	/// <summary>
	/// Class implementing a visualization of a line with endpoints in WPF.
	/// </summary>
	public class WpfLine : WpfGuiElement, ILine, IScalable
	{
		/// <summary>
		/// Geometry for the line.
		/// </summary>
		public LineGeometry Line { get; private set; }

		/// <summary>
		/// Geometry for the first endpoint.
		/// </summary>
		public EllipseGeometry StartEllipse { get; private set; }

		/// <summary>
		/// Geometry for the second endpoint.
		/// </summary>
		public EllipseGeometry EndEllipse { get; private set; }

		private double EndRadius;
		private ImageViewModel ImageViewModel;

		/// <summary>
		/// Clip line on set.
		/// </summary>
		public Vector2 Start
		{
			get => Start_;
			set
			{
				Start_ = value;
				UpdateDrawnLine();
			}
		}
		private Vector2 Start_;

		/// <summary>
		/// Clip line on set.
		/// </summary>
		public Vector2 End
		{
			get => End_;
			set
			{
				End_ = value;
				UpdateDrawnLine();
			}
		}
		private Vector2 End_;

		/// <summary>
		/// True if the line is not above the image even partly.
		/// </summary>
		private bool OutsideView_ = false;

		/// <summary>
		/// Controls whether the line is displayed (line is displayed based on Visible if line is at least 
		/// partly above the image, otherwise it will not be visible).
		/// </summary>
		public override bool Visible
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

		/// <summary>
		/// Create a line.
		/// </summary>
		/// <param name="Start">First endpoint.</param>
		/// <param name="End">Second endpoint.</param>
		/// <param name="endRadius">Radius of the endpoints (there will no endpoints created if 0).</param>
		/// <param name="imageViewModel">Reference to ImageViewModel, so that the line can be displayed.</param>
		/// <param name="color">Chosen color of the line.</param>
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

		/// <summary>
		/// Scale the endpoints.
		/// </summary>
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

		/// <summary>
		/// Clip the line to the image borders.
		/// </summary>
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

		/// <summary>
		/// Return "point" if it is inside image borders or the point closest to it on a ray from "rayOrigin" to "point".
		/// </summary>
		private Vector2 LimitPointRayToImageBox(Vector2 point, Vector2 rayOrigin)
		{
			double limit = 5;
			if (point.X >= ImageViewModel.Width - limit || point.Y >= ImageViewModel.Height - limit || point.X < limit || point.Y < limit)
				return Intersections2D.GetRayInsideBoxIntersection(new Line2D(rayOrigin, point).AsRay(), new Vector2(limit, limit), new Vector2(ImageViewModel.Width - limit, ImageViewModel.Height - limit));

			return point;
		}

		private protected override void RemoveOldColor(ApplicationColor color)
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

		private protected override void AddNewColor(ApplicationColor color)
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
			ImageViewModel.Scalables.Remove(this);
		}
	}

	/// <summary>
	/// Class implementing a visualization of an ellipse in WPF.
	/// </summary>
	public class WpfEllipse : WpfGuiElement, IEllipse, IScalable
	{
		/// <summary>
		/// Geometry for the ellipse.
		/// </summary>
		public EllipseGeometry Ellipse { get; private set; }

		private double Radius;
		private ImageViewModel ImageViewModel;

		public Vector2 Position
		{
			get => Ellipse.Center.AsVector2();
			set
			{
				var point = value.AsPoint();
				Ellipse.Center = point;

				if (Visible)
				{
					if (WasInside_ && !IsInside)
						RemoveOldColor(Color);
					else if (!WasInside_ && IsInside)
						AddNewColor(Color);
				}
				WasInside_ = IsInside;
			}
		}

		/// <summary>
		/// Contains the result of IsInside call after last position changed.
		/// In other words, whether the ellipse is inside image borders.
		/// </summary>
		private bool WasInside_ = false;

		/// <summary>
		/// Controls whether the ellipse is displayed (only if the ellipse is inside image borders, 
		/// if it is not, it is not displayed).
		/// </summary>
		public override bool Visible
		{
			get => Visible_;
			set
			{
				if (Visible_ != value)
				{
					if (WasInside_)
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

		/// <summary>
		/// True if ellipse is currently inside image borders.
		/// </summary>
		private bool IsInside
		{
			get
			{
				return
					Position.X - Radius > 0 &&
					Position.Y - Radius > 0 &&
					Position.X + Radius < ImageViewModel.Width &&
					Position.Y + Radius < ImageViewModel.Height;
			}
		}


		/// <summary>
		/// Create an ellipse.
		/// </summary>
		/// <param name="position">Ellipse position.</param>
		/// <param name="radius">Radius of the ellipse.</param>
		/// <param name="imageViewModel">Reference to ImageViewModel, so that the ellipse can be displayed.</param>
		/// <param name="color">Chosen color of the ellipse.</param>
		public WpfEllipse(Point position, double radius, ImageViewModel imageViewModel, ApplicationColor color)
		{
			Radius = radius;
			ImageViewModel = imageViewModel;
			Color_ = color;

			Ellipse = new EllipseGeometry(position, Radius, Radius);

			WasInside_ = IsInside;
			if (WasInside_)
				AddNewColor(Color);
		}


		/// <summary>
		/// Scale the ellipse (radius).
		/// </summary>
		public void SetNewScale(double scale)
		{
			Ellipse.RadiusX = Radius / scale;
			Ellipse.RadiusY = Radius / scale;
		}

		private protected override void RemoveOldColor(ApplicationColor color)
		{
			GeometryGroup geometry = GetGeometry(color, ImageViewModel);
			geometry.Children.Remove(Ellipse);
		}

		private protected override void AddNewColor(ApplicationColor color)
		{
			GeometryGroup geometry = GetGeometry(color, ImageViewModel);
			geometry.Children.Add(Ellipse);
		}

		public void Dispose()
		{
			RemoveOldColor(Color);
			Ellipse = null;
			ImageViewModel.Scalables.Remove(this);
		}
	}
}
