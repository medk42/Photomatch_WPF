using System;
using System.Collections.Generic;
using System.Text;
using PhotomatchCore.Logic.Model;
using PhotomatchCore.Utilities;

namespace PhotomatchCore.Gui.GuiControls.Helper
{
	/// <summary>
	/// Listener for changes of a specified edge, updates visualization.
	/// </summary>
	public class EdgeEventListener
	{
		private readonly ILine WindowLine;
		private readonly Edge Edge;
		private readonly ModelVisualization ModelVisualization;

		/// <param name="windowLine">Displayed edge.</param>
		/// <param name="edge">Edge corresponding to displayed edge.</param>
		/// <param name="modelVisualization">For displaying clipped edge.</param>
		public EdgeEventListener(ILine windowLine, Edge edge, ModelVisualization modelVisualization)
		{
			this.WindowLine = windowLine;
			this.Edge = edge;
			this.ModelVisualization = modelVisualization;
		}

		/// <summary>
		/// Update displayed edge.
		/// </summary>
		public void StartPositionChanged(Vector3 newPosition)
		{
			ModelVisualization.DisplayClippedEdge(WindowLine, Edge);
		}

		/// <summary>
		/// Update displayed edge.
		/// </summary>
		public void EndPositionChanged(Vector3 newPosition)
		{
			ModelVisualization.DisplayClippedEdge(WindowLine, Edge);
		}
	}
}
