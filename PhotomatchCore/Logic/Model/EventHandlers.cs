using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Logic.Model
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
}
