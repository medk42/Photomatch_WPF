using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls
{
	class CameraModelCalibrationHandler
	{
		private bool Active_;
		public bool Active
		{
			get => Active_;
			set
			{
				if (value != Active_)
				{
					Active_ = value;
					SetActive(Active_);
				}
			}
		}

		private readonly ModelCreationHandler ModelCreationHandler;

		public CameraModelCalibrationHandler(ModelCreationHandler modelCreationHandler)
		{
			this.ModelCreationHandler = modelCreationHandler;
		}

		private void SetActive(bool active)
		{
			ModelCreationHandler.ShowModel(active);
		}

		public void CalibrationTool_Click(CameraModelCalibrationTool cameraModelCalibrationTool)
		{

		}
	}
}
