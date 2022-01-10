using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Gui
{
	public enum MouseButton { Left, Right, Middle }

	public enum ApplicationColor { XAxis, YAxis, ZAxis, Model }

	public enum ProjectState { None, NewProject, NamedProject }

	public enum DesignTool { CameraCalibration, CameraModelCalibration, ModelCreation }

	public enum KeyboardKey { LeftShift, Escape };

	public enum ModelCreationTool { Delete, Edge };

	public enum CameraModelCalibrationTool { CalibrateOrigin };
}
