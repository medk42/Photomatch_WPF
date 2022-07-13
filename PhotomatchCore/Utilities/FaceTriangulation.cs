using System;
using System.Collections.Generic;
using System.Text;
using PhotomatchCore.Logic.Model;
using PhotomatchCore.Logic.Perspective;

namespace PhotomatchCore.Utilities
{
	/// <summary>
	/// Struct representing a triangle, containing 3 vertex indices, for face triangulation.
	/// </summary>
	struct TriangleIndices
	{
		public int A { get; set; }
		public int B { get; set; }
		public int C { get; set; }
	}

	public static class FaceTriangulation
	{
		private static readonly double TriangulateMinEdgeAngle = Math.PI / 6;

		/// <summary>
		/// Calculate angle at current vertex (considering anti-clockwise vertex order).
		/// </summary>
		/// <param name="prev">previous vertex</param>
		/// <param name="act">current vertex</param>
		/// <param name="next">next vertex</param>
		/// <returns>The angle at current vertex.</returns>
		private static double CalculateAngleOfVertex(Vector2 prev, Vector2 act, Vector2 next)
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
		private static void AddEartip(Vector2 prev, Vector2 act, Vector2 next, List<Vector2> vertices, HashSet<int> earTips, int id)
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
		private static void InitializeTriangulate(Vector3 normal, List<Vertex> faceVertices, List<Vertex> uniqueFaceVertices, List<Vector2> vertices, int[] verticesMap, int[] prevVertex, int[] nextVertex, HashSet<int> earTips, double[] angles)
		{
			Matrix3x3 rotate = Camera.RotateAlign(normal, new Vector3(0, 0, 1));

			foreach (Vertex v in uniqueFaceVertices)
				vertices.Add(new Vector2(rotate * v.Position));

			for (int i = 0; i < verticesMap.Length; i++)
			{
				verticesMap[i] = uniqueFaceVertices.IndexOf(faceVertices[i]);

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
		/// Calculate triangle angles and return them as a Vector3 (angleA, angleB, angleC).
		/// </summary>
		/// <param name="triangle">triangle to calculate</param>
		/// <param name="vertices">list of unique vertices</param>
		private static Vector3 CalculateTriangleAngles(TriangleIndices triangle, List<Vector2> vertices)
		{
			double aAngle = CalculateAngleOfVertex(vertices[triangle.C], vertices[triangle.A], vertices[triangle.B]);
			double bAngle = CalculateAngleOfVertex(vertices[triangle.A], vertices[triangle.B], vertices[triangle.C]);
			double cAngle = CalculateAngleOfVertex(vertices[triangle.B], vertices[triangle.C], vertices[triangle.A]);

			return new Vector3(aAngle, bAngle, cAngle);
		}

		/// <summary>
		/// Returns true if edge defined by two vertices edgeStart and edgeEnd is one of the triangle edges, false otherwise.
		/// </summary>
		private static bool IsSideOfTriangle(TriangleIndices triangle, int edgeStart, int edgeEnd)
		{
			return
				(triangle.A == edgeStart || triangle.B == edgeStart || triangle.C == edgeStart) &&
				(triangle.A == edgeEnd || triangle.B == edgeEnd || triangle.C == edgeEnd);
		}

		/// <summary>
		/// Get the last vertex of a triangle.
		/// </summary>
		/// <param name="triangle">triangle to get last vertex of</param>
		/// <param name="vertexA">one of the triangle vertices</param>
		/// <param name="vertexB">one of the triangle vertices</param>
		/// <returns>The last vertex of the triangle or -1 if vertexA and vertexB are not two vertices of the triangle.</returns>
		private static int GetLastVertex(TriangleIndices triangle, int vertexA, int vertexB)
		{
			if ((triangle.A == vertexA && triangle.B == vertexB) || (triangle.A == vertexB && triangle.B == vertexA))
				return triangle.C;
			if ((triangle.A == vertexA && triangle.C == vertexB) || (triangle.A == vertexB && triangle.C == vertexA))
				return triangle.B;
			if ((triangle.C == vertexA && triangle.B == vertexB) || (triangle.C == vertexB && triangle.B == vertexA))
				return triangle.A;

			return -1;
		}

		/// <summary>
		/// Switch around two triangles sharing one edge if minimum angle of the new configuration is larger, otherwise just add triangle to triangles.
		/// </summary>
		/// <param name="triangle">triangle that is being added</param>
		/// <param name="existingTriangle">triangle already in triangles, to be removed and replaced by two new triangles if new configuration is better</param>
		/// <param name="triangles">list of existing triangles to add result to</param>
		/// <param name="vertices">list of unique vertices</param>
		/// <param name="start">start of the edge from point of view of triangle</param>
		/// <param name="end">end of the edge from point of view of triangle</param>
		private static void SwitchQuadrilateral(TriangleIndices triangle, TriangleIndices existingTriangle, List<TriangleIndices> triangles, List<Vector2> vertices, int start, int end)
		{

			int triangleLastVertex = GetLastVertex(triangle, start, end);
			int existingTriangleLastVertex = GetLastVertex(existingTriangle, start, end);

			TriangleIndices triangleA = new TriangleIndices() { A = triangleLastVertex, B = start, C = existingTriangleLastVertex };
			TriangleIndices triangleB = new TriangleIndices() { A = existingTriangleLastVertex, B = end, C = triangleLastVertex };

			Vector3 anglesA = CalculateTriangleAngles(triangleA, vertices);
			Vector3 anglesB = CalculateTriangleAngles(triangleB, vertices);
			double minAngle = Math.Min(Math.Min(Math.Min(anglesA.X, anglesA.Y), anglesA.Z), Math.Min(Math.Min(anglesB.X, anglesB.Y), anglesB.Z));

			Vector3 anglesOldA = CalculateTriangleAngles(triangle, vertices);
			Vector3 anglesOldB = CalculateTriangleAngles(existingTriangle, vertices);
			double minAngleOld = Math.Min(Math.Min(Math.Min(anglesOldA.X, anglesOldA.Y), anglesOldA.Z), Math.Min(Math.Min(anglesOldB.X, anglesOldB.Y), anglesOldB.Z));

			if (minAngleOld < minAngle)
			{
				triangles.Remove(existingTriangle);
				triangles.Add(triangleA);
				triangles.Add(triangleB);
			}
			else
				triangles.Add(triangle);
		}

		/// <summary>
		/// Add a triangle to existing triangles. Try to switch edges with another triangle, if the smallest
		/// angle in the new triangle is too small. 
		/// </summary>
		/// <param name="triangle">triangle to add</param>
		/// <param name="triangles">list of already existing triangles</param>
		/// <param name="vertices">list of unique vertices</param>
		private static void EdgeSwapAdd(TriangleIndices triangle, List<TriangleIndices> triangles, List<Vector2> vertices)
		{
			Vector3 angles = CalculateTriangleAngles(triangle, vertices);
			double minAngle = Math.Min(angles.X, Math.Min(angles.Y, angles.Z));

			if (minAngle < TriangulateMinEdgeAngle)
			{
				int start, end;
				if (angles.X >= angles.Y && angles.X >= angles.Z)
				{
					start = triangle.B;
					end = triangle.C;
				}
				else if (angles.Y >= angles.X && angles.Y >= angles.Z)
				{
					start = triangle.C;
					end = triangle.A;
				}
				else
				{
					start = triangle.A;
					end = triangle.B;
				}

				foreach (TriangleIndices existingTriangle in triangles)
				{
					if (IsSideOfTriangle(existingTriangle, start, end))
					{
						SwitchQuadrilateral(triangle, existingTriangle, triangles, vertices, start, end);
						return;
					}
				}
			}

			triangles.Add(triangle);
		}

		/// <summary>
		/// Use ear-clipping triangulation to triangulate this face.
		/// Using https://arxiv.org/ftp/arxiv/papers/1212/1212.6038.pdf.
		/// </summary>
		public static void Triangulate(List<Triangle> triangulated, List<Vertex> uniqueFaceVertices, List<Vertex> faceVertices, Vector3 normal)
		{
			List<TriangleIndices> triangles = new List<TriangleIndices>();

			List<Vector2> vertices = new List<Vector2>();
			int[] verticesMap = new int[faceVertices.Count];
			int[] nextVertex = new int[faceVertices.Count];
			int[] prevVertex = new int[faceVertices.Count];

			double[] angles = new double[faceVertices.Count];
			HashSet<int> earTips = new HashSet<int>();

			InitializeTriangulate(normal, faceVertices, uniqueFaceVertices, vertices, verticesMap, prevVertex, nextVertex, earTips, angles);

			for (int i = 0; i < angles.Length - 2; i++)
			{
				int smallestId = -1;
				foreach (int ear in earTips)
					if (smallestId == -1 || angles[ear] < angles[smallestId])
						smallestId = ear;

				int prevId = prevVertex[smallestId];
				int nextId = nextVertex[smallestId];

				EdgeSwapAdd(new TriangleIndices()
				{
					A = verticesMap[prevId],
					B = verticesMap[smallestId],
					C = verticesMap[nextId]
				}, triangles, vertices);

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

			triangulated.Clear();
			foreach (TriangleIndices triangle in triangles)
				triangulated.Add(new Triangle()
				{
					A = uniqueFaceVertices[triangle.A],
					B = uniqueFaceVertices[triangle.B],
					C = uniqueFaceVertices[triangle.C]
				});
		}
	}
}
