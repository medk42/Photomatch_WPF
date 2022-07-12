using PhotomatchCore.Logic.Perspective;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Gui.GuiControls.ToolHandlers.ModelCreationToolHandlers.Helper
{
	public class Cursor
	{
		private ILine CursorXLine, CursorYLine, CursorZLine;
		private PerspectiveData Perspective;

		private bool Visible_ = false;
		public bool Visible
		{
			get => Visible_;
			set
			{
				if (Visible_ != value)
				{
					Visible_ = value;

					CursorXLine.Visible = value;
					CursorYLine.Visible = value;
					CursorZLine.Visible = value;
				}
			}
		}

		private Vector3 Position_;
		public Vector3 Position
		{
			get => Position_;
			set
			{
				Position_ = value;

				Vector2 start = Perspective.WorldToScreen(Position_);

				Vector2 endX = Perspective.WorldToScreen(Position_ + new Vector3(1, 0, 0));
				Vector2 endY = Perspective.WorldToScreen(Position_ + new Vector3(0, 1, 0));
				Vector2 endZ = Perspective.WorldToScreen(Position_ + new Vector3(0, 0, 1));

				CursorXLine.Start = start;
				CursorYLine.Start = start;
				CursorZLine.Start = start;
				CursorXLine.End = endX;
				CursorYLine.End = endY;
				CursorZLine.End = endZ;
			}
		}

		public Cursor(IImageView window, PerspectiveData perspective)
		{
			CursorXLine = new InfiniteLine(window, new Vector2(), new Vector2(), ApplicationColor.XAxisDotted);
			CursorXLine.Visible = false;

			CursorYLine = new InfiniteLine(window, new Vector2(), new Vector2(), ApplicationColor.YAxisDotted);
			CursorYLine.Visible = false;

			CursorZLine = new InfiniteLine(window, new Vector2(), new Vector2(), ApplicationColor.ZAxisDotted);
			CursorZLine.Visible = false;

			this.Perspective = perspective;
		}

		public void Dispose()
		{
			CursorXLine.Dispose();
			CursorYLine.Dispose();
			CursorZLine.Dispose();
		}
	}
}
