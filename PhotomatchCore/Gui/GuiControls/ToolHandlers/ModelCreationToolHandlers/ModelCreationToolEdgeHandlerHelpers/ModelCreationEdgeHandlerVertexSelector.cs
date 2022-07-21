using PhotomatchCore.Gui.GuiControls.Helper;
using PhotomatchCore.Logic;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using PhotomatchCore.Logic.Model;
using PhotomatchCore.Gui.GuiControls.ToolHandlers.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers.Helper;

namespace PhotomatchCore.Gui.GuiControls.ToolHandlers.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers
{
	/// <summary>
	/// Class representing a selector for vertices.
	/// </summary>
	public class ModelCreationEdgeHandlerVertexSelector : IModelCreationEdgeHandlerSelector
	{
		public ApplicationColor VertexColor => ApplicationColor.Vertex;

		private ModelVisualization ModelVisualization;
		private bool Enabled = true;

		/// <param name="modelVisualization">Selector uses ModelVisualization to get vertex under mouse.</param>
		public ModelCreationEdgeHandlerVertexSelector(ModelVisualization modelVisualization)
		{
			ModelVisualization = modelVisualization;
		}

		/// <summary>
		/// Try to find vertex under mouse.
		/// </summary>
		/// <returns>Reference to data about the found vertex or null.</returns>
		public IModelCreationEdgeHandlerVertex GetVertex(Vector2 mouseCoord)
		{
			var vertexTuple = ModelVisualization.GetVertexUnderMouse(mouseCoord);
			if (Enabled && vertexTuple.Item1 != null)
				return new SelectedVertex()
				{
					ScreenPosition = vertexTuple.Item2,
					WorldPosition = vertexTuple.Item1.Position,
					ModelVertex = vertexTuple.Item1
				};
			else
				return null;
		}

		public void UpdateModel(Model model) { }

		/// <summary>
		/// Disable selector when CTRL is down.
		/// </summary>
		public void KeyDown(KeyboardKey key)
		{
			if (key == KeyboardKey.LeftCtrl)
				Enabled = false;
		}

		/// <summary>
		/// Enable selector when CTRL is up.
		/// </summary>
		public void KeyUp(KeyboardKey key)
		{
			if (key == KeyboardKey.LeftCtrl)
				Enabled = true;
		}
	}
}
