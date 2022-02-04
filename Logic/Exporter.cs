﻿using Photomatch_ProofOfConcept_WPF.Utilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Logic
{

	/// <summary>
	/// Class for exporting model in .obj format.
	/// </summary>
	public static class Exporter
	{
		/// <summary>
		/// Texture resolution is selected by finding a bounding box of a polygon and then width is the highest horizontal distance 
		/// on model image and height is the highest vertical distance on model image. Then both are multiplied by ExportTextureResolutionMultiplier.
		/// </summary>
		private static readonly double ExportTextureResolutionMultiplier = 1.5;

		/// <summary>
		/// Get intersection between a ray and a face. Wrapper for Intersections3D.GetRayPolygonIntersection since that method
		/// requires Vector3 vertices.
		/// </summary>
		/// <returns>Intersection point between a ray and a face.</returns>
		private static RayPolygonIntersectionPoint GetRayFaceIntersection(Ray3D ray, Face face)
		{
			List<Vector3> vertices = new List<Vector3>();
			for (int j = 0; j < face.Count; j++)
				vertices.Add(face[j].Position);

			return Intersections3D.GetRayPolygonIntersection(ray, vertices, face.Normal);
		}

		/// <summary>
		/// Find and return the best perspective for specified face. 
		/// 
		/// Essentially the first perspective for which the ray from camera to Face.FacePoint of the face
		/// doesn't intersect any other face first. In other words, the first perspective that has FacePoint
		/// visible.
		/// </summary>
		/// <returns>Best perspective for specified face.</returns>
		private static PerspectiveData GetFacePerspective(Face face, List<PerspectiveData> perspectives, Model model)
		{
			foreach (PerspectiveData perspective in perspectives)
			{
				Vector2 facePointScreen = perspective.WorldToScreen(face.FacePoint);
				Ray3D ray = perspective.ScreenToWorldRay(facePointScreen);
				RayPolygonIntersectionPoint faceIntersection = GetRayFaceIntersection(ray, face);

				bool viable = true;
				foreach (Face otherFace in model.Faces)
				{
					if (otherFace != face)
					{
						RayPolygonIntersectionPoint compareFaceIntersection = GetRayFaceIntersection(ray, otherFace);
						if (compareFaceIntersection.IntersectedPolygon && compareFaceIntersection.RayRelative < faceIntersection.RayRelative)
						{
							viable = false;
							break;
						}
					}
				}

				if (viable)
					return perspective;
			}

			return null;
		}

		/// <summary>
		/// Calculate and return output texture resolution and perspective projection matrix for correction 
		/// of the perspective distortion of the texture for specified face and perspective.
		/// </summary>
		/// <param name="width">render width of the face texture</param>
		/// <param name="height">render height of the face texture</param>
		/// <returns>Perspective projection matrix for correction of the perspective distortion.</returns>
		private static Matrix3x3 GetFacePerspectiveProjectMatrix(Face face, PerspectiveData perspective, out int width, out int height)
		{
			Matrix3x3 rotate = Camera.RotateAlign(face.Normal, new Vector3(0, 0, 1));
			Matrix3x3 inverseRotate = rotate.Transposed();

			Vector3 min = new Vector3(double.PositiveInfinity, double.PositiveInfinity, 0);
			Vector3 max = new Vector3(double.NegativeInfinity, double.NegativeInfinity, 0);

			for (int j = 0; j < face.Count; j++)
			{
				Vector3 rotated = rotate * face[j].Position;
				if (rotated.X < min.X)
					min.X = rotated.X;
				if (rotated.X > max.X)
					max.X = rotated.X;
				if (rotated.Y < min.Y)
					min.Y = rotated.Y;
				if (rotated.Y > max.Y)
					max.Y = rotated.Y;
				min.Z = rotated.Z;
				max.Z = rotated.Z;
			}

			Vector2 topLeft = perspective.WorldToScreen(inverseRotate * min);
			Vector2 topRight = perspective.WorldToScreen(inverseRotate * min.WithX(max.X));
			Vector2 bottomLeft = perspective.WorldToScreen(inverseRotate * max.WithX(min.X));
			Vector2 bottomRight = perspective.WorldToScreen(inverseRotate * max);

			width = (int)(ExportTextureResolutionMultiplier * Math.Max((topRight - topLeft).Magnitude, (bottomRight - bottomLeft).Magnitude));
			height = (int)(ExportTextureResolutionMultiplier * Math.Max((bottomLeft - topLeft).Magnitude, (bottomRight - topRight).Magnitude));

			return ImageUtils.CalculateProjectiveTransformationMatrix(
				topLeft, topRight, bottomLeft, bottomRight,
				new Vector2(0, 0), new Vector2(width - 1, 0),
				new Vector2(0, height - 1), new Vector2(width - 1, height - 1)
			);
		}

		/// <summary>
		/// Generate UV coordinates for a specified face and perspective.
		/// </summary>
		/// <param name="project">matrix for perspective distortion correction of the texture</param>
		/// <param name="uvCoordinatesList">list to which generated UV coordinates will be added</param>
		/// <param name="width">render width of the face texture</param>
		/// <param name="height">render height of the face texture</param>
		private static void GenerateFacePerspectiveUVCoordinates(Face face, PerspectiveData perspective, Matrix3x3 project, List<Vector2> uvCoordinatesList, int width, int height)
		{
			foreach (Vertex v in face.UniqueVertices)
			{
				Vector2 screenPosition = perspective.WorldToScreen(v.Position);
				Vector3 scaledNewPosition = project * new Vector3(screenPosition.X, screenPosition.Y, 1);
				scaledNewPosition /= scaledNewPosition.Z;
				uvCoordinatesList.Add(new Vector2(scaledNewPosition.X / (width - 1), 1 - scaledNewPosition.Y / (height - 1)));
			}
		}

		/// <summary>
		/// Export perspective corrected texture for face with original image coordinates specified by project matrix, width and height.
		/// </summary>
		/// <param name="image">source image</param>
		/// <param name="project">perspective correction matrix</param>
		/// <param name="path">path to export texture to</param>
		/// <param name="width">texture width</param>
		/// <param name="height">texture height</param>
		private static void ExportProjectPerspectiveTexture(Image<Rgb24> image, Matrix3x3 project, string path, int width, int height)
		{
			using (var canvas = new Image<Rgb24>(width, height))
			{
				Matrix3x3 inverseProject = project.Adjugate();

				for (int y = 0; y < canvas.Height; y++)
				{
					for (int x = 0; x < canvas.Width; x++)
					{
						Vector3 p = inverseProject * new Vector3(x, y, 1);
						double u = p.X / p.Z;
						double v = p.Y / p.Z;
						int u_min = (int)Math.Floor(u);
						int u_max = (int)Math.Ceiling(u);
						int v_min = (int)Math.Floor(v);
						int v_max = (int)Math.Ceiling(v);
						double u_dist = u - u_min;
						double v_dist = v - v_min;

						if (u_min >= 0 && v_min >= 0 && u_max < image.Width && v_max < image.Height)
						{
							Vector3 resMin = (1 - u_dist) * image[u_min, v_min].AsVector3() + u_dist * image[u_max, v_min].AsVector3();
							Vector3 resMax = (1 - u_dist) * image[u_min, v_max].AsVector3() + u_dist * image[u_max, v_max].AsVector3();
							Vector3 res = (1 - v_dist) * resMin + v_dist * resMax;
							canvas[x, y] = new Rgb24((byte)res.X, (byte)res.Y, (byte)res.Z);
						}
					}
				}

				canvas.Save(path);
			}
		}

		/// <summary>
		/// Create .mtl file containing materials for face textures for the .obj export. 
		/// </summary>
		/// <param name="path">path to which export the .mtl file</param>
		/// <param name="invalidFaces">materials will not be exported for faces with specified indices</param>
		/// <param name="model">model from which the faces are being exported</param>
		private static void GenerateMtlFile(string path, List<int> invalidFaces, Model model)
		{
			using (var fileStream = File.Create(path))
			using (var writer = new StreamWriter(fileStream, Encoding.ASCII))
			{
				for (int i = 0; i < model.Faces.Count; i++)
				{
					if (!invalidFaces.Contains(i))
					{
						writer.WriteLine($"newmtl face{i}");
						writer.WriteLine($"\tmap_Kd face{i}.png");
						writer.WriteLine();
					}
				}
			}
		}

		/// <summary>
		/// Create .obj file containing vertices, UV coordinates and face mappings for both.
		/// </summary>
		/// <param name="path">path to which export the .obj file</param>
		/// <param name="fileNameNoExtension">export file name, so that .mtl file can be referenced</param>
		/// <param name="uvCoordinates">UV coordinates to export</param>
		/// <param name="invalidFaces">UV coordinates/textures will not be mapped for faces with specified indices</param>
		/// <param name="model">model which is being exported</param>
		private static void GenerateObjFile(string path, string fileNameNoExtension, List<Vector2> uvCoordinates, List<int> invalidFaces, Model model)
		{
			using (var fileStream = File.Create(path))
			using (var writer = new StreamWriter(fileStream, Encoding.ASCII))
			{
				writer.WriteLine($"mtllib ./{fileNameNoExtension}.mtl");
				writer.WriteLine();

				foreach (Vertex v in model.Vertices)
					writer.WriteLine($"v {v.Position.X.ToString(CultureInfo.InvariantCulture)} {v.Position.Z.ToString(CultureInfo.InvariantCulture)} {(-v.Position.Y).ToString(CultureInfo.InvariantCulture)}");


				writer.WriteLine();

				foreach (Vector2 uvCoord in uvCoordinates)
					writer.WriteLine($"vt {uvCoord.X.ToString(CultureInfo.InvariantCulture)} {uvCoord.Y.ToString(CultureInfo.InvariantCulture)}");

				writer.WriteLine();

				for (int i = 0, uvID = 1; i < model.Faces.Count; i++)
				{
					Face face = model.Faces[i];

					if (!invalidFaces.Contains(i))
						writer.WriteLine($"usemtl face{i}");

					foreach (Triangle triangle in face.Triangulated)
					{
						int aId = model.Vertices.IndexOf(triangle.A) + 1;
						int bId = model.Vertices.IndexOf(triangle.B) + 1;
						int cId = model.Vertices.IndexOf(triangle.C) + 1;

						int aUV = uvID + face.UniqueVertices.IndexOf(triangle.A);
						int bUV = uvID + face.UniqueVertices.IndexOf(triangle.B);
						int cUV = uvID + face.UniqueVertices.IndexOf(triangle.C);

						if (face.Reversed)
						{
							(aId, bId) = (bId, aId);
							(aUV, bUV) = (bUV, aUV);
						}

						if (invalidFaces.Contains(i))
							writer.WriteLine($"f {aId} {bId} {cId}");
						else
							writer.WriteLine($"f {aId}/{aUV} {bId}/{bUV} {cId}/{cUV}");
					}

					if (!invalidFaces.Contains(i))
						uvID += face.UniqueVertices.Count;
				}
			}
		}

		/// <summary>
		/// Export specified model to specified path. New folder with the same name as the .obj in filePath
		/// will be created and textures, .mtl file and .obj file will be stored there.
		/// </summary>
		/// <param name="model">model to export</param>
		/// <param name="filePath">file path to export model to containing .obj</param>
		/// <param name="logger">logger for logging progress</param>
		/// <param name="perspectives">existing perspectives from which we can project textures</param>
		public static void Export(Model model, string filePath, ILogger logger, List<PerspectiveData> perspectives)
		{
			try
			{
				string fileName = Path.GetFileName(filePath);
				string fileNameNoExtension = Path.GetFileNameWithoutExtension(filePath);
				string newFolderPath = Path.Combine(new FileInfo(filePath).Directory.FullName, fileNameNoExtension);

				Directory.CreateDirectory(newFolderPath);

				List<Vector2> uvCoordinates = new List<Vector2>();
				List<int> invalidFaces = new List<int>();

				for (int i = 0; i < model.Faces.Count; i++)
				{
					PerspectiveData selectedPerspective = GetFacePerspective(model.Faces[i], perspectives, model);

					logger.Log("Export Model", $"Exporting texture {i + 1}/{model.Faces.Count}...", LogType.Progress);

					if (selectedPerspective != null)
					{
						int width, height;
						Matrix3x3 project = GetFacePerspectiveProjectMatrix(model.Faces[i], selectedPerspective, out width, out height);
						GenerateFacePerspectiveUVCoordinates(model.Faces[i], selectedPerspective, project, uvCoordinates, width, height);
						ExportProjectPerspectiveTexture(selectedPerspective.Image, project, Path.Combine(newFolderPath, $"face{i}.png"), width, height);
					}
					else
					{
						invalidFaces.Add(i);
					}
				}

				logger.Log("Export Model", $"Generating .mtl file...", LogType.Progress);
				GenerateMtlFile(Path.Combine(newFolderPath, fileNameNoExtension + ".mtl"), invalidFaces, model);

				logger.Log("Export Model", $"Generating .obj file...", LogType.Progress);
				GenerateObjFile(Path.Combine(newFolderPath, fileName), fileNameNoExtension, uvCoordinates, invalidFaces, model);
			}
			catch (Exception ex)
			{
				if (ex is UnauthorizedAccessException)
					logger.Log("Export Model", "Unauthorized access to file.", LogType.Warning);
				else if (ex is IOException)
					logger.Log("Export Model", "Save operation was not successful.", LogType.Warning);
				else if (ex is ArgumentException || ex is DirectoryNotFoundException || ex is NotSupportedException)
					logger.Log("Export Model", "Path is invalid.", LogType.Warning);
				else if (ex is PathTooLongException)
					logger.Log("Export Model", "Path is too long.", LogType.Warning);
				else throw ex;

				return;
			}

			logger.Log("Export Model", "Successfully exported model.", LogType.Info);
		}
	}
}