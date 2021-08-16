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
	}
}
