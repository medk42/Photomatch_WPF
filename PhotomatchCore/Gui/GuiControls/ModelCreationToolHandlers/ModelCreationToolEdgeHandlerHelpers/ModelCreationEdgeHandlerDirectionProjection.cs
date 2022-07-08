using PhotomatchCore.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Gui.GuiControls.ModelCreationToolHandlers.ModelCreationToolEdgeHandlerHelpers
{
	public class ModelCreationEdgeHandlerDirectionProjection
	{
		public Vector3 ProjectedWorld { get; set; }
		public Vector2 ProjectedScreen { get; set; }
		public double DistanceWorld { get; set; }
		public double DistanceScreen { get; set; }
		public Vector3 Direction { get; set; }
	}
}
