using Photomatch_ProofOfConcept_WPF.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers
{
	public class VectorDirection : IModelCreationEdgeHandlerDirection
	{
		public ApplicationColor EdgeColor { get; private set; }

		private Vector3 Direction;
		private PerspectiveData Perspetive;

		public VectorDirection(Vector3 direction, PerspectiveData perspetive, ApplicationColor axisColor)
		{
			this.Direction = direction;
			this.Perspetive = perspetive;
			this.EdgeColor = axisColor;
		}

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
