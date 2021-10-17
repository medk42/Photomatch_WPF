﻿using System;
using System.Collections.Generic;
using System.Text;

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
		IWindow CreateImageWindow(ImageWindow imageWindow);
	}

	public interface IWindow
	{
		void SetImage(System.Drawing.Bitmap image);
		double ScreenDistance(Vector2 pointA, Vector2 pointB);
		ILine CreateLine(Vector2 start, Vector2 end, double endRadius, ApplicationColor color);
	}

	public interface Actions
	{
		void LoadImage_Pressed();
	}
}
