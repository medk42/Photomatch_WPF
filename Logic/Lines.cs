using MatrixVector;
using System;

namespace Lines
{
	public struct Line2D
	{
		public Vector2 Start { get; set; }
		public Vector2 End { get; set; }

		public Line2D(Vector2 Start, Vector2 End)
		{
			this.Start = Start;
			this.End = End;
		}

		public Line2D WithStart(Vector2 NewStart) => new Line2D(NewStart, this.End);
		public Line2D WithEnd(Vector2 NewEnd) => new Line2D(this.Start, NewEnd);

		public Ray2D AsRay() => new Ray2D(Start, End - Start);
	}

	public struct Ray2D
	{
		public Vector2 Start { get; set; }

		private Vector2 _direction;
		public Vector2 Direction
		{
			get => _direction;
			set => _direction = value.Normalized();
		}

		public Ray2D(Vector2 Start, Vector2 Direction) : this()
		{
			this.Start = Start;
			this.Direction = Direction;
		}

		public Ray2D WithStart(Vector2 NewStart) => new Ray2D(NewStart, this.Direction);
		public Ray2D WithDirection(Vector2 NewDirection) => new Ray2D(this.Start, NewDirection);

		public Line2D AsLine() => new Line2D(Start, Start + Direction);
	}

	public struct IntersectionPoint2D
	{
		/// <summary>
		/// The point of intersection between the two lines, may be out of bounds of either line.
		/// </summary>
		public Vector2 Intersection { get; set; }

		/// <summary>
		/// Relative position of the intersection on the first line (0 means start, 1 means end)
		/// </summary>
		public double LineARelative { get; set; }

		/// <summary>
		/// Relative position of the intersection on the second line. (0 means start, 1 means end)
		/// </summary>
		public double LineBRelative { get; set; }

		/// <summary>
		/// Create intersection point with specified parameters.
		/// </summary>
		/// <param name="intersection">The point of intersection between the two lines, may be out of bounds of either line.</param>
		/// <param name="lineARelative">Relative position of the intersection on the first line (0 means start, 1 means end)</param>
		/// <param name="lineBRelative">Relative position of the intersection on the second line. (0 means start, 1 means end)</param>
		public IntersectionPoint2D(Vector2 intersection, double lineARelative, double lineBRelative)
		{
			Intersection = intersection;
			LineARelative = lineARelative;
			LineBRelative = lineBRelative;
		}
	}

	public static class Intersections2D
	{
		/// <summary>
		/// Get the point of intersection between two lines using line-line intersection (https://en.wikipedia.org/wiki/Line%E2%80%93line_intersection)
		/// </summary>
		/// <param name="LineA">First line.</param>
		/// <param name="LineB">Second line.</param>
		/// <returns>
		///	IntersectionPoint2D struct containing 3 values:
		///		The point of intersection between the two lines, may be out of bounds of either line.
		///		Relative position of the intersection on the first line (0 means start, 1 means end)
		///		Relative position of the intersection on the second line. (0 means start, 1 means end)
		///	</returns>
		public static IntersectionPoint2D GetLineLineIntersection(Line2D LineA, Line2D LineB)
		{
			double x1 = LineA.Start.X;
			double y1 = LineA.Start.Y;
			double x2 = LineA.End.X;
			double y2 = LineA.End.Y;

			double x3 = LineB.Start.X;
			double y3 = LineB.Start.Y;
			double x4 = LineB.End.X;
			double y4 = LineB.End.Y;

			double denominator = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);

			double t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denominator;
			double u = ((y1 - y2) * (x1 - x3) - (x1 - x2) * (y1 - y3)) / denominator;

			return new IntersectionPoint2D(new Vector2(x1 + t * (x2 - x1), y1 + t * (y2 - y1)), t, u);
		}

		/// <summary>
		/// Get the point of intersection between a ray and a box specified by "top-left" (smaller X and Y) and
		/// "bottom-right" (bigger X and Y) corners. If the ray is outside the box, return ray origin.
		/// </summary>
		/// <param name="corner1">Corner with smaller X and Y.</param>
		/// <param name="corner2">Corner with bigger X and Y.</param>
		/// <returns>Point of intersection between the ray and the box or ray origin if ray is outside the box</returns>
		public static Vector2 GetRayInsideBoxIntersection(Ray2D ray, Vector2 corner1, Vector2 corner2)
		{
			if (corner1.X > corner2.X || corner1.Y > corner2.Y)
				throw new ArgumentException("\"corner1\" needs to have smaller coordinates than \"corner2\".");

			Vector2 topLeft = new Vector2(corner1.X, corner1.Y);
			Vector2 topRight = new Vector2(corner2.X, corner1.Y);
			Vector2 bottomLeft = new Vector2(corner1.X, corner2.Y);
			Vector2 bottomRight = new Vector2(corner2.X, corner2.Y);

			Line2D top = new Line2D(topLeft, topRight);
			Line2D bottom = new Line2D(bottomLeft, bottomRight);
			Line2D left = new Line2D(topLeft, bottomLeft);
			Line2D right = new Line2D(topRight, bottomRight);

			Line2D rayLine = ray.AsLine();

			if (ray.Start.Y >= corner1.Y)
			{
				var intersection = Intersections2D.GetLineLineIntersection(rayLine, top);
				if (intersection.LineARelative >= 0 && (intersection.LineBRelative >= 0 && intersection.LineBRelative <= 1))
					return intersection.Intersection;
			}

			if (ray.Start.Y < corner2.Y)
			{
				var intersection = Intersections2D.GetLineLineIntersection(rayLine, bottom);
				if (intersection.LineARelative >= 0 && (intersection.LineBRelative >= 0 && intersection.LineBRelative <= 1))
					return intersection.Intersection;
			}

			if (ray.Start.X >= corner1.X)
			{
				var intersection = Intersections2D.GetLineLineIntersection(rayLine, left);
				if (intersection.LineARelative >= 0 && (intersection.LineBRelative >= 0 && intersection.LineBRelative <= 1))
					return intersection.Intersection;
			}

			if (ray.Start.X < corner2.X)
			{
				var intersection = Intersections2D.GetLineLineIntersection(rayLine, right);
				if (intersection.LineARelative >= 0 && (intersection.LineBRelative >= 0 && intersection.LineBRelative <= 1))
					return intersection.Intersection;
			}

			return ray.Start;
		}
	}
}
