using Photomatch_ProofOfConcept_WPF.Gui.GuiControls.Helper;
using Photomatch_ProofOfConcept_WPF.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls.ModelCreationToolHandlers
{
	public class ModelCreationComplexFaceHandler : BaseModelCreationToolHandler
	{
		public override ModelCreationTool ToolType => ModelCreationTool.ComplexFace;

		private ModelVisualization ModelVisualization;
		private Model Model;
		private IWindow Window;

		private List<ILine> Lines = new List<ILine>();
		private List<Vertex> Vertices = new List<Vertex>();

		public ModelCreationComplexFaceHandler(ModelVisualization modelVisualization, Model model, IWindow window)
		{
			this.ModelVisualization = modelVisualization;
			this.Model = model;
			this.Window = window;

			this.Active = false;
			SetActive(Active);
		}

		public override void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			if (Active)
			{
				if (button == MouseButton.Right)
				{
					Model.AddFace(Vertices);
					Clear();
					return;
				}
				else if (button != MouseButton.Left)
					return;

				Tuple<Vertex, Vector2> found = ModelVisualization.GetVertexUnderMouse(mouseCoord);
				Vertex foundPoint = found.Item1;
				Vector2 foundPosition = found.Item2;

				if (foundPoint != null)
				{
					foreach (Vertex v in Vertices)
					{
						if (foundPoint == v)
						{
							Model.AddFace(Vertices);
							Clear();
							return;
						}
					}

					if (Vertices.Count > 0)
						Lines[Vertices.Count].End = foundPosition;
					else
						Lines.Add(Window.CreateLine(mouseCoord, foundPosition, 0, ApplicationColor.Selected));

					Vertices.Add(foundPoint);
					Lines.Add(Window.CreateLine(foundPosition, mouseCoord, 0, ApplicationColor.Selected));
				}

				ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);
			}
		}

		public override void MouseMove(Vector2 mouseCoord)
		{
			if (Active)
			{
				ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);

				if (Vertices.Count > 0)
				{
					Lines[0].Start = mouseCoord;
					Lines[Vertices.Count].End = mouseCoord;
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
						Clear();
						break;
				}
			}
		}

		private void Clear()
		{
			Vertices.Clear();
			foreach (ILine line in Lines)
				line.Dispose();
			Lines.Clear();
		}

		internal override void SetActive(bool active)
		{
			ModelVisualization.ModelHoverEllipse.Active = active;

			if (!active)
				Clear();
		}
	}
}
