using System;
using System.Collections.Generic;
using System.Text;
using SixLabors.ImageSharp;

using MatrixVector;
using Logging;
using GuiEnums;
using GuiControls;

namespace GuiInterfaces
{
	public interface ILine
	{
		Vector2 Start { get; set; }
		Vector2 End { get; set; }
	}

	public interface MasterGUI : ILogger
	{
		string GetImageFilePath();
		string GetSaveProjectFilePath();
		string GetLoadProjectFilePath();
		IWindow CreateImageWindow(ImageWindow imageWindow);
	}

	public interface IWindow
	{
		void SetImage(Image image);
		double ScreenDistance(Vector2 pointA, Vector2 pointB);
		ILine CreateLine(Vector2 start, Vector2 end, double endRadius, ApplicationColor color);
		void DisposeAll();
	}

	public interface Actions
	{
		void LoadImage_Pressed();
		void SaveProject_Pressed();
		void SaveProjectAs_Pressed();
		void LoadProject_Pressed();
	}
}
