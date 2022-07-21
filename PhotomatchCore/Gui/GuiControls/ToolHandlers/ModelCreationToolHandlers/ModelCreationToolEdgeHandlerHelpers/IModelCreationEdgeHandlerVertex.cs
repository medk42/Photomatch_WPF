using PhotomatchCore.Logic;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using PhotomatchCore.Logic.Model;

namespace PhotomatchCore.Gui.GuiControls.ToolHandlers.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers
{
	/// <summary>
	/// Interface containing data about a found point on a model.
	/// </summary>
	public interface IModelCreationEdgeHandlerVertex
	{
		/// <summary>
		/// Position of the point on screen.
		/// </summary>
		Vector2 ScreenPosition { get; }

		/// <summary>
		/// Position of the point in 3d space.
		/// </summary>
		Vector3 WorldPosition { get; }

		/// <summary>
		/// Vertex corresponding to the point (can be created on first call if it doesn't exist).
		/// Caller is responsible for disposing of unwanted vertices.
		/// </summary>
		Vertex ModelVertex { get; }

		/// <summary>
		/// For example:
		/// Selector might have found a point on an edge, but the created edge has fixed direction,
		/// so it updates its end to the closest point in 3d space. But if the created edge intersects
		/// this edge, we might want to move the found point to the intersection, so that the created
		/// edge ends AT this edge, not at the same coordinates as a point on the edge.
		/// 
		/// For this use case, we supply a method using which the implementing class can update values
		/// of the found point to the intersection.
		/// 
		/// For clearer understanding, see implementations.
		/// </summary>
		/// <param name="holdRay">represents the created edge with fixed direction</param>
		/// <returns>
		/// True if the point was updated and should be used as end of the line instead of a closest
		/// point along the fixed direction of the created edge.
		/// </returns>
		bool UpdateToHoldRay(Ray3D holdRay);
	}
}
