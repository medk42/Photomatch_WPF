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
	public delegate void FaceRemovedEventHandler(Face face);
	public delegate void VertexPositionChangedEventHandler(Vector3 position, int id);

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
		public event VertexPositionChangedEventHandler VertexPositionChangedEvent;
		public event FaceRemovedEventHandler FaceRemovedEvent;

		public Vertex this[int i]
		{
			get => Vertices[i];
		}

		public int Count => Vertices.Count;

		private Vector3 Normal_;
		public Vector3 Normal { get => Normal_; }

		private Vector3 TriangleMiddle_;
		public Vector3 TriangleMiddle { get => TriangleMiddle_; }

		private List<Vertex> Vertices { get; } = new List<Vertex>();

		public Face(List<Vertex> vertices)
		{
			this.Vertices = vertices;

			for (int i = 0; i < Vertices.Count; i++)
			{
				Vertex v = Vertices[i];
				v.VertexRemovedEvent += (v) => Remove();

				if (i < 3)
				{
					v.PositionChangedEvent += (position) =>
					{
						RecalculateProperties();
						VertexPositionChangedEvent?.Invoke(position, i);
					};
				}
				else
					v.PositionChangedEvent += (position) => VertexPositionChangedEvent?.Invoke(position, i);
			}

			RecalculateProperties();
		}

		private void RecalculateProperties()
		{
			Normal_ = Vector3.Cross(Vertices[1].Position - Vertices[0].Position, Vertices[2].Position - Vertices[0].Position).Normalized();
			TriangleMiddle_ = (Vertices[0].Position + Vertices[1].Position + Vertices[2].Position) / 3;
		}

		public void Remove()
		{
			FaceRemovedEvent?.Invoke(this);
		}
	}

	public class Model : ISafeSerializable<Model>
	{
		public delegate void AddEdgeEventHandler(Edge edge);
		public event AddEdgeEventHandler AddEdgeEvent;

		public delegate void AddVertexEventHandler(Vertex vertex);
		public event AddVertexEventHandler AddVertexEvent;

		public delegate void AddFaceEventHandler(Face face);
		public event AddFaceEventHandler AddFaceEvent;

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
			Edge newEdge = new Edge(start, end);
			newEdge.EdgeRemovedEvent += RemoveEdge;

			Edges.Add(newEdge);
			AddEdgeEvent?.Invoke(newEdge);

			return newEdge;
		}

		public Face AddFace(List<Vertex> vertices)
		{
			Face newFace = new Face(vertices);
			newFace.FaceRemovedEvent += RemoveFace;

			Faces.Add(newFace);
			AddFaceEvent?.Invoke(newFace);

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

		private void RemoveFace(Face f)
		{
			f.FaceRemovedEvent -= RemoveFace;
			Faces.Remove(f);
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
