using Photomatch_ProofOfConcept_WPF.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls.Helper
{
	class FaceEventListener
	{
		private readonly IPolygon WindowPolygon;
		private readonly PerspectiveData Perspective;

		public FaceEventListener(IPolygon windowPolygon, PerspectiveData perspective)
		{
			this.WindowPolygon = windowPolygon;
			this.Perspective = perspective;
		}

		public void FaceVertexPositionChanged(Vector3 newPosition, int vertexId)
		{
			WindowPolygon[vertexId] = Perspective.WorldToScreen(newPosition);
		}
	}
}
