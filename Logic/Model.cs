using MatrixVector;
using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Logic
{
	public delegate void PositionChangedEventHandler(Vector3 position);

	public class Vertex
	{
		public event PositionChangedEventHandler PositionChangedEvent;

		private Vector3 Position_;
		public Vector3 Position
		{
			get => Position_;
			set
			{
				Position_ = value;
				PositionChangedEvent?.Invoke(value);
			}
		}
	}

	public class Edge
	{
		public event PositionChangedEventHandler StartPositionChangedEvent;
		public event PositionChangedEventHandler EndPositionChangedEvent;

		public Vertex Start { get; private set; }
		public Vertex End { get; private set; }

		public Edge(Vertex start, Vertex end)
		{
			this.Start = start;
			this.End = end;

			Start.PositionChangedEvent += (position) => StartPositionChangedEvent?.Invoke(position);
			End.PositionChangedEvent += (position) => EndPositionChangedEvent?.Invoke(position);
		}
	}

	public class Model
	{
		public delegate void AddEdgeEventHandler(Edge line);
		public event AddEdgeEventHandler AddEdgeEvent;

		public delegate void AddVertexEventHandler(Vertex line);
		public event AddVertexEventHandler AddVertexEvent;

		public List<Vertex> Vertices { get; } = new List<Vertex>();
		public List<Edge> Edges { get; } = new List<Edge>();

		public Vertex AddVertex(Vector3 position)
		{
			Vertex newPoint = new Vertex() { Position = position };

			Vertices.Add(newPoint);
			AddVertexEvent?.Invoke(newPoint);

			return newPoint;
		}

		public Edge AddEdge(Vertex start, Vertex end)
		{
			Edge newLine = new Edge(start, end);

			Edges.Add(newLine);
			AddEdgeEvent?.Invoke(newLine);

			return newLine;
		}
	}
}
