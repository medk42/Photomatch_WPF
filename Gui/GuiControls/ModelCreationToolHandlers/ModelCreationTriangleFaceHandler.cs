using Photomatch_ProofOfConcept_WPF.Gui.GuiControls.Helper;
using Photomatch_ProofOfConcept_WPF.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls.ModelCreationToolHandlers
{
	public class ModelCreationTriangleFaceHandler : BaseModelCreationToolHandler
	{
		public override ModelCreationTool ToolType => ModelCreationTool.TriangleFace;

		private ModelVisualization ModelVisualization;
		private Model Model;

		private enum TriangleFaceState { None, FirstPoint, SecondPoint };
		private Vertex first, second;
		private TriangleFaceState State = TriangleFaceState.None;

		public ModelCreationTriangleFaceHandler(ModelVisualization modelVisualization, Model model)
		{
			this.ModelVisualization = modelVisualization;
			this.Model = model;

			this.Active = false;
			SetActive(Active);
		}

		public override void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			if (button != MouseButton.Left)
				return;

			Vertex foundPoint = ModelVisualization.GetVertexUnderMouse(mouseCoord);

			if (foundPoint != null)
			{
				switch (State)
				{
					case TriangleFaceState.None:
						first = foundPoint;
						State = TriangleFaceState.FirstPoint;
						break;
					case TriangleFaceState.FirstPoint:
						if (first != foundPoint)
						{
							second = foundPoint;
							State = TriangleFaceState.SecondPoint;
						}
						break;
					case TriangleFaceState.SecondPoint:
						if (first != foundPoint && second != foundPoint)
						{
							Model.AddFace(new List<Vertex>() { first, second, foundPoint });
							State = TriangleFaceState.None;
						}
						break;
				}
			}

			ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);
		}

		public override void MouseMove(Vector2 mouseCoord)
		{
			ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);
		}

		internal override void SetActive(bool active)
		{
			ModelVisualization.ModelHoverEllipse.Active = active;
		}
	}
}
