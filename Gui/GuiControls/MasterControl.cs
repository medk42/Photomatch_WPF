using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Photomatch_ProofOfConcept_WPF.Logic;
using Photomatch_ProofOfConcept_WPF.Utilities;
using System.Globalization;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls
{
	public class MasterControl : Actions
	{
		private enum ProjectState { None, NewProject, NamedProject }

		private static readonly ulong ProjectFileChecksum = 0x54_07_02_47_23_43_94_42;
		private static readonly string NewProjectName = "new project...";

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

			Image image = null;
			try
			{
				image = Image.Load(filePath);
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
				{
					var writer = new BinaryWriter(fileStream);

					writer.Write(ProjectFileChecksum);
					Model.Serialize(writer);
					writer.Write((int)DesignTool);
					writer.Write(Windows.Count);
					foreach (ImageWindow window in Windows)
					{
						window.PerspectiveSafeSerializable.Serialize(writer);
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

		public void ExportModel_Pressed()
		{
			string fileName = Gui.GetModelExportFilePath();

			try
			{
				using (var fileStream = File.Create(fileName))
				using (var writer = new StreamWriter(fileStream, Encoding.ASCII))
				{
					foreach (Vertex v in Model.Vertices)
						writer.WriteLine($"v {v.Position.X.ToString(CultureInfo.InvariantCulture)} {v.Position.Y.ToString(CultureInfo.InvariantCulture)} {v.Position.Z.ToString(CultureInfo.InvariantCulture)}");

					writer.WriteLine();

					foreach (Face f in Model.Faces)
					{
						writer.Write('f');

						if (f.Reversed)
						{
							for (int i = f.Count - 1; i >= 0; i--)
							{
								writer.Write(" ");
								writer.Write(Model.Vertices.IndexOf(f[i]) + 1);
							}
						}
						else
						{
							for (int i = 0; i < f.Count; i++)
							{
								writer.Write(" ");
								writer.Write(Model.Vertices.IndexOf(f[i]) + 1);
							}
						}

						writer.WriteLine();
					}
				}
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
