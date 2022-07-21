using PhotomatchCore.Logic;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using PhotomatchCore.Logic.Model;

namespace PhotomatchCore.Gui.GuiControls.ToolHandlers.ModelCreationToolHandlers
{

	/// <summary>
	/// Abstract class defining the methods of model creation tool handlers.
	/// </summary>
	public abstract class BaseModelCreationToolHandler
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

		/// <summary>
		/// IMPORTANT: specifies which tool is the handler implementing.
		/// </summary>
		public abstract ModelCreationTool ToolType { get; }

		/// <summary>
		/// Dispose of all resources held by the handler.
		/// </summary>
		public virtual void Dispose() { }

		public virtual void KeyDown(KeyboardKey key) { }

		public virtual void KeyUp(KeyboardKey key) { }

		public virtual void MouseDown(Vector2 mouseCoord, MouseButton button) { }

		public virtual void MouseMove(Vector2 mouseCoord) { }

		public virtual void MouseUp(Vector2 mouseCoord, MouseButton button) { }

		internal virtual void SetActive(bool active) { }

		/// <summary>
		/// Update model to model passed by parameter.
		/// </summary>
		public virtual void UpdateModel(Model model) { }
	}
}
