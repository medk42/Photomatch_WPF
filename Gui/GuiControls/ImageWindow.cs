using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Photomatch_ProofOfConcept_WPF.Gui.GuiControls.Helper;
using Photomatch_ProofOfConcept_WPF.Logic;
using Photomatch_ProofOfConcept_WPF.Utilities;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls
{
	public class ImageWindow
	{
		public PerspectiveData Perspective;

		private static readonly double PointGrabRadius = 8;
		private static readonly double PointDrawRadius = 4;

		private MasterGUI Gui;
		private ILogger Logger;
		private IWindow Window { get; }
		private ModelVisualization ModelVisualization;

		private ModelCreationHandler ModelCreationHandler;
		private CameraCalibrationHandler CameraCalibrationHandler;
		private CameraModelCalibrationHandler CameraModelCalibrationHandler;

		private bool Initialized = false;

		public ImageWindow(PerspectiveData perspective, MasterGUI gui, ILogger logger, Model model, DesignTool currentDesignTool, ModelCreationTool currentModelCreationTool, CameraModelCalibrationTool currentCameraModelCalibrationTool)
		{
			this.Gui = gui;
			this.Logger = logger;
			this.Window = Gui.CreateImageWindow(this, Path.GetFileName(perspective.ImagePath));
			this.Perspective = perspective;
			this.Window.SetImage(perspective.Image);

			this.ModelVisualization = new ModelVisualization(Perspective, Window, model, PointGrabRadius, PointDrawRadius);
			this.ModelCreationHandler = new ModelCreationHandler(model, Window, Perspective, ModelVisualization);
			this.CameraCalibrationHandler = new CameraCalibrationHandler(Perspective, Window, PointGrabRadius, PointDrawRadius);
			this.CameraModelCalibrationHandler = new CameraModelCalibrationHandler(ModelVisualization, model, Perspective, Window, PointGrabRadius, PointDrawRadius);

			this.CameraCalibrationHandler.CoordSystemUpdateEvent += ModelVisualization.UpdateDisplayedGeometry;

			this.ModelCreationTool_Changed(currentModelCreationTool);
			this.CameraModelCalibrationTool_Changed(currentCameraModelCalibrationTool);
			this.DesignTool_Changed(currentDesignTool);

			this.Initialized = true;
		}

		public void MouseMove(Vector2 mouseCoord)
		{
			CameraCalibrationHandler.MouseMove(mouseCoord);
			ModelCreationHandler.MouseMove(mouseCoord);
			CameraModelCalibrationHandler.MouseMove(mouseCoord);
		}

		public void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			CameraCalibrationHandler.MouseDown(mouseCoord, button);
			ModelCreationHandler.MouseDown(mouseCoord, button);
			CameraModelCalibrationHandler.MouseDown(mouseCoord, button);
		}

		public void MouseUp(Vector2 mouseCoord, MouseButton button)
		{
			CameraCalibrationHandler.MouseUp(mouseCoord, button);
			ModelCreationHandler.MouseUp(mouseCoord, button);
			CameraModelCalibrationHandler.MouseUp(mouseCoord, button);
		}

		public void KeyDown(KeyboardKey key)
		{
			ModelCreationHandler.KeyDown(key);
		}

		public void KeyUp(KeyboardKey key)
		{
			ModelCreationHandler.KeyUp(key);
		}

		public void Dispose()
		{
			Perspective = null;
			CameraCalibrationHandler.CoordSystemUpdateEvent -= ModelVisualization.UpdateDisplayedGeometry;

			ModelCreationHandler.Dispose();
			CameraCalibrationHandler.Dispose();
			Window.DisposeAll();
		}

		public void CalibrationAxes_Changed(CalibrationAxes calibrationAxes)
		{
			if (Initialized)
			{
				CameraCalibrationHandler.CalibrationAxes_Changed(calibrationAxes);
			}
		}

		public void InvertedAxes_Changed(InvertedAxes invertedAxes)
		{
			if (Initialized)
			{
				CameraCalibrationHandler.InvertedAxes_Changed(invertedAxes);
			}
		}

		public void DesignTool_Changed(DesignTool newDesignTool)
		{
			CameraCalibrationHandler.Active = false;
			ModelCreationHandler.Active = false;
			CameraModelCalibrationHandler.Active = false;

			switch (newDesignTool)
			{
				case DesignTool.CameraCalibration:
					CameraCalibrationHandler.Active = true;
					break;
				case DesignTool.CameraModelCalibration:
					CameraModelCalibrationHandler.Active = true;
					break;
				case DesignTool.ModelCreation:
					ModelCreationHandler.Active = true;
					break;
				default:
					throw new Exception("Unknown switch case.");
			}
		}

		public void ModelCreationTool_Changed(ModelCreationTool newModelCreationTool)
		{
			ModelCreationHandler.CreationTool_Changed(newModelCreationTool);
		}

		public void CameraModelCalibrationTool_Changed(CameraModelCalibrationTool newCameraModelCalibrationTool)
		{
			CameraModelCalibrationHandler.CalibrationTool_Changed(newCameraModelCalibrationTool);
		}
	}
}
