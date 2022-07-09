using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Logic.Model
{
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
}
