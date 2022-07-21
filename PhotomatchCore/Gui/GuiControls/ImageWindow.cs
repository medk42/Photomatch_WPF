using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PhotomatchCore.Gui.GuiControls.Helper;
using PhotomatchCore.Utilities;
using PhotomatchCore.Logic.Model;
using PhotomatchCore.Logic.Perspective;
using PhotomatchCore.Gui.GuiControls.ToolHandlers;

namespace PhotomatchCore.Gui.GuiControls
{
	/// <summary>
	/// Class representing the image window on the ViewModel layer.
	/// </summary>
	public class ImageWindow : IImageWindowActions
	{
		public PerspectiveData Perspective;

		private static readonly double PointGrabRadius = 16;
		private static readonly double PointDrawRadius = 4;

		private IMasterView Gui;
		private MasterControl Control;
		private ILogger Logger;
		private IImageView Window { get; }
		private ModelVisualization ModelVisualization;

		private ModelCreationHandler ModelCreationHandler;
		private CameraCalibrationHandler CameraCalibrationHandler;
		private CameraModelCalibrationHandler CameraModelCalibrationHandler;

		private DesignTool CurrentDesignTool;

		private bool Initialized = false;

		/// <summary>
		/// Create the image window on the ViewModel layer.
		/// </summary>
		/// <param name="perspective">Camera calibration data for this window.</param>
		/// <param name="gui">Reference to the main window on the View layer.</param>
		/// <param name="control">Reference to the main window on the ViewModel layer.</param>
		/// <param name="logger">Reference to a logger for sending messages to user.</param>
		/// <param name="model">Reference to the shared model.</param>
		public ImageWindow(PerspectiveData perspective, IMasterView gui, MasterControl control, ILogger logger, Model model, DesignTool currentDesignTool, ModelCreationTool currentModelCreationTool, CameraModelCalibrationTool currentCameraModelCalibrationTool)
		{
			this.Gui = gui;
			this.Control = control;
			this.Logger = logger;
			this.Window = Gui.CreateImageWindow(this, Path.GetFileName(perspective.ImagePath));
			this.Perspective = perspective;
			this.Window.SetImage(perspective.Image);

			this.ModelVisualization = new ModelVisualization(Perspective, Window, model, PointGrabRadius, PointDrawRadius);
			this.ModelCreationHandler = new ModelCreationHandler(model, Window, Perspective, ModelVisualization, Logger, PointDrawRadius, PointGrabRadius);
			this.CameraCalibrationHandler = new CameraCalibrationHandler(Perspective, Window, PointGrabRadius, PointDrawRadius);
			this.CameraModelCalibrationHandler = new CameraModelCalibrationHandler(ModelVisualization, model, Perspective, Window, PointGrabRadius, PointDrawRadius);

			this.CameraCalibrationHandler.CoordSystemUpdateEvent += ModelVisualization.UpdateDisplayedGeometry;

			this.ModelCreationTool_Changed(currentModelCreationTool);
			this.CameraModelCalibrationTool_Changed(currentCameraModelCalibrationTool);
			this.DesignTool_Changed(currentDesignTool);

			this.Initialized = true;
		}

		/// <summary>
		/// Pass notification to handlers.
		/// </summary>
		public void MouseMove(Vector2 mouseCoord)
		{
			CameraCalibrationHandler.MouseMove(mouseCoord);
			ModelCreationHandler.MouseMove(mouseCoord);
			CameraModelCalibrationHandler.MouseMove(mouseCoord);
		}

		/// <summary>
		/// Pass notification to handlers.
		/// </summary>
		public void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			CameraCalibrationHandler.MouseDown(mouseCoord, button);
			ModelCreationHandler.MouseDown(mouseCoord, button);
			CameraModelCalibrationHandler.MouseDown(mouseCoord, button);
		}

