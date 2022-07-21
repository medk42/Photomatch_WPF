using PhotomatchCore.Logic.Perspective;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Gui.GuiControls.ToolHandlers.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers
{
	
	/// <summary>
	/// Class representing a direction in 3d space for EdgeHandler.
	/// </summary>
	public class VectorDirection
	{
		/// <summary>
		/// Color of this direction.
		/// </summary>
		public ApplicationColor EdgeColor { get; private set; }

		/// <summary>
		/// The actual direction.
		/// </summary>
		public Vector3 Direction { get; private set; }

		private PerspectiveData Perspetive;

		/// <param name="perspetive">For converting between world and screen space.</param>
		public VectorDirection(Vector3 direction, PerspectiveData perspetive, ApplicationColor axisColor)
		{
			Direction = direction;
			Perspetive = perspetive;
			EdgeColor = axisColor;
		}

		/// <summary>
		/// Find a closest point on a ray specified by point "from" and direction "Direction" 
		/// to "mouseCoord" on screen or to a ray created from "mouseCoord" in 3d space.
		/// </summary>
		/// <returns>Class representing the closest point.</returns>
		public ModelCreationEdgeHandlerDirectionProjection Project(Vector3 from, Vector2 mouseCoord)
		{
			Ray2D screenRay = new Line2D(Perspetive.WorldToScreen(from), Perspetive.WorldToScreen(from + Direction)).AsRay();
			Vector2Proj screenProject = Intersections2D.ProjectVectorToRay(mouseCoord, screenRay);

			ClosestPoint3D worldClosestPoint = Intersections3D.GetRayRayClosest(new Ray3D(from, Direction), Perspetive.ScreenToWorldRay(mouseCoord));

			return new ModelCreationEdgeHandlerDirectionProjection()
			{
				ProjectedScreen = screenProject.Projection,
				DistanceScreen = screenProject.Distance,
				ProjectedWorld = worldClosestPoint.RayAClosest,
				DistanceWorld = worldClosestPoint.Distance,
				Direction = Direction
			};
		}
	}
}
