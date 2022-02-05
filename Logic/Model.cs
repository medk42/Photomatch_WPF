using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Photomatch_ProofOfConcept_WPF.Utilities;

namespace Photomatch_ProofOfConcept_WPF.Logic
{
	/// <summary>
	/// Event handler for Vector3 position change.
	/// </summary>
	/// <param name="position">new position</param>
	public delegate void PositionChangedEventHandler(Vector3 position);

	/// <summary>
	/// Event handler for vertex removal.
	/// </summary>
	/// <param name="vertex">removed vertex</param>
	public delegate void VertexRemovedEventHandler(Vertex vertex);

	/// <summary>
	/// Event handler for edge removal.
	/// </summary>
	/// <param name="edge">removed edge</param>
	public delegate void EdgeRemovedEventHandler(Edge edge);

	/// <summary>
	/// Event handler for face removal.
	/// </summary>
	/// <param name="face">removed face</param>
	public delegate void FaceRemovedEventHandler(Face face);

	/// <summary>
	/// Event handler for position change of vertex with specified ID (for Face).
	/// </summary>
	/// <param name="position">new position</param>
	/// <param name="id">id of changed vertex</param>
	public delegate void VertexPositionChangedEventHandler(Vector3 position, int id);

	/// <summary>
	/// Event handler for user setting reverse of a face manually.
	/// </summary>
	/// <param name="reverse">true if user set face to reversed</param>
	public delegate void FaceUserReverseSetEventHandler(bool reverse);

	/// <summary>
	/// Class containing data about 3D model vertex.
	/// </summary>
	public class Vertex
	{
		/// <summary>
		/// Event called on vertex position change.
		/// </summary>
		public event PositionChangedEventHandler PositionChangedEvent;

		/// <summary>
		/// Event called on vertex removal.
		/// </summary>
		public event VertexRemovedEventHandler VertexRemovedEvent;

		/// <summary>
		/// List of edges connected to this vertex.
		/// </summary>
		public List<Edge> ConnectedEdges { get; } = new List<Edge>();

		/// <summary>
		/// Position of the vertex.
		/// </summary>
		public Vector3 Position
		{
			get => Position_;
			set
			{
				Position_ = value;
				PositionChangedEvent?.Invoke(value);
			}
		}
		private Vector3 Position_;

		/// <summary>
		/// Remove the vertex from the model.
		/// </summary>
		public void Remove()
		{
			VertexRemovedEvent?.Invoke(this);
		}

		/// <summary>
		/// Dispose of vertex events.
		/// </summary>
		public void Dispose()
		{
			VertexRemovedEvent = null;
			PositionChangedEvent = null;
		}
	}

	/// <summary>
	/// Class containing data about 3D model edge.
	/// </summary>
	public class Edge
	{
		/// <summary>
		/// Event called on start vertex position change.
		/// </summary>
		public event PositionChangedEventHandler StartPositionChangedEvent;

		/// <summary>
		/// Event called on end vertex position change.
		/// </summary>
		public event PositionChangedEventHandler EndPositionChangedEvent;

		/// <summary>
		/// Event called on edge removal.
		/// </summary>
		public event EdgeRemovedEventHandler EdgeRemovedEvent;

		/// <summary>
		/// Start vertex of the edge.
		/// </summary>
		public Vertex Start { get; private set; }

		/// <summary>
		/// End vertex of the edge.
		/// </summary>
		public Vertex End { get; private set; }

		/// <summary>
		/// True if, after this edge is removed, vertices with 0 edges or vertices splitting another edge should be removed, false otherwise.
		/// </summary>
		public bool RemoveVerticesOnRemove { get; set; } = true;

		private PositionChangedEventHandler StartPositionChangedEventHandler, EndPositionChangedEventHandler;
		private VertexRemovedEventHandler VertexRemovedEventHandler;

