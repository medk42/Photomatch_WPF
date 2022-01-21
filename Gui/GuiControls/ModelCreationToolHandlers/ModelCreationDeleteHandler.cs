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
		private Model Model;

		private ILine HoverEdge = null;

		public ModelCreationDeleteHandler(ModelVisualization modelVisualization, Model model)
		{
			this.ModelVisualization = modelVisualization;
			this.Model = model;

			this.Active = false;
			SetActive(Active);
		}

		private void ResetHoverEdge()
		{
			if (HoverEdge != null)
			{
				HoverEdge.Color = ApplicationColor.Model;
				HoverEdge = null;
			}
		}

		public override void MouseMove(Vector2 mouseCoord)
		{
			if (Active)
			{
				if (ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord))
				{
					ResetHoverEdge();
					return;
				}

				Tuple<Edge, ILine> foundEdge = ModelVisualization.GetEdgeUnderMouse(mouseCoord);
				if (foundEdge != null)
				{
					if (foundEdge != HoverEdge)
					{
						ResetHoverEdge();
						HoverEdge = foundEdge.Item2;
						foundEdge.Item2.Color = ApplicationColor.Highlight;
					}
				}
				else 
					ResetHoverEdge();
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
				{
					foundPoint.Remove();
					ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);
					return;
				}

				Tuple<Edge, ILine> foundEdge = ModelVisualization.GetEdgeUnderMouse(mouseCoord);
				if (foundEdge != null)
				{
					ResetHoverEdge();
					foundEdge.Item1.Remove();
					return;
				}
			}
		}

		internal override void SetActive(bool active)
		{
			ModelVisualization.ModelHoverEllipse.Active = active;

			if (!active)
				ResetHoverEdge();
		}
	}
}
