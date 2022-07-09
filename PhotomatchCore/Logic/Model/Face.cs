using PhotomatchCore.Logic.Perspective;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Logic.Model
{
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

			try
			{
				RecalculateProperties();
			}
			catch (Exception e)
			{
				Dispose();
				throw e;
			}
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

			FaceTriangulation.Triangulate(Triangulated, UniqueVertices, Vertices, Normal);

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
	}
}
