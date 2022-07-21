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
	/// <summary>
	/// Class for handling model creation.
	/// </summary>
	public class ModelCreationHandler
	{
		/// <summary>
		/// Get/set true if the handler is currently being used and is displayed, false otherwise.
		/// </summary>
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
		private bool Active_;

		private PerspectiveData Perspective;
		private Model Model;
		private ModelVisualization ModelVisualization;

		private ModelCreationTool ModelCreationTool;
		private BaseModelCreationToolHandler[] ModelCreationToolHandlers;

		/// <summary>
		/// Handler uses 5 handlers, one for each tool.
		/// </summary>
		/// <param name="model">Handler is changing the model.</param>
		/// <param name="window">Handler needs to display additional geometry.</param>
		/// <param name="perspective">Handler converts between world and screen space.</param>
		/// <param name="modelVisualization">Handler displays the model.</param>
		/// <param name="logger">Handler sends messages to user.</param>
		/// <param name="pointGrabRadius">Screen distance in pixels, from which a vertex/edge can be selected.</param>
		/// <param name="pointDrawRadius">The radius of drawn vertices in pixels on screen.</param>
		public ModelCreationHandler(Model model, IImageView window, PerspectiveData perspective, ModelVisualization modelVisualization, ILogger logger, double pointDrawRadius, double pointGrabRadius)
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

		/// <summary>
		/// Pass notification to handlers.
		/// </summary>
		public void MouseMove(Vector2 mouseCoord)
		{
			for (int i = 0; i < ModelCreationToolHandlers.Length; i++)
			{
				ModelCreationToolHandlers[i].MouseMove(mouseCoord);
			}
		}

		/// <summary>
		/// Pass notification to handlers.
		/// </summary>
		public void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			for (int i = 0; i < ModelCreationToolHandlers.Length; i++)
			{
				ModelCreationToolHandlers[i].MouseDown(mouseCoord, button);
			}
		}

		/// <summary>
		/// Pass notification to handlers.
		/// </summary>
		public void MouseUp(Vector2 mouseCoord, MouseButton button)
		{
			for (int i = 0; i < ModelCreationToolHandlers.Length; i++)
			{
				ModelCreationToolHandlers[i].MouseUp(mouseCoord, button);
			}
		}

		/// <summary>
		/// Pass notification to handlers.
		/// </summary>
		public void KeyDown(KeyboardKey key)
		{
			for (int i = 0; i < ModelCreationToolHandlers.Length; i++)
			{
				ModelCreationToolHandlers[i].KeyDown(key);
			}
		}

		/// <summary>
		/// Pass notification to handlers.
		/// </summary>
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
		
		/// <summary>
		/// Dispose of ModelVisualization, all handlers and perspective.
		/// </summary>
		public void Dispose()
		{
			ModelVisualization.Dispose();
			for (int i = 0; i < ModelCreationToolHandlers.Length; i++)
			{
				ModelCreationToolHandlers[i].Dispose();
			}
			Perspective = null;
		}

		/// <summary>
		/// Select handler based on newModelCreationTool.
		/// </summary>
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

		/// <summary>
		/// Update model to model passed by parameter in all handlers.
		/// </summary>
		public void UpdateModel(Model model)
		{
			foreach (BaseModelCreationToolHandler handler in ModelCreationToolHandlers)
				handler.UpdateModel(model);
		}
	}
}