		/// <summary>
		/// Create edge with start and end vertices.
		/// </summary>
		public Edge(Vertex start, Vertex end)
		{
			this.Start = start;
			this.End = end;

			this.StartPositionChangedEventHandler = (position) => StartPositionChangedEvent?.Invoke(position);
			this.EndPositionChangedEventHandler = (position) => EndPositionChangedEvent?.Invoke(position);
			this.VertexRemovedEventHandler = (vertex) => Remove();

			Start.PositionChangedEvent += StartPositionChangedEventHandler;
			End.PositionChangedEvent += EndPositionChangedEventHandler;

			Start.VertexRemovedEvent += VertexRemovedEventHandler;
			End.VertexRemovedEvent += VertexRemovedEventHandler;
		}

		/// <summary>
		/// Remove the edge from the model. Also called if either of the vertices is removed.
		/// </summary>
		public void Remove()
		{
			EdgeRemovedEvent?.Invoke(this);
		}

		/// <summary>
		/// Dispose of edge events and event registrations on vertices.
		/// </summary>
		public void Dispose()
		{
			EdgeRemovedEvent = null;
			StartPositionChangedEvent = null;
			EndPositionChangedEvent = null;

			Start.PositionChangedEvent -= StartPositionChangedEventHandler;
			End.PositionChangedEvent -= EndPositionChangedEventHandler;

			Start.VertexRemovedEvent -= VertexRemovedEventHandler;
			End.VertexRemovedEvent -= VertexRemovedEventHandler;
		}
	}

	/// <summary>
	/// Class containing data about 3D model face.
	/// </summary>
	public class Face
	{
		/// <summary>
		/// Event called on any vertex position change - with the new position and id of the changed vertex.
		/// </summary>
		public event VertexPositionChangedEventHandler VertexPositionChangedEvent;

		/// <summary>
		/// Event called on face removal.
		/// </summary>
		public event FaceRemovedEventHandler FaceRemovedEvent;

		/// <summary>
		/// Event called on user manually changing reverse;
		/// </summary>
		public event FaceUserReverseSetEventHandler FaceUserReverseSetEvent;

		/// <summary>
		/// Get vertex with ID i.
		/// </summary>
		/// <param name="i">ID of vertex to return</param>
		/// <returns>Vertex with ID i.</returns>
		public Vertex this[int i]
		{
			get => Vertices[i];
		}

		/// <summary>
		/// Get the number of vertices.
		/// </summary>
		public int Count => Vertices.Count;

		/// <summary>
		/// True if user drew the face reversed (in clockwise direction, looking from outside).
		/// Necessary for face exporting, since we need to export in anti-clockwise direction.
		/// If the face is reversed, triangulation is also reversed!
		/// </summary>
		public bool Reversed
		{
			get => UserReversed ?? (FacesFront.Count % 2 == 1);
		}

		public bool? UserReversed { get; set; } = null;

		/// <summary>
		/// Face normal. Calculated from first 3 vertices and reversed if the normal is facing the wrong way. 
		/// If the face is correctly designed by user (in anti-clockwise direction), normal should be pointing 
		/// out of the model. Cached.
		/// </summary>
		public Vector3 Normal { get; private set; }

		/// <summary>
		/// A vector that is guaranteed to be inside the face (calculated as some position inside the first triangulated triangle). Cached.
		/// </summary>
		public Vector3 FacePoint { get; private set; }

		/// <summary>
		/// Triangulation of the face. Cashed.
		/// </summary>
		public List<Triangle> Triangulated { get; } = new List<Triangle>();

		/// <summary>
		/// Unique vertices of the face. Cashed.
		/// </summary>
		public List<Vertex> UniqueVertices { get; } = new List<Vertex>();

		private List<Vertex> Vertices = new List<Vertex>();
		private List<Face> FacesFront = new List<Face>();
		private VertexRemovedEventHandler VertexRemovedEventHandler;
		private List<PositionChangedEventHandler> PositionChangedEventHandlers = new List<PositionChangedEventHandler>();

