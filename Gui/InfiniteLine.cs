using Photomatch_ProofOfConcept_WPF.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Gui
{
	class InfiniteLine : ILine
	{
		private static readonly double TinyDouble = 1e-6;

		private Vector2 Start_;
		public Vector2 Start
		{
			get => Start_;
			set
			{
				Start_ = value;
				RecalculateInfiniteLine();
			}
		}

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

		public InfiniteLine(IWindow window, Vector2 start, Vector2 end, ApplicationColor color)
		{
			this.Window = window;
			this.GuiLine = window.CreateLine(new Vector2(), new Vector2(), 0, color);
			this.Start = start;
			this.End = end;
		}

		public void Dispose() => GuiLine.Dispose();

		private void RecalculateInfiniteLine()
		{
			Vector2 topLeft = new Vector2();
			Vector2 bottomRight = new Vector2(Window.Width, Window.Height);

			Ray2D StartEndRay = new Line2D(Start, End).AsRay();
			//StartEndRay = StartEndRay.WithStart(StartEndRay.Start + StartEndRay.Direction * TinyDouble);
			Vector2 end = Intersections2D.GetRayInsideBoxIntersection(StartEndRay, topLeft, bottomRight);
			if (end.Valid)
			{
				Ray2D endStartRay = new Line2D(end, Start).AsRay();
				//endStartRay = endStartRay.WithStart(endStartRay.Start + endStartRay.Direction * TinyDouble);
				Vector2 start = Intersections2D.GetRayInsideBoxIntersection(endStartRay, topLeft, bottomRight);
				GuiLine.Start = start;
				GuiLine.End = end;
			}
			else
			{
				Ray2D EndStartRay = new Line2D(End, Start).AsRay();
				//EndStartRay.WithStart(EndStartRay.Start + EndStartRay.Direction * TinyDouble);
				Vector2 start = Intersections2D.GetRayInsideBoxIntersection(EndStartRay, topLeft, bottomRight);
				if (start.Valid)
				{
					Ray2D startEndRay = new Line2D(start, End).AsRay();
					//startEndRay = startEndRay.WithStart(startEndRay.Start + startEndRay.Direction * TinyDouble);
					end = Intersections2D.GetRayInsideBoxIntersection(startEndRay, topLeft, bottomRight);
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
