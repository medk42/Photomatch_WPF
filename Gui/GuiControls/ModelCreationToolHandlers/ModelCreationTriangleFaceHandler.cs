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
		private IWindow Window;

		private enum TriangleFaceState { None, FirstPoint, SecondPoint };
		private Vertex first, second;
		private TriangleFaceState State = TriangleFaceState.None;

		private ILine Line1;
		private ILine Line2;
		private ILine Line3;

		public ModelCreationTriangleFaceHandler(ModelVisualization modelVisualization, Model model, IWindow window)
		{
			this.ModelVisualization = modelVisualization;
			this.Model = model;
			this.Window = window;

			this.Line1 = Window.CreateLine(new Vector2(), new Vector2(), 0, ApplicationColor.Selected);
			this.Line2 = Window.CreateLine(new Vector2(), new Vector2(), 0, ApplicationColor.Selected);
			this.Line3 = Window.CreateLine(new Vector2(), new Vector2(), 0, ApplicationColor.Selected);

			this.Line1.Visible = false;
			this.Line2.Visible = false;
			this.Line3.Visible = false;

			this.Active = false;
			SetActive(Active);
		}

		public override void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			if (Active)
			{
				if (button != MouseButton.Left)
					return;

				Tuple<Vertex, Vector2> found = ModelVisualization.GetVertexUnderMouse(mouseCoord);
				Vertex foundPoint = found.Item1;
				Vector2 foundPosition = found.Item2;

				if (foundPoint != null)
				{
					switch (State)
					{
						case TriangleFaceState.None:
							first = foundPoint;
							State = TriangleFaceState.FirstPoint;

							Line1.Start = foundPosition;
							Line1.End = foundPosition;
							Line1.Visible = true;
							break;
						case TriangleFaceState.FirstPoint:
							if (first != foundPoint)
							{
								second = foundPoint;
								State = TriangleFaceState.SecondPoint;

								Line1.End = foundPosition;
								Line2.Start = foundPosition;
								Line2.End = foundPosition;
								Line3.Start = foundPosition;
								Line3.End = Line1.Start;
								Line2.Visible = true;
								Line3.Visible = true;
							}
							break;
						case TriangleFaceState.SecondPoint:
							if (first != foundPoint && second != foundPoint)
							{
								Model.AddFace(new List<Vertex>() { first, second, foundPoint });
								State = TriangleFaceState.None;

								Line1.Visible = false;
								Line2.Visible = false;
								Line3.Visible = false;
							}
							break;
					}
				}

				ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);
			}
		}

		public override void MouseMove(Vector2 mouseCoord)
		{
			if (Active)
			{
				ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);

				switch (State)
				{
					case TriangleFaceState.None:
						break;
					case TriangleFaceState.FirstPoint:
						Line1.End = mouseCoord;
						break;
					case TriangleFaceState.SecondPoint:
						Line2.End = mouseCoord;
						Line3.Start = mouseCoord;
						break;
				}
			}
		}

		public override void KeyDown(KeyboardKey key)
		{
			if (Active)
			{
				switch (key)
				{
					case KeyboardKey.Escape:
						CancelFaceCreate();
						break;
				}
			}
		}

		private void CancelFaceCreate()
		{
			State = TriangleFaceState.None;
			Line1.Visible = false;
			Line2.Visible = false;
			Line3.Visible = false;
		}

		internal override void SetActive(bool active)
		{
			ModelVisualization.ModelHoverEllipse.Active = active;

			if (!active)
				CancelFaceCreate();
		}

		public override void UpdateModel(Model model)
		{
			Model = model;
		}
	}
}
