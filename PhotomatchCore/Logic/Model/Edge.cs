using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Logic.Model
{
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
}
