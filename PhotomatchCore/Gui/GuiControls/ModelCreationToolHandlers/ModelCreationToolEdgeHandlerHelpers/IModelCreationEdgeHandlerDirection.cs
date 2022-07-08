using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Gui.GuiControls.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers
{
	public interface IModelCreationEdgeHandlerDirection
	{
		ApplicationColor EdgeColor { get; }
		ModelCreationEdgeHandlerDirectionProjection Project(Vector3 from, Vector2 mouseCoord);
	}
}
