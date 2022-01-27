using System;
using System.Collections.Generic;
using System.IO;

using Photomatch_ProofOfConcept_WPF.Utilities;

namespace Photomatch_ProofOfConcept_WPF.Logic
{
	public struct Line2D : ISafeSerializable<Line2D>
	{
		public Vector2 Start { get; set; }
		public Vector2 End { get; set; }

		public double Length => (Start - End).Magnitude;

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

		/// <summary>
		/// Return whether a point is inside a polygon.
		/// </summary>
		/// <param name="vertices">Vertices defining the polygon.</param>
		public static bool IsPointInsidePolygon(Vector2 point, List<Vector2> vertices)
		{
			int crossings = 0;
			Line2D ray = new Line2D(point, point + new Vector2(1, 0));
			for (int i = 0; i < vertices.Count; i++)
			{
				Line2D edge;
				if (i < vertices.Count - 1)
					edge = new Line2D(vertices[i], vertices[i + 1]);
				else
					edge = new Line2D(vertices[i], vertices[0]);
				IntersectionPoint2D intersection = GetLineLineIntersection(ray, edge);
				if (intersection.LineBRelative >= 0 && intersection.LineBRelative <= 1 && intersection.LineARelative >= 0)
					crossings++;
			}

			return crossings % 2 == 1;
		}

		/// <summary>
		/// Returns whether is a specified point on the right side of a line (specified by its endpoints).
		/// </summary>
		private static bool IsRight(Vector2 lineStart, Vector2 lineEnd, Vector2 point)
		{
			Vector2 startEnd = lineEnd - lineStart;
			Vector2 startPoint = point -  lineStart;

			double cross = startEnd.X * startPoint.Y - startEnd.Y * startPoint.X;

			return cross <= 0;
		}

		/// <summary>
		/// Return whether a point is inside a triangle (without potential issues at vertices which might happen with IsPointInsidePolygon).
		/// "point" defines point and "a", "b", "c" are triangle vertices.
		/// </summary>
		public static bool IsPointInsideTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
		{
			bool leftHanded = IsRight(a, b, c);
			if (leftHanded)
			{
				return IsRight(a, b, point) && IsRight(b, c, point) && IsRight(c, a, point);
			}
			else
			{
				return IsRight(a, c, point) && IsRight(c, b, point) && IsRight(b, a, point);
			}
		}

		/// <summary>
		/// Returns whether a polygon defined by vertices is clockwise. Supports non-convex polygons.
		/// Based on https://stackoverflow.com/questions/1165647/how-to-determine-if-a-list-of-polygon-points-are-in-clockwise-order.
		/// </summary>
		public static bool IsClockwise(List<Vector2> vertices)
		{
			double sum = 0;
			for (int i = 0; i < vertices.Count; i++)
			{
				Vector2 start = vertices[i > 0 ? i - 1 : vertices.Count - 1];
				Vector2 end = vertices[i];

				sum += (end.X - start.X) * (end.Y + start.Y);
			}

			return sum > 0;
		}
	}

	public struct Line3D : ISafeSerializable<Line3D>
	{
		public Vector3 Start { get; set; }
		public Vector3 End { get; set; }

		public double Length => (Start - End).Magnitude;

		public Line3D(Vector3 Start, Vector3 End)
		{
			this.Start = Start;
			this.End = End;
		}

		public Line3D WithStart(Vector3 NewStart) => new Line3D(NewStart, this.End);
		public Line3D WithEnd(Vector3 NewEnd) => new Line3D(this.Start, NewEnd);

		public Ray3D AsRay() => new Ray3D(Start, End - Start);

		public void Serialize(BinaryWriter writer)
		{
			Start.Serialize(writer);
			End.Serialize(writer);
		}

		public void Deserialize(BinaryReader reader)
		{
			Start = ISafeSerializable<Vector3>.CreateDeserialize(reader);
			End = ISafeSerializable<Vector3>.CreateDeserialize(reader);
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

		public Line3D AsLine() => new Line3D(Start, Start + Direction);

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

	public struct Plane3D
	{
		public Vector3 PlanePoint { get; set; }

		private Vector3 _Normal;
		public Vector3 Normal
		{
			get => _Normal;
			set => _Normal = value.Normalized();
		}

		public Plane3D(Vector3 planePoint, Vector3 normal) : this()
		{
			this.PlanePoint = planePoint;
			this.Normal = normal;
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

	public struct RayPlaneIntersectionPoint
	{
		/// <summary>
		/// The point of intersection.
		/// </summary>
		public Vector3 Intersection { get; set; }

		/// <summary>
		/// Relative position of the intersection point on the ray.
		/// </summary>
		public double RayRelative { get; set; }
	}

	public struct RayPolygonIntersectionPoint
	{
		/// <summary>
		/// Point of intersection of the ray with the plane defined by the normal and the first vertex of the polygon.
		/// </summary>
		public Vector3 Intersection { get; set; }

		/// <summary>
		/// Relative position of the intersection point on the ray.
		/// </summary>
		public double RayRelative { get; set; }

		/// <summary>
		/// true if ray intersected the polygon, false otherwise.
		/// </summary>
		public bool IntersectedPolygon { get; set; }
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

		/// <summary>
		/// Get intersection between ray and a plane (source https://en.wikipedia.org/wiki/Line%E2%80%93plane_intersection).
		/// </summary>
		public static RayPlaneIntersectionPoint GetRayPlaneIntersection(Ray3D ray, Plane3D plane)
		{
			double d = Vector3.Dot(plane.PlanePoint - ray.Start, plane.Normal) / Vector3.Dot(ray.Direction, plane.Normal);
			return new RayPlaneIntersectionPoint() { Intersection = ray.Start + (d * ray.Direction), RayRelative = d };
		}

		/// <summary>
		/// Get intersection between ray and a polygon (which has to lay in one plane defined by normal).
		/// </summary>
		/// <param name="vertices">Polygon vertices.</param>
		/// <param name="normal">Normal defining polygon plane.</param>
		public static RayPolygonIntersectionPoint GetRayPolygonIntersection(Ray3D ray, List<Vector3> vertices, Vector3 normal)
		{
			RayPlaneIntersectionPoint planeIntersectionPoint = GetRayPlaneIntersection(ray, new Plane3D(vertices[0], normal));

			Matrix3x3 rotateMatrix = Camera.RotateAlign(normal, new Vector3(0, 0, 1));

			Vector3 rotated = rotateMatrix * planeIntersectionPoint.Intersection;
			Vector2 planePoint = new Vector2(rotated.X, rotated.Y);

			List<Vector2> planeVertices = new List<Vector2>();
			foreach (Vector3 vertex in vertices)
			{
				rotated = rotateMatrix * vertex;
				planeVertices.Add(new Vector2(rotated.X, rotated.Y));
			}

			bool inside = Intersections2D.IsPointInsidePolygon(planePoint, planeVertices);

			return new RayPolygonIntersectionPoint() { Intersection = planeIntersectionPoint.Intersection, RayRelative = planeIntersectionPoint.RayRelative, IntersectedPolygon = inside };
		}
	}
}
