﻿using Photomatch.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch.Gui.GuiControls.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers
{
	public interface IModelCreationEdgeHandlerDirection
	{
		ApplicationColor EdgeColor { get; }
		ModelCreationEdgeHandlerDirectionProjection Project(Vector3 from, Vector2 mouseCoord);
	}
}
