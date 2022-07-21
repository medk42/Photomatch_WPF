using PhotomatchCore.Logic.Model;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Gui.GuiControls.ToolHandlers.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers.Helper
{
	/// <summary>
	/// Class representing a found vertex on a model.
	/// </summary>
	public class SelectedVertex : IModelCreationEdgeHandlerVertex
	{
		/// <summary>
		/// Position of the vertex on the screen.
		/// </summary>
		public Vector2 ScreenPosition { get; set; }

		/// <summary>
		/// Position of the vertex in 3d space.
		/// </summary>
		public Vector3 WorldPosition { get; set; }

		/// <summary>
		/// The actual vertex.
		/// </summary>
		public Vertex ModelVertex { get; set; }

		/// <summary>
		/// If the holdRay intersects this vertex, select this vertex as the end
		/// instead of a closest point.
		/// </summary>
		public bool UpdateToHoldRay(Ray3D holdRay)
		{
			Vector3RayProj worldPosProject = Intersections3D.ProjectVectorToRay(WorldPosition, holdRay);

			if (worldPosProject.Distance <= 1e-6)
			{
				return true;
			}

			return false;
		}
	}
}
