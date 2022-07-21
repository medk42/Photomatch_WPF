using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Gui.GuiControls.ToolHandlers.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers
{
	/// <summary>
	/// Class representing a closest point on ray made by VectorDirection.
	/// </summary>
	public class ModelCreationEdgeHandlerDirectionProjection
	{
		/// <summary>
		/// Closest (in 3d space) point on ray to a ray created from a point on screen.
		/// </summary>
		public Vector3 ProjectedWorld { get; set; }

		/// <summary>
		/// Closest (on 2d screen) point on a ray to a point on a screen.
		/// </summary>
		public Vector2 ProjectedScreen { get; set; }

		/// <summary>
		/// Distance between points in 3d space (for 3d projection).
		/// </summary>
		public double DistanceWorld { get; set; }

		/// <summary>
		/// Distance between points on screen (for screen projection).
		/// </summary>
		public double DistanceScreen { get; set; }

		/// <summary>
		/// Direction of the ray.
		/// </summary>
		public Vector3 Direction { get; set; }
	}
}
