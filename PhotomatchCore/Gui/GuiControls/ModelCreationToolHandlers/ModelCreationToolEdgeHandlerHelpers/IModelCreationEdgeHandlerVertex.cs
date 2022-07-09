using PhotomatchCore.Logic;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using PhotomatchCore.Logic.Model;

namespace PhotomatchCore.Gui.GuiControls.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers
{
	public interface IModelCreationEdgeHandlerVertex
	{
		Vector2 ScreenPosition { get; }
		Vector3 WorldPosition { get; }
		Vertex ModelVertex { get; }
		bool UpdateToHoldRay(Ray3D holdRay);
	}
}
