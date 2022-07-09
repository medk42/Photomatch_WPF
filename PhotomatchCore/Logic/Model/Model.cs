using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PhotomatchCore.Logic.Model
{
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
