using PhotomatchCore.Logic;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Gui.GuiControls.ModelCreationToolHandlers
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
		void UpdateModel(Model model);
	}
}
