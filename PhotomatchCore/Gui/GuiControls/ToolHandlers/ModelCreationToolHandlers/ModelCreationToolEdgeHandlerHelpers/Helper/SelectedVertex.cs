using PhotomatchCore.Logic.Model;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Gui.GuiControls.ToolHandlers.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers.Helper
{
	public class SelectedVertex : IModelCreationEdgeHandlerVertex
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
}
