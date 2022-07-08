using PhotomatchCore.Data;
using PhotomatchCore.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Gui.GuiControls.ModelCreationToolHandlers
{
	public abstract class BaseModelCreationToolHandler : IModelCreationToolHandler
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

		public abstract ModelCreationTool ToolType { get; }

		public virtual void Dispose() { }

		public virtual void KeyDown(KeyboardKey key) { }

		public virtual void KeyUp(KeyboardKey key) { }

		public virtual void MouseDown(Vector2 mouseCoord, MouseButton button) { }

		public virtual void MouseMove(Vector2 mouseCoord) { }

		public virtual void MouseUp(Vector2 mouseCoord, MouseButton button) { }

		internal virtual void SetActive(bool active) { }

		public virtual void UpdateModel(Model model) { }
	}
}
