using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Photomatch_ProofOfConcept_WPF.Logic;
using Photomatch_ProofOfConcept_WPF.Utilities;
using System.Globalization;
using SixLabors.ImageSharp.PixelFormats;
using Photomatch_ProofOfConcept_WPF.Gui.GuiControls.Helper;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls
{
	public class MasterControl : Actions
	{
		private enum ProjectState { None, NewProject, NamedProject }

		private static readonly ulong ProjectFileChecksum = 0x54_07_02_47_23_43_94_42;
		private static readonly string NewProjectName = "new project...";
		private static readonly double ExportTextureResolutionMultiplier = 1.5;

		private MasterGUI Gui;
		private ILogger Logger;
		private List<ImageWindow> Windows;
		private ProjectState State;
		private string ProjectPath;
		private Model Model;
		private DesignTool DesignTool;
		private ModelCreationTool ModelCreationTool;
		private CameraModelCalibrationTool CameraModelCalibrationTool;

		public MasterControl(MasterGUI gui)
		{
			this.Gui = gui;
			this.Logger = gui;
			this.Windows = new List<ImageWindow>();
			this.State = ProjectState.None;
			this.ProjectPath = null;
			this.Model = new Model();
			this.Model.AddVertex(new Vector3());
			this.DesignTool = DesignTool.CameraCalibration;
			this.ModelCreationTool = ModelCreationTool.Edge;
			this.CameraModelCalibrationTool = CameraModelCalibrationTool.CalibrateOrigin;

			Gui.DisplayProjectName(NewProjectName);
		}

		public void NewProject_Pressed() => Reset();

		public void LoadImage_Pressed()
		{
			string filePath = Gui.GetImageFilePath();
			if (filePath == null)
			{
				Logger.Log("Load Image", "No file was selected.", LogType.Info);
				return;
			}

			Image<Rgb24> image = null;
			try
			{
				image = Image.Load<Rgb24>(filePath);
			}
			catch (Exception ex)
			{
				if (ex is FileNotFoundException)
					Logger.Log("Load Image", "File not found.", LogType.Warning);
				else if (ex is UnknownImageFormatException)
					Logger.Log("Load Image", "Incorrect or unsupported image format.", LogType.Warning);
				else
					throw ex;
			}

			if (image != null)
			{
				Logger.Log("Load Image", "File loaded successfully.", LogType.Info);
				Windows.Add(new ImageWindow(new PerspectiveData(image, filePath), Gui, Logger, Model, DesignTool, ModelCreationTool, CameraModelCalibrationTool));

				if (State == ProjectState.None)
					State = ProjectState.NewProject;

				Gui.DisplayModelCreationTool(ModelCreationTool);
				Gui.DisplayCameraModelCalibrationTool(CameraModelCalibrationTool);
				Gui.DisplayDesignTool(DesignTool);
			}
		}

		public void SaveProject_Pressed()
		{
			switch (State)
			{
				case ProjectState.None:
					Logger.Log("Save Project", "Nothing to save.", LogType.Warning);
					return;
				case ProjectState.NewProject:
					SaveProjectAs_Pressed();
					break;
				case ProjectState.NamedProject:
					if (!SaveProject(ProjectPath))
						return;
					break;
				default:
					throw new NotImplementedException("Unknown ProjectState");
			}

			Logger.Log("Save Project", "Successfully saved project.", LogType.Info);
		}

		public void SaveProjectAs_Pressed()
		{
			switch (State)
			{
				case ProjectState.None:
					Logger.Log("Save Project", "Nothing to save.", LogType.Warning);
					return;
				case ProjectState.NewProject:
				case ProjectState.NamedProject:
					string filePath = Gui.GetSaveProjectFilePath();
					if (filePath == null)
					{
						Logger.Log("Save Project", "No file was selected.", LogType.Info);
						return;
					}
					if (!SaveProject(filePath))
						return;
					State = ProjectState.NamedProject;
					ProjectPath = filePath;

					string projectName = Path.GetFileName(filePath);
					Gui.DisplayProjectName(projectName);
					break;
				default:
					throw new NotImplementedException("Unknown ProjectState");
			}

			Logger.Log("Save Project", "Successfully saved project.", LogType.Info);
		}

		private bool SaveProject(string fileName)
		{
			try
			{
				using (var fileStream = File.Create(fileName))
				using (var writer = new BinaryWriter(fileStream))
				{
					writer.Write(ProjectFileChecksum);
					Model.Serialize(writer);
					writer.Write((int)DesignTool);
					writer.Write(Windows.Count);
					foreach (ImageWindow window in Windows)
					{
						window.Perspective.Serialize(writer);
					}
				}
			}
			catch (Exception ex)
			{
				if (ex is UnauthorizedAccessException)
					Logger.Log("Save Project", "Unauthorized access to file.", LogType.Warning);
				else if (ex is IOException)
					Logger.Log("Save Project", "Save operation was not successful.", LogType.Warning);
				else if (ex is ArgumentException || ex is DirectoryNotFoundException || ex is NotSupportedException)
					Logger.Log("Save Project", "Path is invalid.", LogType.Warning);
				else if (ex is PathTooLongException)
					Logger.Log("Save Project", "Path is too long.", LogType.Warning);
				else throw ex;

				return false;
			}

			return true;
		}

		public void LoadProject_Pressed()
		{
			string filePath = Gui.GetLoadProjectFilePath();
			if (filePath == null)
			{
				Logger.Log("Load Project", "No file was selected.", LogType.Info);
				return;
			}

			this.Reset();

			try
			{
				using (var fileStream = File.OpenRead(filePath))
				{
					var reader = new BinaryReader(fileStream);

					ulong checksum = reader.ReadUInt64();
					if (ProjectFileChecksum != checksum)
					{
						Logger.Log("Load Project", "Invalid file.", LogType.Warning);
						return;
					}

					Model = ISafeSerializable<Model>.CreateDeserialize(reader);

					DesignTool = (DesignTool)reader.ReadInt32();
					Gui.DisplayDesignTool(DesignTool);

					int windowCount = reader.ReadInt32();
					for (int i = 0; i < windowCount; i++)
					{
						PerspectiveData perspective = ISafeSerializable<PerspectiveData>.CreateDeserialize(reader);
						Windows.Add(new ImageWindow(perspective, Gui, Logger, Model, DesignTool, ModelCreationTool, CameraModelCalibrationTool));
					}
				}

				string projectName = Path.GetFileName(filePath);
				Gui.DisplayProjectName(projectName);

				State = ProjectState.NamedProject;
				ProjectPath = filePath;
				Logger.Log("Load Project", $"Successfully loaded project {projectName}.", LogType.Info);
			}
			catch (Exception ex)
			{
				this.Reset();

				if (ex is UnauthorizedAccessException)
					Logger.Log("Load Project", "Unauthorized access to file or path was directory.", LogType.Warning);
				else if (ex is IOException || ex is ArgumentOutOfRangeException)
					Logger.Log("Load Project", "Invalid file.", LogType.Warning);
				else if (ex is ArgumentException || ex is DirectoryNotFoundException || ex is NotSupportedException)
					Logger.Log("Load Project", "Path is invalid.", LogType.Warning);
				else if (ex is PathTooLongException)
					Logger.Log("Load Project", "Path is too long.", LogType.Warning);
				else if (ex is FileNotFoundException)
					Logger.Log("Load Project", "File not found.", LogType.Warning);
				else
					Logger.Log("Load Project", "Invalid file.", LogType.Warning);
			}
		}

		private RayPolygonIntersectionPoint GetRayFaceIntersection(Ray3D ray, Face face)
		{
			List<Vector3> vertices = new List<Vector3>();
			for (int j = 0; j < face.Count; j++)
				vertices.Add(face[j].Position);

			return Intersections3D.GetRayPolygonIntersection(ray, vertices, face.Normal);
		}

		private ImageWindow GetFaceWindow(Face face)
		{
			foreach (ImageWindow window in Windows)
			{
				Vector2 facePointScreen = window.Perspective.WorldToScreen(face.FacePoint);
				Ray3D ray = window.Perspective.ScreenToWorldRay(facePointScreen);
				RayPolygonIntersectionPoint faceIntersection = GetRayFaceIntersection(ray, face);

				bool viable = true;
				foreach (Face otherFace in Model.Faces)
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
					return window;
			}

			return null;
		}

		private Matrix3x3 GetFaceWindowProjectMatrix(Face face, ImageWindow window, out int width, out int height)
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

			Vector2 topLeft = window.Perspective.WorldToScreen(inverseRotate * min);
			Vector2 topRight = window.Perspective.WorldToScreen(inverseRotate * min.WithX(max.X));
			Vector2 bottomLeft = window.Perspective.WorldToScreen(inverseRotate * max.WithX(min.X));
			Vector2 bottomRight = window.Perspective.WorldToScreen(inverseRotate * max);

			width = (int)(ExportTextureResolutionMultiplier * Math.Max((topRight - topLeft).Magnitude, (bottomRight - bottomLeft).Magnitude));
			height = (int)(ExportTextureResolutionMultiplier * Math.Max((bottomLeft - topLeft).Magnitude, (bottomRight - topRight).Magnitude));

			return ImageUtils.CalculateProjectiveTransformationMatrix(
				topLeft, topRight, bottomLeft, bottomRight,
				new Vector2(0, 0), new Vector2(width - 1, 0),
				new Vector2(0, height - 1), new Vector2(width - 1, height - 1)
			);
		}

		private void GenerateFaceWindowUVCoordinates(Face face, ImageWindow window, Matrix3x3 project, List<Vector2> uvCoordinatesList, int width, int height)
		{
			foreach (Vertex v in face.UniqueVertices)
			{
				Vector2 screenPosition = window.Perspective.WorldToScreen(v.Position);
				Vector3 scaledNewPosition = project * new Vector3(screenPosition.X, screenPosition.Y, 1);
				scaledNewPosition /= scaledNewPosition.Z;
				uvCoordinatesList.Add(new Vector2(scaledNewPosition.X / (width - 1), 1 - scaledNewPosition.Y / (height - 1)));
			}
		}

		private void ExportProjectWindowTexture(Image<Rgb24> image, Matrix3x3 project, string path, int width, int height)
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

		private void GenerateMtlFile(string path, List<int> invalidFaces)
		{
			using (var fileStream = File.Create(path))
			using (var writer = new StreamWriter(fileStream, Encoding.ASCII))
			{
				for (int i = 0; i < Model.Faces.Count; i++)
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

		private void GenerateObjFile(string path, string fileNameNoExtension, List<Vector2> uvCoordinates, List<int> invalidFaces)
		{
			using (var fileStream = File.Create(path))
			using (var writer = new StreamWriter(fileStream, Encoding.ASCII))
			{
				writer.WriteLine($"mtllib ./{fileNameNoExtension}.mtl");
				writer.WriteLine();

				foreach (Vertex v in Model.Vertices)
					writer.WriteLine($"v {v.Position.X.ToString(CultureInfo.InvariantCulture)} {v.Position.Z.ToString(CultureInfo.InvariantCulture)} {(-v.Position.Y).ToString(CultureInfo.InvariantCulture)}");


				writer.WriteLine();

				foreach (Vector2 uvCoord in uvCoordinates)
					writer.WriteLine($"vt {uvCoord.X.ToString(CultureInfo.InvariantCulture)} {uvCoord.Y.ToString(CultureInfo.InvariantCulture)}");

				writer.WriteLine();

				for (int i = 0, uvID = 1; i < Model.Faces.Count; i++)
				{
					Face face = Model.Faces[i];

					if (!invalidFaces.Contains(i))
						writer.WriteLine($"usemtl face{i}");

					foreach (Triangle triangle in face.Triangulated)
					{
						int aId = Model.Vertices.IndexOf(triangle.A) + 1;
						int bId = Model.Vertices.IndexOf(triangle.B) + 1;
						int cId = Model.Vertices.IndexOf(triangle.C) + 1;

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

		public void ExportModel_Pressed()
		{
			if (State == ProjectState.None)
			{
				Logger.Log("Export Model", "Nothing to export.", LogType.Info);
				return;
			}

			string filePath = Gui.GetModelExportFilePath();

			try
			{
				string fileName = Path.GetFileName(filePath);
				string fileNameNoExtension = Path.GetFileNameWithoutExtension(filePath);
				string newFolderPath = Path.Combine(new FileInfo(filePath).Directory.FullName, fileNameNoExtension);

				Directory.CreateDirectory(newFolderPath);

				List<Vector2> uvCoordinates = new List<Vector2>();
				List<int> invalidFaces = new List<int>();

				for (int i = 0; i < Model.Faces.Count; i++)
				{
					ImageWindow selectedWindow = GetFaceWindow(Model.Faces[i]);

					if (selectedWindow != null)
					{
						int width, height;
						Matrix3x3 project = GetFaceWindowProjectMatrix(Model.Faces[i], selectedWindow, out width, out height);
						GenerateFaceWindowUVCoordinates(Model.Faces[i], selectedWindow, project, uvCoordinates, width, height);
						ExportProjectWindowTexture(selectedWindow.Perspective.Image, project, Path.Combine(newFolderPath, $"face{i}.png"), width, height);
					}
					else
					{
						invalidFaces.Add(i);
					}
				}

				GenerateMtlFile(Path.Combine(newFolderPath, fileNameNoExtension + ".mtl"), invalidFaces);
				GenerateObjFile(Path.Combine(newFolderPath, fileName), fileNameNoExtension, uvCoordinates, invalidFaces);
			}
			catch (Exception ex)
			{
				if (ex is UnauthorizedAccessException)
					Logger.Log("Export Model", "Unauthorized access to file.", LogType.Warning);
				else if (ex is IOException)
					Logger.Log("Export Model", "Save operation was not successful.", LogType.Warning);
				else if (ex is ArgumentException || ex is DirectoryNotFoundException || ex is NotSupportedException)
					Logger.Log("Export Model", "Path is invalid.", LogType.Warning);
				else if (ex is PathTooLongException)
					Logger.Log("Export Model", "Path is too long.", LogType.Warning);
				else throw ex;

				return;
			}

			Logger.Log("Export Model", "Successfully exported model.", LogType.Info);
		}

		public void DesignTool_Changed(DesignTool newDesignTool)
		{
			if (this.DesignTool != newDesignTool)
			{
				this.DesignTool = newDesignTool;

				Gui.DisplayDesignTool(newDesignTool);

				foreach (ImageWindow window in Windows)
					window.DesignTool_Changed(newDesignTool);
			}
		}

		public void ModelCreationTool_Changed(ModelCreationTool newModelCreationTool)
		{
			if (this.ModelCreationTool != newModelCreationTool)
			{
				this.ModelCreationTool = newModelCreationTool;

				foreach (ImageWindow window in Windows)
					window.ModelCreationTool_Changed(newModelCreationTool);
			}
		}

		public void CameraModelCalibrationTool_Changed(CameraModelCalibrationTool newCameraModelCalibrationTool)
		{
			if (this.CameraModelCalibrationTool != newCameraModelCalibrationTool)
			{
				this.CameraModelCalibrationTool = newCameraModelCalibrationTool;

				foreach (ImageWindow window in Windows)
					window.CameraModelCalibrationTool_Changed(newCameraModelCalibrationTool);
			}
		}

		public void Reset()
		{
			foreach (ImageWindow window in Windows)
				window.Dispose();

			Windows.Clear();
			State = ProjectState.None;
			ProjectPath = null;
			Model.Dispose();
			Model.AddVertex(new Vector3());

			Gui.DisplayProjectName(NewProjectName);

			DesignTool = DesignTool.CameraCalibration;
			ModelCreationTool = ModelCreationTool.Edge;
			CameraModelCalibrationTool = CameraModelCalibrationTool.CalibrateOrigin;
			Gui.DisplayModelCreationTool(ModelCreationTool);
			Gui.DisplayCameraModelCalibrationTool(CameraModelCalibrationTool);
			Gui.DisplayDesignTool(DesignTool);
		}
	}
}
