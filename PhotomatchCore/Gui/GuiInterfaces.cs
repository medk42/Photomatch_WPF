using System;
using System.Collections.Generic;
using System.Text;
using SixLabors.ImageSharp;

using PhotomatchCore.Logic;
using PhotomatchCore.Utilities;
using PhotomatchCore.Gui.GuiControls;

namespace PhotomatchCore.Gui
{
	/// <summary>
	/// Interface representing a GUI line.
	/// </summary>
	public interface ILine
	{
		Vector2 Start { get; set; }
		Vector2 End { get; set; }
 		ApplicationColor Color { get; set; }
		bool Visible { get; set; }

		/// <summary>
		/// Dispose of object resources.
		/// </summary>
		void Dispose();
	}

	/// <summary>
	/// Interface representing a filled ellipse.
	/// </summary>
	public interface IEllipse
	{
		Vector2 Position { get; set; }
		ApplicationColor Color { get; set; }
		bool Visible { get; set; }

		/// <summary>
		/// Dispose of object resources.
		/// </summary>
		void Dispose();
	}

	/// <summary>
	/// Interface representing a filled polygon.
	/// </summary>
	public interface IPolygon
	{
		/// <summary>
		/// Get/set position of i-th vertex, counting from 0.
		/// </summary>
		/// <returns>Position of the vertex.</returns>
		Vector2 this[int i] { get; set; }
		ApplicationColor Color { get; set; }
		bool Visible { get; set; }

		/// <summary>
		/// Vertex count.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Add vertex to the end.
		/// </summary>
		/// <param name="vertex">Vertex position.</param>
		void Add(Vector2 vertex);

		/// <summary>
		/// Remove vertex at specified index.
		/// </summary>
		void Remove(int index);

		/// <summary>
		/// Dispose of object resources.
		/// </summary>
		void Dispose();
	}

	/// <summary>
	/// Interface for calling the main class of the used application GUI.
	/// </summary>
	public interface MasterGUI : ILogger
	{
		/// <summary>
		/// Get a file path to an image from the user.
		/// </summary>
		string GetImageFilePath();

		/// <summary>
		/// Get a file path to a location where to save a project from the user.
		/// </summary>
		string GetSaveProjectFilePath();

		/// <summary>
		/// Get a file path to saved project to load from the user.
		/// </summary>
		string GetLoadProjectFilePath();

		/// <summary>
		/// Get a file path to export the model to from the user.
		/// </summary>
		string GetModelExportFilePath();

		/// <summary>
		/// Create a sub-window for an ImageWindow with a specified title.
		/// </summary>
		/// <returns>Interface that represents the created window.</returns>
		IWindow CreateImageWindow(ImageWindow imageWindow, string title);

		/// <summary>
		/// Create a sub-window displaying the created 3D model.
		/// </summary>
		/// <returns>Interface that represents the created window.</returns>
		IModelView CreateModelWindow(Model model);

		/// <summary>
		/// Display project name from the parameter.
		/// </summary>
		void DisplayProjectName(string projectName);

		/// <summary>
		/// Display that current design tool is designTool.
		/// </summary>
		void DisplayDesignTool(DesignTool designTool);

		/// <summary>
		/// Display that current model creation tool is modelCreationTool.
		/// </summary>
		void DisplayModelCreationTool(ModelCreationTool modelCreationTool);

		/// <summary>
		/// Display that current camera model calibration tool is cameraModelCalibrationTool.
		/// </summary>
		void DisplayCameraModelCalibrationTool(CameraModelCalibrationTool cameraModelCalibrationTool);

		/// <summary>
		/// Display a warning prompt to a user with buttons "Yes" and "No".
		/// </summary>
		/// <param name="title">Prompt title.</param>
		/// <param name="message">Prompt message.</param>
		/// <returns>true if user selected "Yes".</returns>
		bool DisplayWarningProceedMessage(string title, string message);
	}

	/// <summary>
	/// Interface for calling the class displaying the created 3D model.
	/// </summary>
	public interface IModelView
	{
		/// <summary>
		/// Update displayed model to model passed by parameter.
		/// </summary>
		void UpdateModel(Model model);
	}

	/// <summary>
	/// Interface for calling the class representing a sub-window for an ImageWindow of the used application GUI.
	/// </summary>
	public interface IWindow
	{
		/// <summary>
		/// Width of the displayed image.
		/// </summary>
		double Width { get; }

		/// <summary>
		/// Height of the displayed image.
		/// </summary>
		double Height { get; }

		/// <summary>
		/// Display image passed by a parameter.
		/// </summary>
		void SetImage(Image image);

		/// <summary>
		/// Get pixel distance on screen between two points with image coordinates (distance can differ based on window and image scaling).
		/// </summary>
		double ScreenDistance(Vector2 pointA, Vector2 pointB);

		/// <summary>
		/// Create a line in the GUI with specified start, end, radius of the endpoints and color.
		/// </summary>
		ILine CreateLine(Vector2 start, Vector2 end, double endRadius, ApplicationColor color);

		/// <summary>
		/// Create an ellipse in the GUI with specified position, radius and color.
		/// </summary>
		IEllipse CreateEllipse(Vector2 position, double radius, ApplicationColor color);

		/// <summary>
		/// Create a polygon in the GUI with a specified color. Vertices can be added later via returned IPolygon interface.
		/// </summary>
		IPolygon CreateFilledPolygon(ApplicationColor color);

		/// <summary>
		/// Dispose of all resources held by the window.
		/// </summary>
		void DisposeAll();

		/// <summary>
		/// Display that current calibration axes are calibrationAxes.
		/// </summary>
		void DisplayCalibrationAxes(CalibrationAxes calibrationAxes);

		/// <summary>
		/// Display that current invertable axes are calibrationAxes and that current inverted axes are invertedAxes.
		/// </summary>
		void DisplayInvertedAxes(CalibrationAxes calibrationAxes, InvertedAxes invertedAxes);

		/// <summary>
		/// Display a warning prompt to a user with buttons "Yes" and "No".
		/// </summary>
		/// <param name="title">Prompt title.</param>
		/// <param name="message">Prompt message.</param>
		/// <returns>true if user selected "Yes".</returns>
		bool DisplayWarningProceedMessage(string title, string message);

		/// <summary>
		/// Close the sub-window.
		/// </summary>
		void Close();
	}

	/// <summary>
	/// Interface representing actions that can be done in the application GUI.
	/// </summary>
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
