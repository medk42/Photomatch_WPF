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

		public void Dispose()
		{
			VertexRemovedEvent = null;
			PositionChangedEvent = null;
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

		public void Dispose()
		{
			EdgeRemovedEvent = null;
			StartPositionChangedEvent = null;
			EndPositionChangedEvent = null;
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

		public bool Reversed
		{
			get => FacesFront.Count % 2 == 1;
		}

		private Vector3 Normal_;
		public Vector3 Normal { get => Normal_; }

		private Vector3 FacePoint_;
		public Vector3 FacePoint { get => FacePoint_; }

		private List<Vertex> Vertices = new List<Vertex>();
		private List<Face> FacesFront = new List<Face>();

		public Face(List<Vertex> vertices)
		{
			this.Vertices = new List<Vertex>(vertices);

			for (int i = 0; i < Vertices.Count; i++)
			{
				Vertex v = Vertices[i];
				v.VertexRemovedEvent += (v) => Remove();

				if (i < 3)
				{
					int iCopy = i;
					v.PositionChangedEvent += (position) =>
					{
						RecalculateProperties();
						VertexPositionChangedEvent?.Invoke(position, iCopy);
					};
				}
				else
					v.PositionChangedEvent += (position) => VertexPositionChangedEvent?.Invoke(position, i);
			}

			RecalculateProperties();
		}

		internal void FaceAdded(Face other)
		{
			if (other == this)
				return;

			Ray3D ray = new Ray3D(FacePoint, Normal);

			List<Vector3> vertices = new List<Vector3>();
			for (int i = 0; i < other.Count; i++)
				vertices.Add(other[i].Position);

			RayPolygonIntersectionPoint point = Intersections3D.GetRayPolygonIntersection(ray, vertices, other.Normal);
			if (point.IntersectedPolygon && point.RayRelative >= 0)
				FacesFront.Add(other);
		}

		internal void FaceChanged(Face other)
		{
			if (FacesFront.Remove(other))
				FaceAdded(other);
		}

		internal void FaceRemoved(Face other)
		{
			FacesFront.Remove(other);
		}

		private void RecalculateProperties()
		{
			Normal_ = Vector3.Cross(Vertices[1].Position - Vertices[0].Position, Vertices[2].Position - Vertices[0].Position).Normalized();
			FacePoint_ = (0.5 * Vertices[0].Position +  0.16 * Vertices[1].Position +  0.34 * Vertices[2].Position);
		}

		public void Remove()
		{
			FaceRemovedEvent?.Invoke(this);
		}

		public void Dispose()
		{
			FaceRemovedEvent = null;
			VertexPositionChangedEvent = null;
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
			newFace.VertexPositionChangedEvent += (position, id) => FaceUpdated(newFace, id, position);

			foreach (Face face in Faces)
			{
				face.FaceAdded(newFace);
				newFace.FaceAdded(face);
			}

			Faces.Add(newFace);
			AddFaceEvent?.Invoke(newFace);

			return newFace;
		}

		private void FaceUpdated(Face face, int vertexId, Vector3 position)
		{
			foreach (Face f in Faces)
			{
				f.FaceChanged(face);
				face.FaceChanged(f);
			}					
		}

		private void RemoveVertex(Vertex v)
		{
			v.VertexRemovedEvent -= RemoveVertex;
			Vertices.Remove(v);
			v.Dispose();
		}

		private void RemoveEdge(Edge e)
		{
			e.EdgeRemovedEvent -= RemoveEdge;
			Edges.Remove(e);
			e.Dispose();
		}

		private void RemoveFace(Face f)
		{
			f.FaceRemovedEvent -= RemoveFace;
			Faces.Remove(f);

			foreach (Face face in Faces)
				face.FaceRemoved(f);

			f.Dispose();
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

			writer.Write(Faces.Count);
			foreach (Face f in Faces)
			{
				writer.Write(f.Count);
				for (int i = 0; i < f.Count; i++)
					writer.Write(Vertices.IndexOf(f[i]));
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

			int faceCount = reader.ReadInt32();
			for (int i = 0; i < faceCount; i++)
			{
				List<Vertex> vertices = new List<Vertex>();
				int faceVertexCount = reader.ReadInt32();
				for (int j = 0; j < faceVertexCount; j++)
					vertices.Add(Vertices[reader.ReadInt32()]);
				AddFace(vertices);
			}
		}

		public void Dispose()
		{
			Vertices.Clear();
			Edges.Clear();
			Faces.Clear();
		}
	}
}
