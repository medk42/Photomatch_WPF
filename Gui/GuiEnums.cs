using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Gui
{
	public enum MouseButton { Left, Right, Middle }

	public enum ApplicationColor { XAxis, YAxis, ZAxis, Model, Selected, Face }

	public enum DesignTool { CameraCalibration, CameraModelCalibration, ModelCreation }

	public enum KeyboardKey { LeftShift, Escape };

	public enum ModelCreationTool { Delete, Edge, TriangleFace, ComplexFace };

	public enum CameraModelCalibrationTool { CalibrateOrigin, CalibrateScale };
}
