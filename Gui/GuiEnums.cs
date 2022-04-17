using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Gui
{
	public enum MouseButton { Left, Right, Middle, DoubleLeft, DoubleRight, DoubleMiddle }

	public enum ApplicationColor { XAxis, YAxis, ZAxis, Model, Selected, Face, Highlight, Vertex, Midpoint, Edgepoint, Invalid, NormalLine, NormalInside, NormalOutside, XAxisDotted, YAxisDotted, ZAxisDotted }

	public enum DesignTool { CameraCalibration, CameraModelCalibration, ModelCreation }

	public enum KeyboardKey { LeftShift, Escape, LeftCtrl, Y, Z, S };

	public enum ModelCreationTool { Delete, Edge, TriangleFace, ComplexFace, FaceNormals };

	public enum CameraModelCalibrationTool { CalibrateOrigin, CalibrateScale };
}
