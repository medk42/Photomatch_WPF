using PhotomatchCore.Logic.Perspective;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Gui.GuiControls.ToolHandlers.ModelCreationToolHandlers.Helper
{

	/// <summary>
	/// Class displaying a 3d cursor at specified point.
	/// </summary>
	public class Cursor
	{
		private ILine CursorXLine, CursorYLine, CursorZLine;
		private PerspectiveData Perspective;

		/// <summary>
		/// Cursor is visible if set to true.
		/// </summary>
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
		private bool Visible_ = false;

		/// <summary>
		/// 3d point at which the cursor will be displayed.
		/// </summary>
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
		private Vector3 Position_;

		/// <summary>
		/// Create the 3d cursor from 3 InfiniteLine objects.
		/// </summary>
		/// <param name="window">Class is displaying the cursor.</param>
		/// <param name="perspective">Class needs to convert from world to screen to display the cursor.</param>
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

		/// <summary>
		/// Dispose of InfiniteLine objects.
		/// </summary>
		public void Dispose()
		{
			CursorXLine.Dispose();
			CursorYLine.Dispose();
			CursorZLine.Dispose();
		}
	}
}
