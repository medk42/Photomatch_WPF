using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch.Gui
{
	/// <summary>
	/// Types of mouse click.
	/// </summary>
	public enum MouseButton { Left, Right, Middle, DoubleLeft, DoubleRight, DoubleMiddle }

	/// <summary>
	/// Types of GUI elements with the same color.
	/// </summary>
	public enum ApplicationColor { XAxis, YAxis, ZAxis, Model, Selected, Face, Highlight, Vertex, Midpoint, Edgepoint, Invalid, NormalLine, NormalInside, NormalOutside, XAxisDotted, YAxisDotted, ZAxisDotted }

	/// <summary>
	/// Types of tools for the application.
	/// </summary>
	public enum DesignTool { CameraCalibration, CameraModelCalibration, ModelCreation }

	/// <summary>
	/// Keyboard keys.
	/// </summary>
	public enum KeyboardKey { LeftShift, Escape, LeftCtrl, Y, Z, S };

	/// <summary>
	/// Tools for model creation.
	/// </summary>
	public enum ModelCreationTool { Delete, Edge, TriangleFace, ComplexFace, FaceNormals };

	/// <summary>
	/// Tools for camera-model calibration.
	/// </summary>
	public enum CameraModelCalibrationTool { CalibrateOrigin, CalibrateScale };
}
