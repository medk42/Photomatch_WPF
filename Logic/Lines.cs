using MatrixVector;
using System;
using Serializables;
using System.IO;
using Photomatch_ProofOfConcept_WPF.Logic;

namespace Lines
{
	public struct Line2D : ISafeSerializable<Line2D>
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

		public void Serialize(BinaryWriter writer)
		{
			Start.Serialize(writer);
			End.Serialize(writer);
		}

		public void Deserialize(BinaryReader reader)
		{
			Start = ISafeSerializable<Vector2>.CreateDeserialize(reader);
			End = ISafeSerializable<Vector2>.CreateDeserialize(reader);
		}
	}

	public struct Ray2D : ISafeSerializable<Ray2D>
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


		public void Serialize(BinaryWriter writer)
		{
			Start.Serialize(writer);
			Direction.Serialize(writer);
		}

		public void Deserialize(BinaryReader reader)
		{
			Start = ISafeSerializable<Vector2>.CreateDeserialize(reader);
			Direction = ISafeSerializable<Vector2>.CreateDeserialize(reader);
		}
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

	public struct Vector2Proj
	{
		/// <summary>
		/// The projected point.
		/// </summary>
		public Vector2 Projection { get; set; }

		/// <summary>
		/// Distance of the projected point from the original point.
		/// </summary>
		public double Distance { get; set; }

		/// <summary>
		/// Relative position of the projected point on the ray.
		/// </summary>
		public double RayRelative { get; set; }
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

			return Vector2.InvalidInstance;
		}

		/// <summary>
		/// Get a projection of a point onto ray as a Vector2 on the ray.
		/// </summary>
		public static Vector2Proj ProjectVectorToRay(Vector2 vector, Ray2D ray)
		{
			double t = Vector2.Dot(vector - ray.Start, ray.Direction);
			Vector2 projected = ray.Start + t * ray.Direction;
			return new Vector2Proj() { Projection = projected, RayRelative = t, Distance = (projected - vector).Magnitude };
		}
	}

	public struct Ray3D : ISafeSerializable<Ray3D>
	{
		public Vector3 Start { get; set; }

		private Vector3 _direction;
		public Vector3 Direction
		{
			get => _direction;
			set => _direction = value.Normalized();
		}

		public Ray3D(Vector3 Start, Vector3 Direction) : this()
		{
			this.Start = Start;
			this.Direction = Direction;
		}

		public Ray3D WithStart(Vector3 NewStart) => new Ray3D(NewStart, this.Direction);
		public Ray3D WithDirection(Vector3 NewDirection) => new Ray3D(this.Start, NewDirection);

		public void Serialize(BinaryWriter writer)
		{
			Start.Serialize(writer);
			Direction.Serialize(writer);
		}

		public void Deserialize(BinaryReader reader)
		{
			Start = ISafeSerializable<Vector3>.CreateDeserialize(reader);
			Direction = ISafeSerializable<Vector3>.CreateDeserialize(reader);
		}
	}

	public struct ClosestPoint3D
	{
		/// <summary>
		/// The closest point between the two rays on first ray.
		/// </summary>
		public Vector3 RayAClosest { get; set; }

		/// <summary>
		/// The closest point between the two rays on second ray.
		/// </summary>
		public Vector3 RayBClosest { get; set; }

		/// <summary>
		/// Relative position of the closest point on the first ray (0 means start, 1 means at the end of the unit direction vector)
		/// </summary>
		public double RayARelative { get; set; }

		/// <summary>
		/// Relative position of the closest point on the second ray. (0 means start, 1 means at the end of the unit direction vector)
		/// </summary>
		public double RayBRelative { get; set; }

		/// <summary>
		/// Closest distance between two rays.
		/// </summary>
		public double Distance { get; set; }
	}

	public struct Vector3Proj
	{
		/// <summary>
		/// The projected point.
		/// </summary>
		public Vector3 Projection { get; set; }

		/// <summary>
		/// Distance of the projected point from the original point.
		/// </summary>
		public double Distance { get; set; }

		/// <summary>
		/// Relative position of the projected point on the ray.
		/// </summary>
		public double RayRelative { get; set; }
	}

	public static class Intersections3D
	{
		/// <summary>
		/// Get the closest point between two 3d rays.
		/// </summary>
		/// <param name="RayA">First ray.</param>
		/// <param name="RayB">Second ray.</param>
		/// <returns>
		///	IntersectionPoint3D struct containing 5 values:
		///		The closest point on first ray.
		///		The closest point on second ray.
		///		Relative position of the closest point on the first ray (0 means start, 1 means at the end of the unit direction vector).
		///		Relative position of the closest point on the second ray (0 means start, 1 means at the end of the unit direction vector).
		///		Distance between the two rays.
		///	</returns>
		public static ClosestPoint3D GetRayRayClosest(Ray3D RayA, Ray3D RayB)
		{
			/* L1 = RayA.Start + t1 * RayA.Direction
			 * L2 = RayB.Start + t2 * RayB.Direction
			 * L3 = RayA.Start + t1 * RayA.Direction + t3 * tangent
			 * L3 = L2  =>  RayA.Start + t1 * RayA.Direction + t3 * tangent = RayB.Start + t2 * RayB.Direction
			 * only t1, t2, t3 unknown, rest are known vectors with 3 components each => 3 equations, 3 unknowns
			 * t1 * RayA.Direction - t2 * RayB.Direction + t3 * tangent = RayB.Start - RayA.Start
			 */

			Vector3 tangent = Vector3.Cross(RayB.Direction, RayA.Direction).Normalized();
			Matrix3x3 leftHandSide = new Matrix3x3() { A_0 = RayA.Direction, A_1 = -RayB.Direction, A_2 = tangent };
			Vector3 rightHandSide = RayB.Start - RayA.Start;
			Vector3 solution = Solver.Solve(leftHandSide, rightHandSide);

			return new ClosestPoint3D() { 
				RayAClosest = RayA.Start + solution.X * RayA.Direction, 
				RayBClosest = RayB.Start + solution.Y * RayB.Direction,
				RayARelative = solution.X, 
				RayBRelative = solution.Y, 
				Distance = Math.Abs(solution.Z)
			};
		}

		/// <summary>
		/// Get a projection of a point onto ray as a Vector3 on the ray.
		/// </summary>
		public static Vector3Proj ProjectVectorToRay(Vector3 vector, Ray3D ray)
		{
			double t = Vector3.Dot(vector - ray.Start, ray.Direction);
			Vector3 projected = ray.Start + t * ray.Direction;
			return new Vector3Proj() { Projection = projected, RayRelative = t, Distance = (projected - vector).Magnitude };
		}
	}
}
