using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using PhotomatchCore.Logic.Model;

namespace PhotomatchCore.Gui.GuiControls.Helper
{
	/// <summary>
	/// Listener for changes of a specified face, updates visualization.
	/// </summary>
	public class FaceEventListener
	{
		private readonly IPolygon WindowPolygon;
		private readonly Face Face;
		private readonly ModelVisualization ModelVisualization;

		/// <param name="windowPolygon">Displayed polygon.</param>
		/// <param name="face">Face corresponding to displayed polygon.</param>
		/// <param name="modelVisualization">For displaying clipped face.</param>
		public FaceEventListener(IPolygon windowPolygon, Face face, ModelVisualization modelVisualization)
		{
			this.WindowPolygon = windowPolygon;
			this.Face = face;
			this.ModelVisualization = modelVisualization;
		}

		/// <summary>
		/// Update displayed polygon.
		/// </summary>
		public void FaceVertexPositionChanged(Vector3 newPosition, int vertexId)
		{
			ModelVisualization.DisplayClippedFace(WindowPolygon, Face);
		}
	}
}
