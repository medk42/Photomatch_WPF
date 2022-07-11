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
	public class ModelCreationEdgeHandlerEdgepointSelector : IModelCreationEdgeHandlerSelector
	{
		public ApplicationColor VertexColor => ApplicationColor.Edgepoint;

		private ModelVisualization ModelVisualization;
		private Model Model;
		private PerspectiveData Perspective;

		public ModelCreationEdgeHandlerEdgepointSelector(ModelVisualization modelVisualization, Model model, PerspectiveData perspective)
		{
			ModelVisualization = modelVisualization;
			Model = model;
			Perspective = perspective;
		}

		public IModelCreationEdgeHandlerVertex GetVertex(Vector2 mouseCoord)
		{
			var edgeTuple = ModelVisualization.GetEdgeUnderMouse(mouseCoord);
			if (edgeTuple != null)
			{
				Edge edge = edgeTuple.Item1;

				Ray3D mouseRay = Perspective.ScreenToWorldRay(mouseCoord);
				Line3D edgeLine = new Line3D(edge.Start.Position, edge.End.Position);
				ClosestPoint3D closest = Intersections3D.GetRayRayClosest(mouseRay, edgeLine.AsRay());
				Vector3 edgeClosestPoint = closest.RayBClosest;

				return new SelectedEdgepoint(edge, edgeClosestPoint, Model, Perspective);
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

		public void KeyDown(KeyboardKey key) { }

		public void KeyUp(KeyboardKey key) { }
	}
}
