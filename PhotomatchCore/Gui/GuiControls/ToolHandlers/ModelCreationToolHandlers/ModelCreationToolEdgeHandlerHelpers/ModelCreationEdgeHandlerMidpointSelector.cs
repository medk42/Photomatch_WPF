using PhotomatchCore.Gui.GuiControls.Helper;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using PhotomatchCore.Logic.Model;
using PhotomatchCore.Logic.Perspective;
using PhotomatchCore.Gui.GuiControls.ToolHandlers.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers.Helper;

namespace PhotomatchCore.Gui.GuiControls.ToolHandlers.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers
{
	/// <summary>
	/// Class representing a selector for edge midpoints.
	/// </summary>
	public class ModelCreationEdgeHandlerMidpointSelector : IModelCreationEdgeHandlerSelector
	{
		public ApplicationColor VertexColor => ApplicationColor.Midpoint;

		private ModelVisualization ModelVisualization;
		private Model Model;
		private PerspectiveData Perspective;
		private IImageView Window;

		private double PointGrabRadius;
		private bool Enabled = true;

		/// <param name="modelVisualization">Selector uses ModelVisualization to get edge under mouse.</param>
		/// <param name="model">Passed to found SelectedEdgepoint</param>
		/// <param name="perspective">Used for WorldToScreen transformation.</param>
		/// <param name="window">Used to find distance on a screen between mouse and edge midpoint.</param>
		/// <param name="pointGrabRadius">Screen distance in pixels, from which a midpoint can be selected.</param>
		public ModelCreationEdgeHandlerMidpointSelector(ModelVisualization modelVisualization, Model model, PerspectiveData perspective, IImageView window, double pointGrabRadius)
		{
			ModelVisualization = modelVisualization;
			Model = model;
			Perspective = perspective;
			Window = window;
			PointGrabRadius = pointGrabRadius;
		}

		/// <summary>
		/// Try to find midpoint under mouse.
		/// </summary>
		/// <returns>Reference to data about the found point on an edge or null.</returns>
		public IModelCreationEdgeHandlerVertex GetVertex(Vector2 mouseCoord)
		{
			var edgeTuple = ModelVisualization.GetEdgeUnderMouse(mouseCoord);
			if (Enabled && edgeTuple != null)
			{
				Edge edge = edgeTuple.Item1;
				Vector3 midpoint = (edge.Start.Position + edge.End.Position) / 2;
				if (Window.ScreenDistance(mouseCoord, Perspective.WorldToScreen(midpoint)) < PointGrabRadius)
					return new SelectedEdgepoint(edge, midpoint, Model, Perspective);
				else
					return null;
			}
			else
			{
				return null;
			}
		}

		public void UpdateModel(Model model)
		{
			Model = model;
		}

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
