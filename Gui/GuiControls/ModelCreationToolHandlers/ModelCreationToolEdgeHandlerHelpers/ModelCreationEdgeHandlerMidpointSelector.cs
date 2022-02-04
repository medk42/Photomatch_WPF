﻿using Photomatch_ProofOfConcept_WPF.Gui.GuiControls.Helper;
using Photomatch_ProofOfConcept_WPF.Gui.GuiControls.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers.Helper;
using Photomatch_ProofOfConcept_WPF.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers
{
	public class ModelCreationEdgeHandlerMidpointSelector : IModelCreationEdgeHandlerSelector
	{
		public ApplicationColor VertexColor => ApplicationColor.Midpoint;

		private ModelVisualization ModelVisualization;
		private Model Model;
		private PerspectiveData Perspective;
		private IWindow Window;

		private double PointGrabRadius;

		public ModelCreationEdgeHandlerMidpointSelector(ModelVisualization modelVisualization, Model model, PerspectiveData perspective, IWindow window, double pointGrabRadius)
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
			if (edgeTuple != null)
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
	}
}