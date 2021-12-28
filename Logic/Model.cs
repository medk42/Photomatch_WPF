using MatrixVector;
using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Logic
{
	public class Point
	{
		public Vector3 Position { get; set; }
	}

	public class Line
	{
		public Point Start { get; set; }
		public Point End { get; set; }
	}

	public class Model
	{
		public delegate void AddLineEventHandler(Line line);
		public event AddLineEventHandler AddLineEvent;

		public delegate void AddPointEventHandler(Point line);
		public event AddPointEventHandler AddPointEvent;

		public List<Point> Points { get; } = new List<Point>();
		public List<Line> Lines { get; } = new List<Line>();

		public Point AddPoint(Vector3 position)
		{
			Point newPoint = new Point() { Position = position };

			Points.Add(newPoint);
			AddPointEvent?.Invoke(newPoint);

			return newPoint;
		}

		public Line AddLine(Point start, Point end)
		{
			Line newLine = new Line() { Start = start, End = end };

			Lines.Add(newLine);
			AddLineEvent?.Invoke(newLine);

			return newLine;
		}
	}
}
