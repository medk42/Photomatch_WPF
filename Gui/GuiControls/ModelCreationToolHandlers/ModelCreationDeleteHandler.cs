using Photomatch_ProofOfConcept_WPF.Gui.GuiControls.Helper;
using Photomatch_ProofOfConcept_WPF.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls.ModelCreationToolHandlers
{
	public class ModelCreationDeleteHandler : BaseModelCreationToolHandler
	{
		public override ModelCreationTool ToolType => ModelCreationTool.Delete;

		private ModelVisualization ModelVisualization;

		public ModelCreationDeleteHandler(ModelVisualization modelVisualization)
		{
			this.ModelVisualization = modelVisualization;

			this.Active = false;
			SetActive(Active);
		}

		public override void MouseMove(Vector2 mouseCoord)
		{
			if (Active)
			{
				ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);
			}
		}

		public override void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			if (Active)
			{
				if (button != MouseButton.Left)
					return;

				Vertex foundPoint = ModelVisualization.GetVertexUnderMouse(mouseCoord).Item1;

				if (foundPoint != null)
					foundPoint.Remove();

				ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);
			}
		}

		internal override void SetActive(bool active)
		{
			ModelVisualization.ModelHoverEllipse.Active = active;
		}
	}
}
