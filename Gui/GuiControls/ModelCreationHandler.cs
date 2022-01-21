using System;
using System.Collections.Generic;
using System.Text;

using Photomatch_ProofOfConcept_WPF.Gui.GuiControls.Helper;
using Photomatch_ProofOfConcept_WPF.Gui.GuiControls.ModelCreationToolHandlers;
using Photomatch_ProofOfConcept_WPF.Logic;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls
{
	public class ModelCreationHandler
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

		private PerspectiveData Perspective;
		private Model Model;
		private ModelVisualization ModelVisualization;

		private ModelCreationTool ModelCreationTool;
		private IModelCreationToolHandler[] ModelCreationToolHandlers;

		public ModelCreationHandler(Model model, IWindow window, PerspectiveData perspective, ModelVisualization modelVisualization)
		{
			this.Model = model;
			this.Perspective = perspective;
			this.ModelVisualization = modelVisualization;

			ModelCreationToolHandlers = new IModelCreationToolHandler[] {
				new ModelCreationEdgeHandler(Perspective, Model, ModelVisualization),
				new ModelCreationDeleteHandler(ModelVisualization),
				new ModelCreationTriangleFaceHandler(ModelVisualization, Model, window),
				new ModelCreationComplexFaceHandler(ModelVisualization, Model, window)
			}; 

			this.Active = false;
			SetActive(Active);
		}

		public void MouseMove(Vector2 mouseCoord)
		{
			for (int i = 0; i < ModelCreationToolHandlers.Length; i++)
			{
				ModelCreationToolHandlers[i].MouseMove(mouseCoord);
			}
		}

		public void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			for (int i = 0; i < ModelCreationToolHandlers.Length; i++)
			{
				ModelCreationToolHandlers[i].MouseDown(mouseCoord, button);
			}
		}

		public void MouseUp(Vector2 mouseCoord, MouseButton button)
		{
			for (int i = 0; i < ModelCreationToolHandlers.Length; i++)
			{
				ModelCreationToolHandlers[i].MouseUp(mouseCoord, button);
			}
		}

		public void KeyDown(KeyboardKey key)
		{
			for (int i = 0; i < ModelCreationToolHandlers.Length; i++)
			{
				ModelCreationToolHandlers[i].KeyDown(key);
			}
		}

		public void KeyUp(KeyboardKey key)
		{
			for (int i = 0; i < ModelCreationToolHandlers.Length; i++)
			{
				ModelCreationToolHandlers[i].KeyUp(key);
			}
		}

		private void SetActive(bool active)
		{
			ModelVisualization.ShowModel(active);

			IModelCreationToolHandler handler = null;
			for (int i = 0; i < ModelCreationToolHandlers.Length; i++)
			{
				ModelCreationToolHandlers[i].Active = false;
				if (ModelCreationToolHandlers[i].ToolType == ModelCreationTool)
					handler = ModelCreationToolHandlers[i];
			}
			handler.Active = active;
		}

		public void Dispose()
		{
			ModelVisualization.Dispose();
			for (int i = 0; i < ModelCreationToolHandlers.Length; i++)
			{
				ModelCreationToolHandlers[i].Dispose();
			}
			Perspective = null;
		}

		public void CreationTool_Changed(ModelCreationTool newModelCreationTool)
		{
			if (ModelCreationTool != newModelCreationTool)
			{
				ModelCreationTool = newModelCreationTool;

				if (Active)
				{
					IModelCreationToolHandler handler = null;
					for (int i = 0; i < ModelCreationToolHandlers.Length; i++)
					{
						ModelCreationToolHandlers[i].Active = false;
						if (ModelCreationToolHandlers[i].ToolType == ModelCreationTool)
							handler = ModelCreationToolHandlers[i];
					}
					handler.Active = true;
				}
			}
		}
	}
}