		/// <summary>
		/// Create a face with a list of vertices and recalculate properties.
		/// </summary>
		public Face(List<Vertex> vertices)
		{
			this.Vertices = new List<Vertex>(vertices);
			this.VertexRemovedEventHandler = (v) => Remove();

			for (int i = 0; i < Vertices.Count; i++)
			{
				Vertex v = Vertices[i];
				v.VertexRemovedEvent += VertexRemovedEventHandler;

				int iCopy = i;
				PositionChangedEventHandler handler = (position) =>
				{
					RecalculateProperties();
					VertexPositionChangedEvent?.Invoke(position, iCopy);
				};
				PositionChangedEventHandlers.Add(handler);
				v.PositionChangedEvent += handler;

				if (!UniqueVertices.Contains(v))
					UniqueVertices.Add(v);
			}

			RecalculateProperties();
		}

		/// <summary>
		/// Reverse face by user action. Face reversal will no longer be calculated automatically.
		/// </summary>
		public void UserReverse()
		{
			UserReversed = !Reversed;
			FaceUserReverseSetEvent?.Invoke(UserReversed.Value);
		}

		/// <summary>
		/// Update faces in front of this face, when a face is added (to calculate which side of the face is looking in/out of the model).
		/// </summary>
		/// <param name="other">added face</param>
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

		/// <summary>
		/// Update faces in front of this face, when a face is updated (to calculate which side of the face is looking in/out of the model).
		/// </summary>
		/// <param name="other">updated face</param>
		internal void FaceChanged(Face other)
		{
			if (FacesFront.Remove(other))
				FaceAdded(other);
		}

		/// <summary>
		/// Update faces in front of this face, when a face is removed (to calculate which side of the face is looking in/out of the model).
		/// </summary>
		/// <param name="other">removed face</param>
		internal void FaceRemoved(Face other)
		{
			FacesFront.Remove(other);
		}

		/// <summary>
		/// Recalculate Normal, FacePoint and Triangulate properties.
		/// </summary>
		private void RecalculateProperties()
		{
			Normal = Vector3.Cross(Vertices[1].Position - Vertices[0].Position, Vertices[2].Position - Vertices[0].Position).Normalized();

			Matrix3x3 rotate = Camera.RotateAlign(Normal, new Vector3(0, 0, 1));
			List<Vector2> vertices = new List<Vector2>();
			foreach (Vertex v in Vertices)
				vertices.Add(new Vector2(rotate * v.Position));
			if (Intersections2D.IsClockwise(vertices))
				Normal = -Normal;

			Triangulate();

			FacePoint = (0.5 * Triangulated[0].A.Position + 0.16 * Triangulated[0].B.Position + 0.34 * Triangulated[0].C.Position);
		}

		/// <summary>
		/// Remove the face from the model. Also called if any of the vertices is removed.
		/// </summary>
		public void Remove()
		{
			FaceRemovedEvent?.Invoke(this);
		}

		/// <summary>
		/// Dispose of face events and event registrations on vertices.
		/// </summary>
		public void Dispose()
		{
			FaceRemovedEvent = null;
			VertexPositionChangedEvent = null;
			FaceUserReverseSetEvent = null;

			for (int i = 0; i < Count; i++)
			{
				Vertices[i].VertexRemovedEvent -= VertexRemovedEventHandler;
				Vertices[i].PositionChangedEvent -= PositionChangedEventHandlers[i];
			}
		}

		/// <summary>
		/// Calculate angle at current vertex (considering anti-clockwise vertex order).
		/// </summary>
		/// <param name="prev">previous vertex</param>
		/// <param name="act">current vertex</param>
		/// <param name="next">next vertex</param>
		/// <returns>The angle at current vertex.</returns>
		private double CalculateAngleOfVertex(Vector2 prev, Vector2 act, Vector2 next)
		{
			Vector2 ab = prev - act;
			Vector2 cb = next - act;
			double dot = Vector2.Dot(ab, cb);
			double pcross = cb.X * ab.Y - cb.Y * ab.X;
			double angle = Math.Atan2(pcross, dot);
			return angle;
		}

