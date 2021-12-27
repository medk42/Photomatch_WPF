﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SixLabors.ImageSharp;

using GuiInterfaces;
using Logging;
using GuiPoints;
using Perspective;
using MatrixVector;
using GuiEnums;
using Lines;
using Serializables;

namespace GuiControls
{
	public class ImageWindow
	{
		private static readonly double PointGrabRadius = 8;
		private static readonly double PointDrawRadius = 4;

		private MasterGUI Gui;
		private ILogger Logger;
		private IWindow Window { get; }

		private PerspectiveData Perspective;
		private DraggablePoints DraggablePoints;

		private ILine LineA1, LineA2, LineB1, LineB2;
		private ILine LineX, LineY, LineZ;

		private bool Initialized = false;

		public ISafeSerializable<PerspectiveData> PerspectiveSafeSerializable
		{
			get => Perspective;
		}

		public ImageWindow(PerspectiveData perspective, MasterGUI gui, ILogger logger)
		{
			this.Gui = gui;
			this.Logger = logger;
			this.Window = Gui.CreateImageWindow(this, Path.GetFileName(perspective.ImagePath));

			this.Perspective = perspective;
			this.DraggablePoints = new DraggablePoints(Window, PointGrabRadius);

			Window.SetImage(perspective.Image);
			Window.DisplayCalibrationAxes(Perspective.CalibrationAxes);
			Window.DisplayInvertedAxes(Perspective.InvertedAxes);

			CreateCoordSystemLines();
			CreatePerspectiveLines();

			Initialized = true;
		}

		public void MouseMove(Vector2 mouseCoord)
		{
			DraggablePoints.MouseMove(mouseCoord);
		}

		public void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			DraggablePoints.MouseDown(mouseCoord, button);
		}

		public void MouseUp(Vector2 mouseCoord, MouseButton button)
		{
			DraggablePoints.MouseUp(mouseCoord, button);
		}

		private void CreatePerspectiveLines()
		{
			Tuple<ApplicationColor, ApplicationColor> colors = GetColorsFromCalibrationAxes(Perspective.CalibrationAxes);

			LineA1 = Window.CreateLine(Perspective.LineA1.Start, Perspective.LineA1.End, PointDrawRadius, colors.Item1);
			LineA2 = Window.CreateLine(Perspective.LineA2.Start, Perspective.LineA2.End, PointDrawRadius, colors.Item1);
			LineB1 = Window.CreateLine(Perspective.LineB1.Start, Perspective.LineB1.End, PointDrawRadius, colors.Item2);
			LineB2 = Window.CreateLine(Perspective.LineB2.Start, Perspective.LineB2.End, PointDrawRadius, colors.Item2);

			AddDraggablePointsForPerspectiveLine(LineA1,
				(value) => Perspective.LineA1 = Perspective.LineA1.WithStart(value),
				(value) => Perspective.LineA1 = Perspective.LineA1.WithEnd(value));
			AddDraggablePointsForPerspectiveLine(LineA2,
				(value) => Perspective.LineA2 = Perspective.LineA2.WithStart(value),
				(value) => Perspective.LineA2 = Perspective.LineA2.WithEnd(value));
			AddDraggablePointsForPerspectiveLine(LineB1,
				(value) => Perspective.LineB1 = Perspective.LineB1.WithStart(value),
				(value) => Perspective.LineB1 = Perspective.LineB1.WithEnd(value));
			AddDraggablePointsForPerspectiveLine(LineB2,
				(value) => Perspective.LineB2 = Perspective.LineB2.WithStart(value),
				(value) => Perspective.LineB2 = Perspective.LineB2.WithEnd(value));
		}

		private Tuple<ApplicationColor, ApplicationColor> GetColorsFromCalibrationAxes(CalibrationAxes axes)
		{
			ApplicationColor colorA, colorB;

			switch (axes)
			{
				case CalibrationAxes.XY:
					colorA = ApplicationColor.XAxis;
					colorB = ApplicationColor.YAxis;
					break;
				case CalibrationAxes.YX:
					colorA = ApplicationColor.YAxis;
					colorB = ApplicationColor.XAxis;
					break;
				case CalibrationAxes.XZ:
					colorA = ApplicationColor.XAxis;
					colorB = ApplicationColor.ZAxis;
					break;
				case CalibrationAxes.ZX:
					colorA = ApplicationColor.ZAxis;
					colorB = ApplicationColor.XAxis;
					break;
				case CalibrationAxes.YZ:
					colorA = ApplicationColor.YAxis;
					colorB = ApplicationColor.ZAxis;
					break;
				case CalibrationAxes.ZY:
					colorA = ApplicationColor.ZAxis;
					colorB = ApplicationColor.YAxis;
					break;
				default:
					throw new Exception("Unexpected switch case.");
			}

			return new Tuple<ApplicationColor, ApplicationColor>(colorA, colorB);
		}

