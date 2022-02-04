using Photomatch_ProofOfConcept_WPF.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers
{
	public interface IModelCreationEdgeHandlerSelector
	{
		ApplicationColor VertexColor { get; }
		IModelCreationEdgeHandlerVertex GetVertex(Vector2 mouseCoord);
		void UpdateModel(Model model);
		void KeyDown(KeyboardKey key);
		void KeyUp(KeyboardKey key);
	}
}
