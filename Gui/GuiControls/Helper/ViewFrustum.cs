using Photomatch.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch.Gui.GuiControls.Helper
{
	public class ViewFrustum
	{
		private PerspectiveData Perspective;

		private Plane3D Left, Top, Right, Bottom;

		public ViewFrustum(PerspectiveData perspective)
		{
			this.Perspective = perspective;
			UpdateFrustum();
		}

		public void UpdateFrustum()
		{
			Ray3D topLeft = Perspective.ScreenToWorldRay(new Vector2(0, 0));
			Ray3D topRight = Perspective.ScreenToWorldRay(new Vector2(Perspective.Image.Width - 1, 0));
			Ray3D bottomLeft = Perspective.ScreenToWorldRay(new Vector2(0, Perspective.Image.Height - 1));
			Ray3D bottomRight = Perspective.ScreenToWorldRay(new Vector2(Perspective.Image.Width - 1, Perspective.Image.Height - 1));

			Left = new Plane3D(topLeft.Start, -Vector3.Cross(topLeft.Direction, bottomLeft.Start - topLeft.Start));
			Bottom = new Plane3D(bottomLeft.Start, -Vector3.Cross(bottomLeft.Direction, bottomRight.Start - bottomLeft.Start));
			Right = new Plane3D(bottomRight.Start, -Vector3.Cross(bottomRight.Direction, topRight.Start - bottomRight.Start));
			Top = new Plane3D(topRight.Start, -Vector3.Cross(topRight.Direction, topLeft.Start - topRight.Start));
		}

		public bool IsVectorInside(Vector3 vector)
		{
			return
				Intersections3D.ProjectVectorToPlane(vector, Left).SignedDistance >= -1e-6 &&
				Intersections3D.ProjectVectorToPlane(vector, Bottom).SignedDistance >= -1e-6 &&
				Intersections3D.ProjectVectorToPlane(vector, Right).SignedDistance >= -1e-6 &&
				Intersections3D.ProjectVectorToPlane(vector, Top).SignedDistance >= -1e-6;
		}

		private Vector3 GetClosestIntersection(Ray3D ray)
		{
			Vector3 closest = Vector3.InvalidInstance;
			double closestDistance = 0;

			foreach (Plane3D plane in new Plane3D[] { Left, Bottom, Right, Top })
			{
				if (Intersections3D.ProjectVectorToPlane(ray.Start, plane).SignedDistance >= 0)
				{
					RayPlaneIntersectionPoint intersection = Intersections3D.GetRayPlaneIntersection(ray, plane);
					if (intersection.RayRelative >= 0)
					{
						if ((!closest.Valid) || intersection.RayRelative < closestDistance)
						{
							closest = intersection.Intersection;
							closestDistance = intersection.RayRelative;
						}
					}
				}
			}

			return closest;
		}

		public Line3D ClipLine(Line3D line)
		{
			bool startIn = IsVectorInside(line.Start);
			bool endIn = IsVectorInside(line.End);

			Ray3D startRay = new Ray3D(line.Start, line.End - line.Start);
			Ray3D endRay = new Ray3D(line.End, line.Start - line.End);

			if (startIn && endIn)
				return line;
			else if (startIn && !endIn)
				return line.WithEnd(GetClosestIntersection(startRay));
			else if (!startIn && endIn)
				return line.WithStart(GetClosestIntersection(endRay));
			else
			{
				Line3D clippedLine = new Line3D(GetClosestIntersection(endRay), GetClosestIntersection(startRay));
				if (clippedLine.Start.Valid && clippedLine.End.Valid && IsVectorInside(clippedLine.Start) && IsVectorInside(clippedLine.End))
					return clippedLine;
				else
					return new Line3D(Vector3.InvalidInstance, Vector3.InvalidInstance);
			}
		}
	}
}