		private void AddDraggablePointsForPerspectiveLine(ILine line, UpdateValue<Vector2> updateValueStart, UpdateValue<Vector2> updateValueEnd)
		{
			DraggablePoints.Points.Add(new ActionPoint(line.Start, (value) =>
			{
				line.Start = value;
				updateValueStart(value);
				UpdateCoordSystemLines();
			}));
			DraggablePoints.Points.Add(new ActionPoint(line.End, (value) =>
			{
				line.End = value;
				updateValueEnd(value);
				UpdateCoordSystemLines();
			}));
		}

		private void CreateCoordSystemLines()
		{
			var origin = new Vector2();
			LineX = Window.CreateLine(origin, origin, PointDrawRadius, ApplicationColor.XAxis);
			LineY = Window.CreateLine(origin, origin, PointDrawRadius, ApplicationColor.YAxis);
			LineZ = Window.CreateLine(origin, origin, PointDrawRadius, ApplicationColor.ZAxis);

			DraggablePoints.Points.Add(new ActionPoint(Perspective.Origin, (value) =>
			{
				Perspective.Origin = value;
				UpdateCoordSystemLines();
			}));

			UpdateCoordSystemLines();
		}

		private void UpdateCoordSystemLines()
		{
			Vector2 dirX = Perspective.GetXDirAt(Perspective.Origin);
			Vector2 dirY = Perspective.GetYDirAt(Perspective.Origin);
			Vector2 dirZ = Perspective.GetZDirAt(Perspective.Origin);

			LineX.Start = Perspective.Origin;
			LineY.Start = Perspective.Origin;
			LineZ.Start = Perspective.Origin;

			if (dirX.Valid && dirY.Valid && dirZ.Valid)
			{
				Vector2 imageSize = new Vector2(Perspective.Image.Width, Perspective.Image.Height);

				Vector2 endX = Intersections2D.GetRayInsideBoxIntersection(new Ray2D(Perspective.Origin, dirX), new Vector2(), imageSize);
				Vector2 endY = Intersections2D.GetRayInsideBoxIntersection(new Ray2D(Perspective.Origin, dirY), new Vector2(), imageSize);
				Vector2 endZ = Intersections2D.GetRayInsideBoxIntersection(new Ray2D(Perspective.Origin, dirZ), new Vector2(), imageSize);

				LineX.End = endX;
				LineY.End = endY;
				LineZ.End = endZ;
			}
			else
			{
				LineX.End = LineX.Start + new Vector2(Perspective.Image.Height * 0.1, 0);
				LineY.End = LineY.Start + new Vector2(0, Perspective.Image.Height * 0.1);
				LineZ.End = LineZ.Start;
			}
		}

		public void Dispose()
		{
			Window.DisposeAll();
		}

		public void CalibrationAxes_Changed(CalibrationAxes calibrationAxes)
		{
			if (Initialized)
			{
				Perspective.CalibrationAxes = calibrationAxes;
				Window.DisplayCalibrationAxes(Perspective.CalibrationAxes);
				UpdateCoordSystemLines();

				Tuple<ApplicationColor, ApplicationColor> colors = GetColorsFromCalibrationAxes(Perspective.CalibrationAxes);
				LineA1.SetColor(colors.Item1);
				LineA2.SetColor(colors.Item1);
				LineB1.SetColor(colors.Item2);
				LineB2.SetColor(colors.Item2);
			}	
		}

		public void InvertedAxes_Changed(InvertedAxes invertedAxes)
		{
			if (Initialized)
			{
				Perspective.InvertedAxes = invertedAxes;
				Window.DisplayInvertedAxes(Perspective.InvertedAxes);
				UpdateCoordSystemLines();
			}
		}
	}

	public class MasterControl : Actions
	{
		private static readonly ulong ProjectFileChecksum = 0x54_07_02_47_23_43_94_42;
		private static readonly string NewProjectName = "new project...";

		private MasterGUI Gui;
		private ILogger Logger;
		private List<ImageWindow> Windows;
		private ProjectState State;
		private string ProjectPath;

		public MasterControl(MasterGUI gui)
		{
			this.Gui = gui;
			this.Logger = gui;
			this.Windows = new List<ImageWindow>();
			this.State = ProjectState.None;
			this.ProjectPath = null;

			Gui.DisplayProjectName(NewProjectName);
		}

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
				Windows.Add(new ImageWindow(new PerspectiveData(image, filePath), Gui, Logger));

				if (State == ProjectState.None)
					State = ProjectState.NewProject;
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

					int windowCount = reader.ReadInt32();
					for (int i = 0; i < windowCount; i++)
					{
						PerspectiveData perspective = ISafeSerializable<PerspectiveData>.CreateDeserialize(reader);
						Windows.Add(new ImageWindow(perspective, Gui, Logger));
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
				if (ex is UnauthorizedAccessException)
					Logger.Log("Load Project", "Unauthorized access to file or path was directory.", LogType.Warning);
				else if (ex is IOException)
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

		public void Reset()
		{
			foreach (ImageWindow window in Windows)
				window.Dispose();

			Windows.Clear();
			State = ProjectState.None;
			ProjectPath = null;

			Gui.DisplayProjectName(NewProjectName);
		}
	}
}
