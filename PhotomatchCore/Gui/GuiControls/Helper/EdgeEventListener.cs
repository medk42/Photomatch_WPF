using System;
using System.Collections.Generic;
using System.Text;

using PhotomatchCore.Logic;

namespace PhotomatchCore.Gui.GuiControls.Helper
{
	public class EdgeEventListener
	{
		private readonly ILine WindowLine;
		private readonly Edge Edge;
		private readonly ModelVisualization ModelVisualization;

		public EdgeEventListener(ILine windowLine, Edge edge, ModelVisualization modelVisualization)
		{
			this.WindowLine = windowLine;
			this.Edge = edge;
			this.ModelVisualization = modelVisualization;
		}

		public void StartPositionChanged(Vector3 newPosition)
		{
			ModelVisualization.DisplayClippedEdge(WindowLine, Edge);
		}

		public void EndPositionChanged(Vector3 newPosition)
		{
			ModelVisualization.DisplayClippedEdge(WindowLine, Edge);
		}
	}
}