		/// <summary>
		/// Pass notification to handlers. Notify MasterControl that a whole operation has ended.
		/// </summary>
		public void MouseUp(Vector2 mouseCoord, MouseButton button)
		{
			CameraCalibrationHandler.MouseUp(mouseCoord, button);
			ModelCreationHandler.MouseUp(mouseCoord, button);
			CameraModelCalibrationHandler.MouseUp(mouseCoord, button);

			Control.ImageEndOperation();
		}

		/// <summary>
		/// Pass notification to handlers.
		/// </summary>
		public void KeyDown(KeyboardKey key)
		{
			ModelCreationHandler.KeyDown(key);
		}

		/// <summary>
		/// Pass notification to handlers.
		/// </summary>
		public void KeyUp(KeyboardKey key)
		{
			ModelCreationHandler.KeyUp(key);
		}

		/// <summary>
		/// Dispose of all resources held by the window.
		/// </summary>
		public void Dispose()
		{
			Perspective = null;
			CameraCalibrationHandler.CoordSystemUpdateEvent -= ModelVisualization.UpdateDisplayedGeometry;

			ModelCreationHandler.Dispose();
			CameraCalibrationHandler.Dispose();
			Window.DisposeAll();
		}

		/// <summary>
		/// Pass notification to CameraCalibrationHandler. Notify MasterControl that a whole operation has ended.
		/// </summary>
		public void CalibrationAxes_Changed(CalibrationAxes calibrationAxes)
		{
			if (Initialized)
			{
				CameraCalibrationHandler.CalibrationAxes_Changed(calibrationAxes);
				Control.ImageEndOperation();
			}
		}

		/// <summary>
		/// Pass notification to CameraCalibrationHandler. Notify MasterControl that a whole operation has ended.
		/// </summary>
		public void InvertedAxes_Changed(InvertedAxes invertedAxes)
		{
			if (Initialized)
			{
				CameraCalibrationHandler.InvertedAxes_Changed(invertedAxes);
				Control.ImageEndOperation();
			}
		}

		/// <summary>
		/// Choose active handler based on design tool.
		/// </summary>
		public void DesignTool_Changed(DesignTool newDesignTool)
		{
			CurrentDesignTool = newDesignTool;

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

		/// <summary>
		/// Pass notification to ModelCreationHandler.
		/// </summary>
		public void ModelCreationTool_Changed(ModelCreationTool newModelCreationTool)
		{
			ModelCreationHandler.CreationTool_Changed(newModelCreationTool);
		}

		/// <summary>
		/// Pass notification to CameraModelCalibrationHandler.
		/// </summary>
		public void CameraModelCalibrationTool_Changed(CameraModelCalibrationTool newCameraModelCalibrationTool)
		{
			CameraModelCalibrationHandler.CalibrationTool_Changed(newCameraModelCalibrationTool);
		}

		/// <summary>
		/// Warn user about closing the window and proceed to close if user agrees.
		/// </summary>
		public void Close_Clicked()
		{
			string message = "Do you really want to close this image? All corresponding calibration data and undo history will be lost!";
			if (Window.DisplayWarningProceedMessage("Close Window", message))
			{
				Window.Close();
				Control.WindowRemoved(this);
			}
		}

		/// <summary>
		/// Pass notification to handlers.
		/// </summary>
		public void UpdateDisplayedGeometry()
		{
			ModelVisualization.UpdateDisplayedGeometry();
			CameraCalibrationHandler.UpdateDisplayedGeometry();
			CameraModelCalibrationHandler.UpdateDisplayedGeometry();

			// reset active on all handlers to reset handlers to default state.
			DesignTool_Changed(CurrentDesignTool);
		}

		/// <summary>
		/// Update displayed model to model passed by parameter.
		/// </summary>
		public void UpdateModel(Model model)
		{
			ModelVisualization.UpdateModel(model);
			CameraModelCalibrationHandler.UpdateModel(model);
			ModelCreationHandler.UpdateModel(model);
		}
	}
}
