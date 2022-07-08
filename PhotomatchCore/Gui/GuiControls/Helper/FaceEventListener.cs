using PhotomatchCore.Data;
using PhotomatchCore.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Gui.GuiControls.Helper
{
	public class FaceEventListener
	{
		private readonly IPolygon WindowPolygon;
		private readonly Face Face;
		private readonly ModelVisualization ModelVisualization;

		public FaceEventListener(IPolygon windowPolygon, Face face, ModelVisualization modelVisualization)
		{
			this.WindowPolygon = windowPolygon;
			this.Face = face;
			this.ModelVisualization = modelVisualization;
		}

		public void FaceVertexPositionChanged(Vector3 newPosition, int vertexId)
		{
			ModelVisualization.DisplayClippedFace(WindowPolygon, Face);
		}
	}
}
