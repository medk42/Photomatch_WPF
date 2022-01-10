using System;
using System.Collections.Generic;
using System.Text;

using Photomatch_ProofOfConcept_WPF.Logic;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls.Helper
{
	class EdgeEventListener
	{
		private readonly ILine WindowLine;
		private readonly PerspectiveData Perspective;

		public EdgeEventListener(ILine windowLine, PerspectiveData perspective)
		{
			this.WindowLine = windowLine;
			this.Perspective = perspective;
		}

		public void StartPositionChanged(Vector3 newPosition)
		{
			WindowLine.Start = Perspective.WorldToScreen(newPosition);
		}

		public void EndPositionChanged(Vector3 newPosition)
		{
			WindowLine.End = Perspective.WorldToScreen(newPosition);
		}
	}
}
