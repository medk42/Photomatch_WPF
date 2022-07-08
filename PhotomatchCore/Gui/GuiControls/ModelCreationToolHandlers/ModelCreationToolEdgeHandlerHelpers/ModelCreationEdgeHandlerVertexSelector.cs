using PhotomatchCore.Data;
using PhotomatchCore.Gui.GuiControls.Helper;
using PhotomatchCore.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Gui.GuiControls.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers
{
	public class ModelCreationEdgeHandlerVertexSelector : IModelCreationEdgeHandlerSelector
	{
		private class SelectedVertex : IModelCreationEdgeHandlerVertex
		{
			public Vector2 ScreenPosition { get; set; }

			public Vector3 WorldPosition { get; set; }

			public Vertex ModelVertex { get; set; }

			public bool UpdateToHoldRay(Ray3D holdRay)
			{
				Vector3RayProj worldPosProject = Intersections3D.ProjectVectorToRay(WorldPosition, holdRay);

				if (worldPosProject.Distance <= 1e-6)
				{
					return true;
				}

				return false;
			}
		}

		public ApplicationColor VertexColor => ApplicationColor.Vertex;

		private ModelVisualization ModelVisualization;
		private bool Enabled = true;

		public ModelCreationEdgeHandlerVertexSelector(ModelVisualization modelVisualization)
		{
			this.ModelVisualization = modelVisualization;
		}

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
