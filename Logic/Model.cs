using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Photomatch_ProofOfConcept_WPF.Utilities;

namespace Photomatch_ProofOfConcept_WPF.Logic
{
	public delegate void PositionChangedEventHandler(Vector3 position);
	public delegate void VertexRemovedEventHandler(Vertex vertex);
	public delegate void EdgeRemovedEventHandler(Edge edge);

	public class Vertex
	{
		public event PositionChangedEventHandler PositionChangedEvent;
		public event VertexRemovedEventHandler VertexRemovedEvent;

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

		public void Remove()
		{
			VertexRemovedEvent?.Invoke(this);
		}
	}

	public class Edge
	{
		public event PositionChangedEventHandler StartPositionChangedEvent;
		public event PositionChangedEventHandler EndPositionChangedEvent;
		public event EdgeRemovedEventHandler EdgeRemovedEvent;

		public Vertex Start { get; private set; }
		public Vertex End { get; private set; }

		public Edge(Vertex start, Vertex end)
		{
			this.Start = start;
			this.End = end;

			Start.PositionChangedEvent += (position) => StartPositionChangedEvent?.Invoke(position);
			End.PositionChangedEvent += (position) => EndPositionChangedEvent?.Invoke(position);

			Start.VertexRemovedEvent += (vertex) => Remove();
			End.VertexRemovedEvent += (vetex) => Remove();
		}

		public void Remove()
		{
			EdgeRemovedEvent?.Invoke(this);
		}
	}

	public class Face
	{
		public List<Vertex> Vertices { get; } = new List<Vertex>();

		public Face(List<Vertex> vertices)
		{
			this.Vertices = vertices;
		}
	}

	public class Model : ISafeSerializable<Model>
	{
		public delegate void AddEdgeEventHandler(Edge line);
		public event AddEdgeEventHandler AddEdgeEvent;

		public delegate void AddVertexEventHandler(Vertex line);
		public event AddVertexEventHandler AddVertexEvent;

		public List<Vertex> Vertices { get; } = new List<Vertex>();
		public List<Edge> Edges { get; } = new List<Edge>();
		public List<Face> Faces { get; } = new List<Face>();

		/// <summary>
		/// First added vertex will be protected against removal.
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public Vertex AddVertex(Vector3 position)
		{
			Vertex newPoint = new Vertex() { Position = position };

			if (Vertices.Count > 0)
				newPoint.VertexRemovedEvent += RemoveVertex;

			Vertices.Add(newPoint);
			AddVertexEvent?.Invoke(newPoint);

			return newPoint;
		}

		public Edge AddEdge(Vertex start, Vertex end)
		{
			Edge newLine = new Edge(start, end);
			newLine.EdgeRemovedEvent += RemoveEdge;

			Edges.Add(newLine);
			AddEdgeEvent?.Invoke(newLine);

			return newLine;
		}

		public Face AddFace(List<Vertex> vertices)
		{
			Face newFace = new Face(vertices);

			Faces.Add(newFace);

			return newFace;
		}

		private void RemoveVertex(Vertex v)
		{
			v.VertexRemovedEvent -= RemoveVertex;
			Vertices.Remove(v);
		}

		private void RemoveEdge(Edge e)
		{
			e.EdgeRemovedEvent -= RemoveEdge;
			Edges.Remove(e);
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Vertices.Count);
			foreach (Vertex v in Vertices)
				v.Position.Serialize(writer);

			writer.Write(Edges.Count);
			foreach (Edge e in Edges)
			{
				writer.Write(Vertices.IndexOf(e.Start));
				writer.Write(Vertices.IndexOf(e.End));
			}
		}

		public void Deserialize(BinaryReader reader)
		{
			int vertexCount = reader.ReadInt32();
			for (int i = 0; i < vertexCount; i++)
				AddVertex(ISafeSerializable<Vector3>.CreateDeserialize(reader));

			int edgeCount = reader.ReadInt32();
			for (int i = 0; i < edgeCount; i++)
			{
				int startIndex = reader.ReadInt32();
				int endIndex = reader.ReadInt32();
				AddEdge(Vertices[startIndex], Vertices[endIndex]);
			}
		}

		public void Dispose()
		{
			Vertices.Clear();
			Edges.Clear();
		}
	}
}
