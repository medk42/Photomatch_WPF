using System;
using System.Collections.Generic;
using System.Text;
using PhotomatchCore.Gui.GuiControls.Helper;
using PhotomatchCore.Utilities;
using PhotomatchCore.Logic.Model;
using PhotomatchCore.Logic.Perspective;
using PhotomatchCore.Gui.GuiControls.ToolHandlers.ModelCreationToolHandlers;

namespace PhotomatchCore.Gui.GuiControls.ToolHandlers
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
		private BaseModelCreationToolHandler[] ModelCreationToolHandlers;

		public ModelCreationHandler(Model model, IWindow window, PerspectiveData perspective, ModelVisualization modelVisualization, ILogger logger, double pointDrawRadius, double pointGrabRadius)
		{
			Model = model;
			Perspective = perspective;
			ModelVisualization = modelVisualization;

			ModelCreationToolHandlers = new BaseModelCreationToolHandler[] {
				new ModelCreationEdgeHandler(Perspective, Model, ModelVisualization, window, pointDrawRadius, pointGrabRadius),
				new ModelCreationDeleteHandler(ModelVisualization),
				new ModelCreationTriangleFaceHandler(ModelVisualization, Model, window),
				new ModelCreationComplexFaceHandler(ModelVisualization, Model, window, logger),
				new ModelCreationFaceNormalsHandler(ModelVisualization, model, window, Perspective)
			};

			Active = false;
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

			BaseModelCreationToolHandler handler = null;
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
					BaseModelCreationToolHandler handler = null;
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

		public void UpdateModel(Model model)
		{
			foreach (BaseModelCreationToolHandler handler in ModelCreationToolHandlers)
				handler.UpdateModel(model);
		}
	}
}
