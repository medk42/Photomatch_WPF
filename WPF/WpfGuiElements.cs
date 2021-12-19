using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

using MatrixVector;
using WpfExtensions;
using WpfInterfaces;
using GuiInterfaces;

namespace WpfGuiElements
{
	public class WpfLine : ILine, IScalable
	{
		public LineGeometry Line { get; }
		public EllipseGeometry StartEllipse { get; }
		public EllipseGeometry EndEllipse { get; }

		private double EndRadius;

		public WpfLine(Point Start, Point End, double endRadius)
		{
			Line = new LineGeometry();
			Line.StartPoint = Start;
			Line.EndPoint = End;

			EndRadius = endRadius;

			if (endRadius != 0)
			{
				StartEllipse = new EllipseGeometry(Start, endRadius, endRadius);
				EndEllipse = new EllipseGeometry(End, endRadius, endRadius);
			}
			else
			{
				StartEllipse = null;
				EndEllipse = null;
			}
		}

		public Vector2 Start
		{
			get => Line.StartPoint.AsVector2();
			set
			{
				var point = value.AsPoint();
				Line.StartPoint = point;
				if (StartEllipse != null)
					StartEllipse.Center = point;
			}
		}

		public Vector2 End
		{
			get => Line.EndPoint.AsVector2();
			set
			{
				var point = value.AsPoint();
				Line.EndPoint = point;
				if (EndEllipse != null)
					EndEllipse.Center = point;
			}
		}

		public void SetNewScale(double scale)
		{
			if (StartEllipse != null)
			{
				StartEllipse.RadiusX = EndRadius / scale;
				StartEllipse.RadiusY = EndRadius / scale;
			}
			if (EndEllipse != null)
			{
				EndEllipse.RadiusX = EndRadius / scale;
				EndEllipse.RadiusY = EndRadius / scale;
			}
		}
	}
}
