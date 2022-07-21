using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using PhotomatchCore.Logic.Model;
using PhotomatchCore.Logic.Perspective;

namespace PhotomatchCore.Gui.GuiControls.ToolHandlers.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers.Helper
{

	/// <summary>
	/// Class representing a found point on an edge of a model.
	/// </summary>
	public class SelectedEdgepoint : IModelCreationEdgeHandlerVertex
	{
		private Model Model;
		private PerspectiveData Perspective;

		private Edge Edge;
		private Vector3 Edgepoint;
		private Vertex Vertex;

		/// <param name="edge">Found edge.</param>
		/// <param name="edgepoint">Point on the edge in 3d space.</param>
		/// <param name="model">Model containing the edge.</param>
		/// <param name="perspective">Perspective for world to screen transformations.</param>
		public SelectedEdgepoint(Edge edge, Vector3 edgepoint, Model model, PerspectiveData perspective)
		{
			Edge = edge;
			Edgepoint = edgepoint;
			Model = model;
			Perspective = perspective;
		}

		/// <summary>
		/// WorldToScreen transformation of WorldPosition.
		/// </summary>
		public Vector2 ScreenPosition => Perspective.WorldToScreen(Edgepoint);

		/// <summary>
		/// Point in 3d space.
		/// </summary>
		public Vector3 WorldPosition => Edgepoint;

		/// <summary>
		/// Gets created on first call by splitting the edge into two at WorldPosition.
		/// </summary>
		public Vertex ModelVertex
		{
			get
			{
				if (Vertex == null)
					Vertex = Model.AddVertexToEdge(Edgepoint, Edge);
				return Vertex;
			}
		}

		/// <summary>
		/// If the holdRay intersects the edge which contains the selected point, change
		/// WorldPosition to the intersection and select this point instead of closest
		/// point.
		/// </summary>
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
