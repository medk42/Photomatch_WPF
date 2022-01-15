using Photomatch_ProofOfConcept_WPF.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls.ModelCreationToolHandlers
{
	interface IModelCreationToolHandler
	{
		bool Active { get; set; }
		ModelCreationTool ToolType { get; }
		void MouseMove(Vector2 mouseCoord);
		void MouseDown(Vector2 mouseCoord, MouseButton button);
		void MouseUp(Vector2 mouseCoord, MouseButton button);
		void KeyDown(KeyboardKey key);
		void KeyUp(KeyboardKey key);
		void Dispose();
	}
}
