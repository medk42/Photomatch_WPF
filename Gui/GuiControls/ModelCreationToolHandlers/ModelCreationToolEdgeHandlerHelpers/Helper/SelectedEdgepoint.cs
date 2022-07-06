using Photomatch.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch.Gui.GuiControls.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers.Helper
{
	public class SelectedEdgepoint : IModelCreationEdgeHandlerVertex
	{
		private Model Model;
		private PerspectiveData Perspective;

		private Edge Edge;
		private Vector3 Edgepoint;
		private Vertex Vertex;

		public SelectedEdgepoint(Edge edge, Vector3 edgepoint, Model model, PerspectiveData perspective)
		{
			this.Edge = edge;
			this.Edgepoint = edgepoint;
			this.Model = model;
			this.Perspective = perspective;
		}

		public Vector2 ScreenPosition => Perspective.WorldToScreen(Edgepoint);

		public Vector3 WorldPosition => Edgepoint;

		public Vertex ModelVertex
		{
			get
			{
				if (Vertex == null)
					Vertex = Model.AddVertexToEdge(Edgepoint, Edge);
				return Vertex;
			}
		}

		public bool UpdateToHoldRay(Ray3D holdRay)
		{
			Line3D edge = new Line3D(Edge.Start.Position, Edge.End.Position);
			ClosestPoint3D closestPoint = Intersections3D.GetRayRayClosest(holdRay, edge.AsRay());

			if (closestPoint.Distance < 1e-6 && closestPoint.RayBRelative >= 0 && closestPoint.RayBRelative <= edge.Length)
			{
				Edgepoint = closestPoint.RayBClosest;
				return true;
			}

			return false;
		}
	}
}
