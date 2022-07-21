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
	/// Class representing a selector for points on an edge.
	/// </summary>
	public class ModelCreationEdgeHandlerEdgepointSelector : IModelCreationEdgeHandlerSelector
	{
		public ApplicationColor VertexColor => ApplicationColor.Edgepoint;

		private ModelVisualization ModelVisualization;
		private Model Model;
		private PerspectiveData Perspective;

		/// <param name="modelVisualization">Selector uses ModelVisualization to get edge under mouse.</param>
		/// <param name="model">Passed to found SelectedEdgepoint</param>
		/// <param name="perspective">Used for ScreenToWorldRay transformation.</param>
		public ModelCreationEdgeHandlerEdgepointSelector(ModelVisualization modelVisualization, Model model, PerspectiveData perspective)
		{
			ModelVisualization = modelVisualization;
			Model = model;
			Perspective = perspective;
		}

		/// <summary>
		/// Try to find a point on an edge under mouse.
		/// </summary>
		/// <returns>Reference to data about the found point on an edge or null.</returns>
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
