using MatrixVector;
using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Logic
{
	public class Vertex
	{
		public Vector3 Position { get; set; }
	}

	public class Edge
	{
		public Vertex Start { get; set; }
		public Vertex End { get; set; }
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
			Edge newLine = new Edge() { Start = start, End = end };

			Edges.Add(newLine);
			AddEdgeEvent?.Invoke(newLine);

			return newLine;
		}
	}
}
