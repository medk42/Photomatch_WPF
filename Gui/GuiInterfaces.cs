using System;
using System.Collections.Generic;
using System.Text;
using SixLabors.ImageSharp;

using Photomatch_ProofOfConcept_WPF.Logic;
using Photomatch_ProofOfConcept_WPF.Utilities;
using Photomatch_ProofOfConcept_WPF.Gui.GuiControls;

namespace Photomatch_ProofOfConcept_WPF.Gui
{
	public interface ILine
	{
		Vector2 Start { get; set; }
		Vector2 End { get; set; }
 		ApplicationColor Color { get; set; }
		bool Visible { get; set; }
		void Dispose();
	}

	public interface IEllipse
	{
		Vector2 Position { get; set; }
		ApplicationColor Color { get; set; }
		bool Visible { get; set; }
		void Dispose();
	}

	public interface IPolygon
	{
		Vector2 this[int i] { get; set; }
		ApplicationColor Color { get; set; }
		bool Visible { get; set; }
		int Count { get; }
		void Add(Vector2 vertex);
		void Remove(int index);
		void Dispose();
	}

	public interface MasterGUI : ILogger
	{
		string GetImageFilePath();
		string GetSaveProjectFilePath();
		string GetLoadProjectFilePath();
		string GetModelExportFilePath();
		IWindow CreateImageWindow(ImageWindow imageWindow, string title);
		void DisplayProjectName(string projectName);
		void DisplayDesignTool(DesignTool designTool);
		void DisplayModelCreationTool(ModelCreationTool modelCreationTool);
		void DisplayCameraModelCalibrationTool(CameraModelCalibrationTool cameraModelCalibrationTool);
		bool DisplayWarningProceedMessage(string title, string message);
	}

	public interface IWindow
	{
		void SetImage(Image image);
		double ScreenDistance(Vector2 pointA, Vector2 pointB);
		ILine CreateLine(Vector2 start, Vector2 end, double endRadius, ApplicationColor color);
		IEllipse CreateEllipse(Vector2 position, double radius, ApplicationColor color);
		IPolygon CreateFilledPolygon(ApplicationColor color);
		void DisposeAll();
		void DisplayCalibrationAxes(CalibrationAxes calibrationAxes);
		void DisplayInvertedAxes(CalibrationAxes calibrationAxes, InvertedAxes invertedAxes);
		bool DisplayWarningProceedMessage(string title, string message);
		void Close();
	}

	public interface Actions
	{
		void LoadImage_Pressed();
		void NewProject_Pressed();
		void SaveProject_Pressed();
		void SaveProjectAs_Pressed();
		void LoadProject_Pressed();
		void ExportModel_Pressed();
		void Undo_Pressed();
		void Redo_Pressed();
		void DesignTool_Changed(DesignTool newDesignTool);
		void ModelCreationTool_Changed(ModelCreationTool newModelCreationTool);
		void CameraModelCalibrationTool_Changed(CameraModelCalibrationTool newCameraModelCalibrationTool);
		void Exit_Pressed();
	}
}
