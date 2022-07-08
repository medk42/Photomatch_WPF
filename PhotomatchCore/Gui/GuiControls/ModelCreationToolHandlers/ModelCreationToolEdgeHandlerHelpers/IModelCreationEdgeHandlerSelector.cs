using PhotomatchCore.Logic;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Gui.GuiControls.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers
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
