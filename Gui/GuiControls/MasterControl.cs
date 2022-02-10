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

		private MasterGUI Gui;
		private ILogger Logger;
		private List<ImageWindow> Windows;
		private ProjectState State;
		private string ProjectPath;
		private Model Model;
		private DesignTool DesignTool;
		private ModelCreationTool ModelCreationTool;
		private CameraModelCalibrationTool CameraModelCalibrationTool;
		private IModelView ModelView;

		private List<byte[]> History = new List<byte[]>();
		private List<byte[]> Future = new List<byte[]>();
		private bool HoldingControl = false;

		private bool Dirty_;
		private bool Dirty
		{
			get => Dirty_;
			set
			{
				if (Dirty_ != value)
				{
					Dirty_ = value;
					DisplayProjectName();
				}
			}
		}

		private bool HistoryDirtyEnabled { get; set; } = true;

		private bool HistoryDirty_;
		private bool HistoryDirty
		{
			get => HistoryDirty_;
			set
			{
				if (!HistoryDirtyEnabled)
					return;

				if (HistoryDirty_ != value)
					HistoryDirty_ = value;

				if (value)
					Future.Clear();
			}
		}

		private string ProjectName_;
		private string ProjectName
		{
			get => ProjectName_;
			set
			{
				if (ProjectName_ != value)
				{
					ProjectName_ = value;
					DisplayProjectName();
				}
			}
		}

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

			this.Model.ModelChangedEvent += () => Dirty = true;
			this.Model.ModelChangedEvent += () => HistoryDirty = true;

			ProjectName = NewProjectName;

			ModelView = Gui.CreateModelWindow(Model);

			AddHistory();
		}

		private void CheckDirty()
		{
			if (Dirty)
			{
				string message = "Do you want to save the current project before continuing?";
				if (Gui.DisplayWarningProceedMessage("Save...", message))
					SaveProject_Pressed();
			}
		}

		public void NewProject_Pressed()
		{
			CheckDirty();
			Reset();
			AddHistory();
		}

		public void LoadImage_Pressed()
		{
			string filePath = Gui.GetImageFilePath();
			if (filePath == null)
			{
				Logger.Log("Load Image", "No file was selected.", LogType.Info);
				return;
			}

			byte[] imageData = null;
			Image<Rgb24> image = null;
			try
			{
				using (FileStream fileStream = File.OpenRead(filePath))
				using (MemoryStream memoryStream = new MemoryStream())
				{
					fileStream.CopyTo(memoryStream);
					imageData = memoryStream.ToArray();
				}

				image = Image.Load<Rgb24>(imageData);
			}
			catch (Exception ex)
			{
				if (ex is FileNotFoundException)
					Logger.Log("Load Image", "File not found.", LogType.SevereWarning);
				else if (ex is ArgumentException || ex is ArgumentNullException || ex is PathTooLongException || ex is DirectoryNotFoundException || ex is UnauthorizedAccessException)
					Logger.Log("Load Image", "Invalid path.", LogType.SevereWarning);
				else if (ex is UnauthorizedAccessException)
					Logger.Log("Load Image", "Unauthorized access to file.", LogType.SevereWarning);
				else if (ex is IOException)
					Logger.Log("Load Image", "Image could not be loaded.", LogType.SevereWarning);
				else if (ex is UnknownImageFormatException)
					Logger.Log("Load Image", "Incorrect or unsupported image format.", LogType.SevereWarning);
				else
					throw ex;
			}

			if (image != null)
			{
				Logger.Log("Load Image", "File loaded successfully.", LogType.Info);
				Windows.Add(new ImageWindow(new PerspectiveData(image, imageData, filePath), Gui, this, Logger, Model, DesignTool, ModelCreationTool, CameraModelCalibrationTool));
				Dirty = true;
				Windows[Windows.Count - 1].Perspective.PerspectiveChangedEvent += () => Dirty = true;
				Windows[Windows.Count - 1].Perspective.PerspectiveChangedEvent += () => HistoryDirty = true;

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
					Logger.Log("Save Project", "Nothing to save.", LogType.SevereWarning);
					return;
				case ProjectState.NewProject:
					SaveProjectAs_Pressed();
					break;
				case ProjectState.NamedProject:
					if (Dirty)
						if (!SaveProject(ProjectPath))
							return;
					break;
				default:
					throw new NotImplementedException("Unknown ProjectState");
			}
		}

		public void SaveProjectAs_Pressed()
		{
			switch (State)
			{
				case ProjectState.None:
					Logger.Log("Save Project", "Nothing to save.", LogType.SevereWarning);
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

					ProjectName = Path.GetFileName(filePath);
					break;
				default:
					throw new NotImplementedException("Unknown ProjectState");
			}
		}

		private bool SaveProject(string fileName)
		{
			try
			{
				using (var fileStream = File.Create(fileName))
				using (var writer = new BinaryWriter(fileStream))
				{
					writer.Write(ProjectFileChecksum);
					Logger.Log("Save Project", "Saving model.", LogType.Progress);
					Model.Serialize(writer);
					writer.Write((int)DesignTool);
					writer.Write(Windows.Count);
					foreach (ImageWindow window in Windows)
					{
						Logger.Log("Save Project", $"Saving window {Windows.IndexOf(window) + 1}/{Windows.Count}.", LogType.Progress);
						window.Perspective.Serialize(writer);
					}
				}

				Logger.Log("Save Project", "Successfully saved project.", LogType.Info);
				Dirty = false;
			}
			catch (Exception ex)
			{
				if (ex is UnauthorizedAccessException)
					Logger.Log("Save Project", "Unauthorized access to file.", LogType.SevereWarning);
				else if (ex is IOException)
					Logger.Log("Save Project", "Save operation was not successful.", LogType.SevereWarning);
				else if (ex is ArgumentException || ex is DirectoryNotFoundException || ex is NotSupportedException)
					Logger.Log("Save Project", "Path is invalid.", LogType.SevereWarning);
				else if (ex is PathTooLongException)
					Logger.Log("Save Project", "Path is too long.", LogType.SevereWarning);
				else throw ex;

				return false;
			}

			return true;
		}

		public void LoadProject_Pressed()
		{
			CheckDirty();

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
						Logger.Log("Load Project", "Invalid file.", LogType.SevereWarning);
						return;
					}

					Model = ISafeSerializable<Model>.CreateDeserialize(reader);
					Model.ModelChangedEvent += () => Dirty = true;
					this.Model.ModelChangedEvent += () => HistoryDirty = true;

					ModelView.UpdateModel(Model);

					DesignTool = (DesignTool)reader.ReadInt32();
					Gui.DisplayDesignTool(DesignTool);

					int windowCount = reader.ReadInt32();
					for (int i = 0; i < windowCount; i++)
					{
						PerspectiveData perspective = ISafeSerializable<PerspectiveData>.CreateDeserialize(reader);
						Windows.Add(new ImageWindow(perspective, Gui, this, Logger, Model, DesignTool, ModelCreationTool, CameraModelCalibrationTool));
						Windows[i].Perspective.PerspectiveChangedEvent += () => Dirty = true;
						Windows[i].Perspective.PerspectiveChangedEvent += () => HistoryDirty = true;
					}
				}

				string projectName = Path.GetFileName(filePath);
				ProjectName = projectName;

				State = ProjectState.NamedProject;
				ProjectPath = filePath;
				Logger.Log("Load Project", $"Successfully loaded project {projectName}.", LogType.Info);

				AddHistory();
			}
			catch (Exception ex)
			{
				this.Reset();

				if (ex is UnauthorizedAccessException)
					Logger.Log("Load Project", "Unauthorized access to file or path was directory.", LogType.SevereWarning);
				else if (ex is IOException || ex is ArgumentOutOfRangeException)
					Logger.Log("Load Project", "Invalid file.", LogType.SevereWarning);
				else if (ex is ArgumentException || ex is DirectoryNotFoundException || ex is NotSupportedException)
					Logger.Log("Load Project", "Path is invalid.", LogType.SevereWarning);
				else if (ex is PathTooLongException)
					Logger.Log("Load Project", "Path is too long.", LogType.SevereWarning);
				else if (ex is FileNotFoundException)
					Logger.Log("Load Project", "File not found.", LogType.SevereWarning);
				else
					Logger.Log("Load Project", "Invalid file.", LogType.SevereWarning);
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

			List<PerspectiveData> perspectives = new List<PerspectiveData>();
			foreach (ImageWindow window in Windows)
				perspectives.Add(window.Perspective);

			Exporter.Export(Model, filePath, Logger, perspectives);
		}

		public void Exit_Pressed()
		{
			CheckDirty();
		}

		private void DeserializeUndoRedo(byte[] data)
		{
			HistoryDirtyEnabled = false;

			using (MemoryStream stream = new MemoryStream(data))
			using (BinaryReader reader = new BinaryReader(stream))
			{
				int count = reader.ReadInt32();
				for (int i = 0; i < count; i++)
				{
					Windows[i].Perspective.DeserializeWithoutImage(reader);
					Windows[i].UpdateDisplayedGeometry();
				}

				Model.Dispose();
				Model = ISafeSerializable<Model>.CreateDeserialize(reader);
				Model.ModelChangedEvent += () => Dirty = true;
				this.Model.ModelChangedEvent += () => HistoryDirty = true;

				ModelView.UpdateModel(Model);

				foreach (ImageWindow window in Windows)
					window.UpdateModel(Model);
			}

			HistoryDirtyEnabled = true;

		}

		public void Undo_Pressed()
		{
			if (History.Count >= 2)
			{
				Future.Add(History[History.Count - 1]);
				History.RemoveAt(History.Count - 1);
				byte[] data = History[History.Count - 1];

				DeserializeUndoRedo(data);

				HistoryDirty = false;
				Dirty = true;
			}
		}

		public void Redo_Pressed()
		{
			if (Future.Count >= 1)
			{
				History.Add(Future[Future.Count - 1]);
				byte[] data = Future[Future.Count - 1];
				Future.RemoveAt(Future.Count - 1);

				DeserializeUndoRedo(data);
				Dirty = true;
			}
		}

		public void MouseUp()
		{
			if (HistoryDirty)
			{
				AddHistory();
			}
		}

		public void KeyDown(KeyboardKey key)
		{
			switch (key)
			{
				case KeyboardKey.LeftCtrl:
					HoldingControl = true;
					break;
				case KeyboardKey.Y:
					if (HoldingControl)
						Redo_Pressed();
					break;
				case KeyboardKey.Z:
					if (HoldingControl)
						Undo_Pressed();
					break;
				case KeyboardKey.S:
					if (HoldingControl)
						SaveProject_Pressed();
					break;
			}
		}

		public void KeyUp(KeyboardKey key)
		{
			switch (key)
			{
				case KeyboardKey.LeftCtrl:
					HoldingControl = false;
					break;
			}
		}

		private void AddHistory()
		{
			byte[] data;

			using (MemoryStream stream = new MemoryStream())
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				writer.Write(Windows.Count);
				foreach (ImageWindow window in Windows)
					window.Perspective.SerializeWithoutImage(writer);

				Model.Serialize(writer);

				data = stream.ToArray();
			}

			if (History.Count > 0)
			{
				byte[] other = History[History.Count - 1];
				if (other.Length == data.Length)
				{
					bool same = true;
					for (int i = 0; i < data.Length; i++)
					{
						if (other[i] != data[i])
						{
							same = false;
							break;
						}
					}
					if (!same)
						History.Add(data);
				}
				else
					History.Add(data);
			}
			else
				History.Add(data);

			HistoryDirty = false;
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
			Model.ModelChangedEvent += () => Dirty = true;
			Model.ModelChangedEvent += () => HistoryDirty = true;
			ModelView.UpdateModel(Model);

			ProjectName = NewProjectName;

			DesignTool = DesignTool.CameraCalibration;
			ModelCreationTool = ModelCreationTool.Edge;
			CameraModelCalibrationTool = CameraModelCalibrationTool.CalibrateOrigin;
			Gui.DisplayModelCreationTool(ModelCreationTool);
			Gui.DisplayCameraModelCalibrationTool(CameraModelCalibrationTool);
			Gui.DisplayDesignTool(DesignTool);

			Dirty = false;
			HistoryDirty = false;
			History.Clear();
			Future.Clear();
		}

		public void WindowRemoved(ImageWindow imageWindow)
		{
			Windows.Remove(imageWindow);
			Dirty = true;
			HistoryDirty = false;
			History.Clear();
			Future.Clear();
			AddHistory();
		}

		private void DisplayProjectName()
		{
			Gui.DisplayProjectName(Dirty ? $"{ProjectName}*" : ProjectName);
		}
	}
}
