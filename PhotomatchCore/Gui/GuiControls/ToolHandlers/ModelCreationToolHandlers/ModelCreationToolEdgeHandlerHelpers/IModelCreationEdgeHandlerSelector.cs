using PhotomatchCore.Logic;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using PhotomatchCore.Logic.Model;

namespace PhotomatchCore.Gui.GuiControls.ToolHandlers.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers
{
	/// <summary>
	/// Interface for selectors of point on a model.
	/// </summary>
	public interface IModelCreationEdgeHandlerSelector
	{
		/// <summary>
		/// Color of points found by this selector.
		/// </summary>
		ApplicationColor VertexColor { get; }

		/// <summary>
		/// Try to find point under mouse.
		/// </summary>
		/// <returns>Reference to data about the found point or null.</returns>
		IModelCreationEdgeHandlerVertex GetVertex(Vector2 mouseCoord);

		/// <summary>
		/// Update model to model passed by parameter.
		/// </summary>
		void UpdateModel(Model model);
		void KeyDown(KeyboardKey key);
		void KeyUp(KeyboardKey key);
	}
}
