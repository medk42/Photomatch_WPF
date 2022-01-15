using System;
using System.Collections.Generic;
using System.Text;

using Photomatch_ProofOfConcept_WPF.Gui.GuiControls.Helper;
using Photomatch_ProofOfConcept_WPF.Gui.GuiControls.ModelCreationToolHandlers;
using Photomatch_ProofOfConcept_WPF.Logic;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls
{
	class ModelCreationHandler
	{
		private bool Active_;
		public bool Active
		{
			get => Active_;
			set
			{
				if (value != Active_)
				{
					Active_ = value;
					SetActive(Active_);
				}
			}
		}

		private ModelCreationEdgeHandler ModelCreationEdgeHandler;

		private PerspectiveData Perspective;
		private Model Model;
		private ModelVisualization ModelVisualization;

		private ModelCreationTool ModelCreationTool;

		public ModelCreationHandler(Model model, PerspectiveData perspective, ModelVisualization modelVisualization)
		{
			this.Model = model;
			this.Perspective = perspective;
			this.ModelVisualization = modelVisualization;

			this.ModelCreationEdgeHandler = new ModelCreationEdgeHandler(Perspective, Model, ModelVisualization);

			this.Active = false;
			SetActive(Active);
		}

		public void MouseMove(Vector2 mouseCoord)
		{
			ModelCreationEdgeHandler.MouseMove(mouseCoord);

			if (Active)
			{
				switch (ModelCreationTool)
				{
					case ModelCreationTool.Delete:
						ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);
						break;
					case ModelCreationTool.Edge:
						break;
					case ModelCreationTool.TriangleFace:
						ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);
						break;
					default:
						throw new Exception("Unknown switch case.");
				}
			}
		}

		public void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			ModelCreationEdgeHandler.MouseDown(mouseCoord, button);

			if (Active)
			{
				switch (ModelCreationTool)
				{
					case ModelCreationTool.Delete:
						MouseDownDelete(mouseCoord, button);
						ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);
						break;
					case ModelCreationTool.Edge:
						break;
					case ModelCreationTool.TriangleFace:
						MouseDownTriangleFace(mouseCoord, button);
						ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);
						break;
					default:
						throw new Exception("Unknown switch case.");
				}
			}
		}

		public void MouseUp(Vector2 mouseCoord, MouseButton button)
		{
			ModelCreationEdgeHandler.MouseUp(mouseCoord, button);
		}

		private enum TriangleFaceState { None, FirstPoint, SecondPoint };
		private Vertex first, second;
		private TriangleFaceState State = TriangleFaceState.None;

		private void MouseDownTriangleFace(Vector2 mouseCoord, MouseButton button)
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
		}

		private void MouseDownDelete(Vector2 mouseCoord, MouseButton button)
		{
			if (button != MouseButton.Left)
				return;

			Vertex foundPoint = ModelVisualization.GetVertexUnderMouse(mouseCoord);

			if (foundPoint != null)
				foundPoint.Remove();
		}

		public void KeyDown(KeyboardKey key)
		{
			ModelCreationEdgeHandler.KeyDown(key);
		}

		public void KeyUp(KeyboardKey key)
		{
			ModelCreationEdgeHandler.KeyUp(key);
		}

		private void SetActive(bool active)
		{
			ModelVisualization.ShowModel(active);

			if (active)
			{
				switch (ModelCreationTool)
				{
					case ModelCreationTool.Delete:
						break;
					case ModelCreationTool.Edge:
						ModelCreationEdgeHandler.Active = true;
						break;
					case ModelCreationTool.TriangleFace:
						break;
				}
			}
			else
			{
				ModelCreationEdgeHandler.Active = false;
			}
		}

		public void Dispose()
		{
			ModelVisualization.Dispose();
			Perspective = null;
		}

		public void CreationTool_Changed(ModelCreationTool newModelCreationTool)
		{
			if (ModelCreationTool != newModelCreationTool)
			{
				ModelCreationTool = newModelCreationTool;

				if (Active)
				{
					ModelCreationEdgeHandler.Active = false;
					switch (ModelCreationTool)
					{
						case ModelCreationTool.Delete:
							break;
						case ModelCreationTool.Edge:
							ModelCreationEdgeHandler.Active = true;
							break;
						case ModelCreationTool.TriangleFace:
							break;
					}
				}
			}
		}
	}
}