		/// <summary>
		/// Add current vertex as eartip if no other vertex is inside a triangle created by previous, current and next vertex.
		/// </summary>
		/// <param name="prev">previous vertex</param>
		/// <param name="act">current vertex</param>
		/// <param name="next">next vertex</param>
		/// <param name="vertices">list of face vertices</param>
		/// <param name="earTips">list of eartips to add to</param>
		/// <param name="id">id of the vertex</param>
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

		/// <summary>
		/// Initialize ear-clipping triangulation.
		/// </summary>
		/// <param name="vertices">face vertices</param>
		/// <param name="verticesMap">initialize a map from an id of face vertex to an id of unique vertex (since vertices can repeat)</param>
		/// <param name="prevVertex">initialize an array containing id of previous vertex ([i]=i-1)</param>
		/// <param name="nextVertex">initialize an array containing id of next vertex ([i]=i+1)</param>
		/// <param name="earTips">
		/// fill a list with all found eartips - vertices with less than 180° angles which (with their neighbors) form a triangle
		/// that doesn't contain any other vertex. In other words, vertices that form a triangle that is inside the face.
		/// </param>
		/// <param name="angles">initialize an array containing the inner angle at each vertex</param>
		private void InitializeTriangulate(List<Vector2> vertices, int[] verticesMap, int[] prevVertex, int[] nextVertex, HashSet<int> earTips, double[] angles)
		{
			Matrix3x3 rotate = Camera.RotateAlign(Normal, new Vector3(0, 0, 1));

			foreach (Vertex v in UniqueVertices)
				vertices.Add(new Vector2(rotate * v.Position));

			for (int i = 0; i < verticesMap.Length; i++)
			{
				verticesMap[i] = UniqueVertices.IndexOf(Vertices[i]);

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

		/// <summary>
		/// Use ear-clipping triangulation to triangulate this face.
		/// Using https://arxiv.org/ftp/arxiv/papers/1212/1212.6038.pdf.
		/// </summary>
		private void Triangulate()
		{
			Triangulated.Clear();

			List<Vector2> vertices = new List<Vector2>();
			int[] verticesMap = new int[Vertices.Count];
			int[] nextVertex = new int[Vertices.Count];
			int[] prevVertex = new int[Vertices.Count];

			double[] angles = new double[Vertices.Count];
			HashSet<int> earTips = new HashSet<int>();

			InitializeTriangulate(vertices, verticesMap, prevVertex, nextVertex, earTips, angles);

			for (int i = 0; i < angles.Length - 2; i++)
			{
				int smallestId = -1;
				foreach (int ear in earTips)
					if (smallestId == -1 || angles[ear] < angles[smallestId])
						smallestId = ear;

				int prevId = prevVertex[smallestId];
				int nextId = nextVertex[smallestId];

				Triangulated.Add(new Triangle()
				{
					A = Vertices[prevId],
					B = Vertices[smallestId],
					C = Vertices[nextId]
				});

				earTips.Remove(smallestId);
				earTips.Remove(prevId);
				earTips.Remove(nextId);

				nextVertex[prevId] = nextId;
				prevVertex[nextId] = prevId;

				Vector2 prevPrevVect = vertices[verticesMap[prevVertex[prevId]]];
				Vector2 prevVect = vertices[verticesMap[prevId]];
				Vector2 nextVect = vertices[verticesMap[nextId]];
				Vector2 nextNextVect = vertices[verticesMap[nextVertex[nextId]]];

				angles[prevId] = CalculateAngleOfVertex(prevPrevVect, prevVect, nextVect);
				angles[nextId] = CalculateAngleOfVertex(prevVect, nextVect, nextNextVect);

				if (angles[prevId] >= 0)
					AddEartip(prevPrevVect, prevVect, nextVect, vertices, earTips, prevId);
				if (angles[nextId] >= 0)
					AddEartip(prevVect, nextVect, nextNextVect, vertices, earTips, nextId);
			}
		}
	}

	/// <summary>
	/// Struct representing a triangle, containing 3 vertices, for face triangulation.
	/// </summary>
	public struct Triangle
	{
		public Vertex A { get; set; }
		public Vertex B { get; set; }
		public Vertex C { get; set; }
	}

	/// <summary>
	/// Class containing data about a 3D model.
	/// </summary>
	public class Model : ISafeSerializable<Model>
	{
		/// <summary>
		/// Event handler for when edge is added.
		/// </summary>
		/// <param name="edge">added edge</param>
		public delegate void AddEdgeEventHandler(Edge edge);

		/// <summary>
		/// Event called when edge is added.
		/// </summary>
		public event AddEdgeEventHandler AddEdgeEvent;


		/// <summary>
		/// Event handler for when vertex is added.
		/// </summary>
		/// <param name="vertex">added vertex</param>
		public delegate void AddVertexEventHandler(Vertex vertex);

		/// <summary>
		/// Event called when vertex is added.
		/// </summary>
		public event AddVertexEventHandler AddVertexEvent;


		/// <summary>
		/// Event handler for when face is added.
		/// </summary>
		/// <param name="face">added face</param>
		public delegate void AddFaceEventHandler(Face face);

		/// <summary>
		/// Event called when face is added.
		/// </summary>
		public event AddFaceEventHandler AddFaceEvent;


		/// <summary>
		/// Event handler for when model is changed in any way.
		/// </summary>
		public delegate void ModelChangedEventHandler();

		/// <summary>
		/// Event called when model is changed in any way.
		/// </summary>
		public event ModelChangedEventHandler ModelChangedEvent;



		/// <summary>
		/// Model vertices - only for enumeration, use AddVertex for adding and Vertex.Remove for removal.
		/// </summary>
		public List<Vertex> Vertices { get; } = new List<Vertex>();

		/// <summary>
		/// Model edges - only for enumeration, use AddEdge for adding and Edge.Remove for removal.
		/// </summary>
		public List<Edge> Edges { get; } = new List<Edge>();

		/// <summary>
		/// Model faces - only for enumeration, use AddFace for adding and Face.Remove for removal.
		/// </summary>
		public List<Face> Faces { get; } = new List<Face>();

		/// <summary>
		/// Add vertex with specified position. First added vertex will be protected against removal.
		/// </summary>
		/// <returns>Added vertex.</returns>
		public Vertex AddVertex(Vector3 position)
		{
			Vertex newPoint = new Vertex() { Position = position };

			if (Vertices.Count > 0)
				newPoint.VertexRemovedEvent += RemoveVertex;

			newPoint.PositionChangedEvent += (newPos) => ModelChangedEvent?.Invoke();

			Vertices.Add(newPoint);
			AddVertexEvent?.Invoke(newPoint);

			ModelChangedEvent?.Invoke();

			return newPoint;
		}

		/// <summary>
		/// Split the edge and add a new vertex with specified position in the middle.
		/// </summary>
		/// <param name="vertexPosition">new vertex position</param>
		/// <param name="edge">edge to add to</param>
		/// <returns>Added vertex.</returns>
		public Vertex AddVertexToEdge(Vector3 vertexPosition, Edge edge)
		{
			Vertex newVertex = AddVertex(vertexPosition);
			Vertex start = edge.Start;
			Vertex end = edge.End;
			edge.RemoveVerticesOnRemove = false;
			edge.Remove();
			AddEdge(start, newVertex);
			AddEdge(newVertex, end);

			return newVertex;
		}

		/// <summary>
		/// Add edge with specified start and end vertices. If an edge with specified vertices already exists or start == end,
		/// do not create an edge and return null.
		/// </summary>
		/// <returns>Added edge or null if edge already exists or start == end.</returns>
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

			ModelChangedEvent?.Invoke();

			return newEdge;
		}

		/// <summary>
		/// Add face with specified vertices.
		/// </summary>
		/// <returns>Added face.</returns>
		public Face AddFace(List<Vertex> vertices)
		{
			Face newFace;
			try
			{
				newFace = new Face(vertices);
			}
			catch (Exception)
			{
				return null;
			}

			newFace.FaceRemovedEvent += RemoveFace;
			newFace.VertexPositionChangedEvent += (position, id) => FaceUpdated(newFace);
			newFace.FaceUserReverseSetEvent += (reverse) => ModelChangedEvent?.Invoke();

			foreach (Face face in Faces)
			{
				face.FaceAdded(newFace);
				newFace.FaceAdded(face);
			}

			Faces.Add(newFace);
			AddFaceEvent?.Invoke(newFace);

			ModelChangedEvent?.Invoke();

			return newFace;
		}

		/// <summary>
		/// Notify all faces about an updated face. Notify updated face about all other faces (since they all changed from its perspective).
		/// </summary>
		/// <param name="face">updated face</param>
		private void FaceUpdated(Face face)
		{
			foreach (Face f in Faces)
			{
				f.FaceChanged(face);
				face.FaceChanged(f);
			}
		}

		/// <summary>
		/// Remove a specified vertex if it has no edges connected.
		/// </summary>
		/// <param name="v">specified vertex</param>
		private void CheckNoConnections(Vertex v)
		{
			if (v.ConnectedEdges.Count == 0)
				v.Remove();
		}

		/// <summary>
		/// Remove a specified vertex if it lies on an edge (has two edges connected that have very similar direction)
		/// and re-create the edge.
		/// </summary>
		/// <param name="v">specified vertex</param>
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

		/// <summary>
		/// Remove a specified vertex when Vertex.Remove is called. 
		/// </summary>
		/// <param name="v">specified vertex</param>
		private void RemoveVertex(Vertex v)
		{
			v.VertexRemovedEvent -= RemoveVertex;
			Vertices.Remove(v);
			v.Dispose();

			ModelChangedEvent?.Invoke();
		}

		/// <summary>
		/// Remove a specified edge when Edge.Remove is called. Remove edge vertices if they have no other edges or lie on another edge.
		/// </summary>
		/// <param name="e">specified edge</param>
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

			ModelChangedEvent?.Invoke();
		}

		/// <summary>
		/// Remove a specified face when Face.Remove is called.
		/// </summary>
		/// <param name="f">specified face</param>
		private void RemoveFace(Face f)
		{
			f.FaceRemovedEvent -= RemoveFace;
			Faces.Remove(f);

			foreach (Face face in Faces)
				face.FaceRemoved(f);

			f.Dispose();

			ModelChangedEvent?.Invoke();
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

				writer.Write(f.UserReversed.HasValue);
				if (f.UserReversed.HasValue)
					writer.Write(f.UserReversed.Value);
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
				Face face = AddFace(vertices);

				if (reader.ReadBoolean())
					face.UserReversed = reader.ReadBoolean();
			}
		}

		/// <summary>
		/// Dispose of model events. Dispose of all vertices, edges and faces.
		/// </summary>
		public void Dispose()
		{
			foreach (Vertex vertex in Vertices)
				vertex.Dispose();
			Vertices.Clear();

			foreach (Edge edge in Edges)
				edge.Dispose();
			Edges.Clear();

			foreach (Face face in Faces)
				face.Dispose();
			Faces.Clear();

			ModelChangedEvent = null;
			AddVertexEvent = null;
			AddEdgeEvent = null;
			AddFaceEvent = null;
		}
	}
}
