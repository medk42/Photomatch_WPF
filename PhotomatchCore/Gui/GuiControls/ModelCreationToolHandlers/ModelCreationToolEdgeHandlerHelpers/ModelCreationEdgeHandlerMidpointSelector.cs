using PhotomatchCore.Gui.GuiControls.Helper;
using PhotomatchCore.Gui.GuiControls.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers.Helper;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using PhotomatchCore.Logic.Model;
using PhotomatchCore.Logic.Perspective;

namespace PhotomatchCore.Gui.GuiControls.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers
{
	public class ModelCreationEdgeHandlerMidpointSelector : IModelCreationEdgeHandlerSelector
	{
		public ApplicationColor VertexColor => ApplicationColor.Midpoint;

		private ModelVisualization ModelVisualization;
		private Model Model;
		private PerspectiveData Perspective;
		private IImageView Window;

		private double PointGrabRadius;
		private bool Enabled = true;

		public ModelCreationEdgeHandlerMidpointSelector(ModelVisualization modelVisualization, Model model, PerspectiveData perspective, IImageView window, double pointGrabRadius)
		{
			this.ModelVisualization = modelVisualization;
			this.Model = model;
			this.Perspective = perspective;
			this.Window = window;
			this.PointGrabRadius = pointGrabRadius;
		}

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
			this.Model = model;
		}

		public void KeyDown(KeyboardKey key)
		{
			if (key == KeyboardKey.LeftCtrl)
				Enabled = false;
		}

		public void KeyUp(KeyboardKey key)
		{
			if (key == KeyboardKey.LeftCtrl)
				Enabled = true;
		}
	}
}
