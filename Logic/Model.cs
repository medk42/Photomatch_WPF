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

		public List<Edge> ConnectedEdges { get; } = new List<Edge>();

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
		public bool RemoveVerticesOnRemove { get; set; } = true;

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

		private double CalculateAngleOfVertex(Vector2 prev, Vector2 act, Vector2 next)
		{
			Vector2 ab = prev - act;
			Vector2 cb = next - act;
			double dot = Vector2.Dot(ab, cb);
			double pcross = cb.X * ab.Y - cb.Y * ab.X;
			double angle = Math.Atan2(pcross, dot);
			return angle;
		}

		private void AddEartip(Vector2 prev, Vector2 act, Vector2 next, List<Vector2> vertices, HashSet<int> earTips, int id)
		{
			foreach (Vector2 vertex in vertices)
			{
				if (vertex == prev || vertex == act || vertex == next)
					continue;
				if (Intersections2D.IsPointInsideTriangle(vertex, prev, act, next))
					return;
			}

			earTips.Add(id);
		}

		private void InitializeTriangulate(List<Vector2> vertices, int[] verticesMap, int[] prevVertex, int[] nextVertex, HashSet<int> earTips, double[] angles, out Matrix3x3 inverseRotate, out double zCoord)
		{
			Matrix3x3 rotate = Camera.RotateAlign(Reversed ? -Normal : Normal, new Vector3(0, 0, 1));
			List<Vertex> uniqueVertices = new List<Vertex>();

			inverseRotate = rotate.Transposed();
			zCoord = (rotate * Vertices[0].Position).Z;

			for (int i = 0; i < verticesMap.Length; i++)
			{
				if (!uniqueVertices.Contains(Vertices[i]))
				{
					Vector3 rotated = rotate * Vertices[i].Position;
					vertices.Add(new Vector2(rotated.X, rotated.Y));
					uniqueVertices.Add(Vertices[i]);
				}

				verticesMap[i] = uniqueVertices.IndexOf(Vertices[i]);

				int iAddOne = i + 1 < verticesMap.Length ? i + 1 : 0;
				nextVertex[i] = iAddOne;
				prevVertex[iAddOne] = i;
			}

			for (int i = 0; i < verticesMap.Length; i++)
			{
				int prevId = i;
				int actId = i + 1 < verticesMap.Length ? i + 1 : i + 1 - verticesMap.Length;
				int nextId = i + 2 < verticesMap.Length ? i + 2 : i + 2 - verticesMap.Length;

				Vector2 prev = vertices[verticesMap[prevId]];
				Vector2 act = vertices[verticesMap[actId]];
				Vector2 next = vertices[verticesMap[nextId]];

				angles[actId] = CalculateAngleOfVertex(prev, act, next);

				if (angles[actId] >= 0)
					AddEartip(prev, act, next, vertices, earTips, actId);
			}
		}

		public List<Triangle> Triangulate()
		{
			List<Triangle> triangles = new List<Triangle>();

			List<Vector2> vertices = new List<Vector2>();
			int[] verticesMap = new int[Vertices.Count];
			int[] nextVertex = new int[Vertices.Count];
			int[] prevVertex = new int[Vertices.Count];

			double[] angles = new double[Vertices.Count];
			HashSet<int> earTips = new HashSet<int>();

			Matrix3x3 inverseRotate;
			double zCoord;

			InitializeTriangulate(vertices, verticesMap, prevVertex, nextVertex, earTips, angles, out inverseRotate, out zCoord);

			for (int i = 0; i < angles.Length - 2; i++)
			{
				int smallestId = -1;
				foreach (int ear in earTips)
					if (smallestId == -1 || angles[ear] < angles[smallestId])
						smallestId = ear;

				int prevId = prevVertex[smallestId];
				int nextId = nextVertex[smallestId];

				Vector2 prevVect = vertices[verticesMap[prevId]];
				Vector2 actVect = vertices[verticesMap[smallestId]];
				Vector2 nextVect = vertices[verticesMap[nextId]];

				triangles.Add(new Triangle() { 
					A = inverseRotate * new Vector3(prevVect.X, prevVect.Y, zCoord), 
					B = inverseRotate * new Vector3(actVect.X, actVect.Y, zCoord), 
					C = inverseRotate * new Vector3(nextVect.X, nextVect.Y, zCoord)
				});

				earTips.Remove(smallestId);
				earTips.Remove(prevId);
				earTips.Remove(nextId);

				nextVertex[prevId] = nextId;
				prevVertex[nextId] = prevId;

				Vector2 prevPrevVect = vertices[verticesMap[prevVertex[prevId]]];
				Vector2 nextNextVect = vertices[verticesMap[nextVertex[nextId]]];

				angles[prevId] = CalculateAngleOfVertex(prevPrevVect, prevVect, nextVect);
				angles[nextId] = CalculateAngleOfVertex(prevVect, nextVect, nextNextVect);

				if (angles[prevId] >= 0)
					AddEartip(prevPrevVect, prevVect, nextVect, vertices, earTips, prevId);
				if (angles[nextId] >= 0)
					AddEartip(prevVect, nextVect, nextNextVect, vertices, earTips, nextId);
			}

			return triangles;
		}
	}

	public struct Triangle
	{
		public Vector3 A { get; set; }
		public Vector3 B { get; set; }
		public Vector3 C { get; set; }
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
			foreach (Edge e in start.ConnectedEdges)
				if (e.Start == end || e.End == end)
					return null;
			
			Edge newEdge = new Edge(start, end);
			newEdge.EdgeRemovedEvent += RemoveEdge;

			start.ConnectedEdges.Add(newEdge);
			end.ConnectedEdges.Add(newEdge);

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

		private void CheckNoConnections(Vertex v)
		{
			if (v.ConnectedEdges.Count == 0)
				v.Remove();
		}

		private void CheckTwoConnections(Vertex v)
		{
			if (v.ConnectedEdges.Count == 2)
			{
				Vertex start = v.ConnectedEdges[0].Start == v ? v.ConnectedEdges[0].End : v.ConnectedEdges[0].Start;
				Vertex end = v.ConnectedEdges[1].Start == v ? v.ConnectedEdges[1].End : v.ConnectedEdges[1].Start;
				Vector3 lineANormal = (start.Position - v.Position).Normalized();
				Vector3 lineBNormal = (v.Position - end.Position).Normalized();
				if ((lineANormal - lineBNormal).Magnitude < 1e-6)
				{
					AddEdge(start, end);
					v.Remove();
				}
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
			e.Start.ConnectedEdges.Remove(e);
			e.End.ConnectedEdges.Remove(e);

			e.EdgeRemovedEvent -= RemoveEdge;
			Edges.Remove(e);

			e.Dispose();

			if (e.RemoveVerticesOnRemove)
			{
				CheckNoConnections(e.Start);
				CheckNoConnections(e.End);
				CheckTwoConnections(e.Start);
				CheckTwoConnections(e.End);
			}
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
