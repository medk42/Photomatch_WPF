using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Logic.Model
{
	/// <summary>
	/// Struct representing a triangle, containing 3 vertices, for face triangulation.
	/// </summary>
	public struct Triangle
	{
		public Vertex A { get; set; }
		public Vertex B { get; set; }
		public Vertex C { get; set; }
	}
}
