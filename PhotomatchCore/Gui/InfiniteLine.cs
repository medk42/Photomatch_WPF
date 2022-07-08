using PhotomatchCore.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Gui
{
	/// <summary>
	/// Class representing an infinite GUI line in a specified GUI window.
	/// </summary>
	class InfiniteLine : ILine
	{
		private Vector2 Start_;
		/// <summary>
		/// A point on the infinite line. Must not equal End.
		/// </summary>
		public Vector2 Start
		{
			get => Start_;
			set
			{
				Start_ = value;
				RecalculateInfiniteLine();
			}
		}

		/// <summary>
		/// A point on the infinite line. Must not equal Start.
		/// </summary>
		private Vector2 End_;
		public Vector2 End
		{
			get => End_;
			set
			{
				End_ = value;
				RecalculateInfiniteLine();
			}
		}

		public ApplicationColor Color 
		{
			get => GuiLine.Color;
			set => GuiLine.Color = value; 
		}

		public bool Visible 
		{ 
			get => GuiLine.Visible; 
			set => GuiLine.Visible = value; 
		}

		private IWindow Window;
		private ILine GuiLine;

		/// <summary>
		/// Create an infinite line in a specified GUI window and with a specified start, end and color.
		/// </summary>
		public InfiniteLine(IWindow window, Vector2 start, Vector2 end, ApplicationColor color)
		{
			this.Window = window;
			this.GuiLine = window.CreateLine(new Vector2(), new Vector2(), 0, color);
			this.Start = start;
			this.End = end;
		}

		public void Dispose() => GuiLine.Dispose();

		/// <summary>
		/// Calculate actual line endpoints inside of the GUI window.
		/// </summary>
		private void RecalculateInfiniteLine()
		{
			Vector2 topLeft = new Vector2();
			Vector2 bottomRight = new Vector2(Window.Width, Window.Height);

			Vector2 end = Intersections2D.GetRayInsideBoxIntersection(new Line2D(Start, End).AsRay(), topLeft, bottomRight);
			if (end.Valid)
			{
				Vector2 start = Intersections2D.GetRayInsideBoxIntersection(new Line2D(end, Start).AsRay(), topLeft, bottomRight);
				GuiLine.Start = start;
				GuiLine.End = end;
			}
			else
			{
				Vector2 start = Intersections2D.GetRayInsideBoxIntersection(new Line2D(End, Start).AsRay(), topLeft, bottomRight);
				if (start.Valid)
				{
					end = Intersections2D.GetRayInsideBoxIntersection(new Line2D(start, End).AsRay(), topLeft, bottomRight);
					GuiLine.Start = start;
					GuiLine.End = end;
				}
				else
				{
					GuiLine.Start = new Vector2();
					GuiLine.End = new Vector2();
				}
			}
		}
	}
}
