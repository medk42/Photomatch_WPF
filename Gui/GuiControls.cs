﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SixLabors.ImageSharp;

using GuiInterfaces;
using Logging;
using GuiPoints;
using Perspective;
using MatrixVector;
using GuiEnums;
using Lines;
using Serializables;
using Photomatch_ProofOfConcept_WPF.Logic;

namespace GuiControls
{
	class EdgeEventListener
	{
		private readonly ILine WindowLine;
		private readonly PerspectiveData Perspective;

		public EdgeEventListener(ILine windowLine, PerspectiveData perspective)
		{
			this.WindowLine = windowLine;
			this.Perspective = perspective;
		}

		public void StartPositionChanged(Vector3 newPosition)
		{
			WindowLine.Start = Perspective.WorldToScreen(newPosition);
		}

		public void EndPositionChanged(Vector3 newPosition)
		{
			WindowLine.End = Perspective.WorldToScreen(newPosition);
		}
	}

	class CameraCalibrationHandler
	{
		public delegate void CoordSystemUpdateEventHandler();
		public CoordSystemUpdateEventHandler CoordSystemUpdateEvent;

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

		private ILine LineA1, LineA2, LineB1, LineB2;
		private ILine LineX, LineY, LineZ;
		private DraggablePoints DraggablePoints;

		private PerspectiveData Perspective;
		private IWindow Window;

		private double PointGrabRadius;
		private double PointDrawRadius;

		public CameraCalibrationHandler(PerspectiveData perspective, IWindow window,  double pointGrabRadius, double pointDrawRadius)
		{
			this.Perspective = perspective;
			this.Window = window;
			this.PointGrabRadius = pointGrabRadius;
			this.PointDrawRadius = pointDrawRadius;

			this.DraggablePoints = new DraggablePoints(Window, PointGrabRadius);
			Window.DisplayCalibrationAxes(Perspective.CalibrationAxes);
			Window.DisplayInvertedAxes(Perspective.CalibrationAxes, Perspective.InvertedAxes);
			CreateCoordSystemLines();
			CreatePerspectiveLines();

			this.Active = false;
			SetActive(Active);
		}

		public void MouseMove(Vector2 mouseCoord)
		{
			if (Active)
				DraggablePoints.MouseMove(mouseCoord);
		}

		public void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			if (Active)
				DraggablePoints.MouseDown(mouseCoord, button);
		}

		public void MouseUp(Vector2 mouseCoord, MouseButton button)
		{
			if (Active)
				DraggablePoints.MouseUp(mouseCoord, button);
		}

		private void CreatePerspectiveLines()
		{
			Tuple<ApplicationColor, ApplicationColor> colors = GetColorsFromCalibrationAxes(Perspective.CalibrationAxes);

			LineA1 = Window.CreateLine(Perspective.LineA1.Start, Perspective.LineA1.End, PointDrawRadius, colors.Item1);
			LineA2 = Window.CreateLine(Perspective.LineA2.Start, Perspective.LineA2.End, PointDrawRadius, colors.Item1);
			LineB1 = Window.CreateLine(Perspective.LineB1.Start, Perspective.LineB1.End, PointDrawRadius, colors.Item2);
			LineB2 = Window.CreateLine(Perspective.LineB2.Start, Perspective.LineB2.End, PointDrawRadius, colors.Item2);

			AddDraggablePointsForPerspectiveLine(LineA1,
				(value) => Perspective.LineA1 = Perspective.LineA1.WithStart(value),
				(value) => Perspective.LineA1 = Perspective.LineA1.WithEnd(value));
			AddDraggablePointsForPerspectiveLine(LineA2,
				(value) => Perspective.LineA2 = Perspective.LineA2.WithStart(value),
				(value) => Perspective.LineA2 = Perspective.LineA2.WithEnd(value));
			AddDraggablePointsForPerspectiveLine(LineB1,
				(value) => Perspective.LineB1 = Perspective.LineB1.WithStart(value),
				(value) => Perspective.LineB1 = Perspective.LineB1.WithEnd(value));
			AddDraggablePointsForPerspectiveLine(LineB2,
				(value) => Perspective.LineB2 = Perspective.LineB2.WithStart(value),
				(value) => Perspective.LineB2 = Perspective.LineB2.WithEnd(value));
		}

		private Tuple<ApplicationColor, ApplicationColor> GetColorsFromCalibrationAxes(CalibrationAxes axes)
		{
			ApplicationColor colorA, colorB;

			switch (axes)
			{
				case CalibrationAxes.XY:
					colorA = ApplicationColor.XAxis;
					colorB = ApplicationColor.YAxis;
					break;
				case CalibrationAxes.YX:
					colorA = ApplicationColor.YAxis;
					colorB = ApplicationColor.XAxis;
					break;
				case CalibrationAxes.XZ:
					colorA = ApplicationColor.XAxis;
					colorB = ApplicationColor.ZAxis;
					break;
				case CalibrationAxes.ZX:
					colorA = ApplicationColor.ZAxis;
					colorB = ApplicationColor.XAxis;
					break;
				case CalibrationAxes.YZ:
					colorA = ApplicationColor.YAxis;
					colorB = ApplicationColor.ZAxis;
					break;
				case CalibrationAxes.ZY:
					colorA = ApplicationColor.ZAxis;
					colorB = ApplicationColor.YAxis;
					break;
				default:
					throw new Exception("Unexpected switch case.");
			}

			return new Tuple<ApplicationColor, ApplicationColor>(colorA, colorB);
		}

		private void AddDraggablePointsForPerspectiveLine(ILine line, UpdateValue<Vector2> updateValueStart, UpdateValue<Vector2> updateValueEnd)
		{
			DraggablePoints.Points.Add(new ActionPoint(line.Start, (value) =>
			{
				line.Start = value;
				updateValueStart(value);
				UpdateCoordSystemLines();
			}));
			DraggablePoints.Points.Add(new ActionPoint(line.End, (value) =>
			{
				line.End = value;
				updateValueEnd(value);
				UpdateCoordSystemLines();
			}));
		}

		private void CreateCoordSystemLines()
		{
			var origin = new Vector2();
			LineX = Window.CreateLine(origin, origin, PointDrawRadius, ApplicationColor.XAxis);
			LineY = Window.CreateLine(origin, origin, PointDrawRadius, ApplicationColor.YAxis);
			LineZ = Window.CreateLine(origin, origin, PointDrawRadius, ApplicationColor.ZAxis);

			DraggablePoints.Points.Add(new ActionPoint(Perspective.Origin, (value) =>
			{
				Perspective.Origin = value;
				UpdateCoordSystemLines();
			}));

			UpdateCoordSystemLines();
		}

		private void UpdateCoordSystemLines()
		{
			Vector2 dirX = Perspective.GetXDirAt(Perspective.Origin);
			Vector2 dirY = Perspective.GetYDirAt(Perspective.Origin);
			Vector2 dirZ = Perspective.GetZDirAt(Perspective.Origin);

			LineX.Start = Perspective.Origin;
			LineY.Start = Perspective.Origin;
			LineZ.Start = Perspective.Origin;

			if (dirX.Valid && dirY.Valid && dirZ.Valid)
			{
				Vector2 imageSize = new Vector2(Perspective.Image.Width, Perspective.Image.Height);

				Vector2 endX = Intersections2D.GetRayInsideBoxIntersection(new Ray2D(Perspective.Origin, dirX), new Vector2(), imageSize);
				Vector2 endY = Intersections2D.GetRayInsideBoxIntersection(new Ray2D(Perspective.Origin, dirY), new Vector2(), imageSize);
				Vector2 endZ = Intersections2D.GetRayInsideBoxIntersection(new Ray2D(Perspective.Origin, dirZ), new Vector2(), imageSize);

				LineX.End = endX.Valid ? endX : Perspective.Origin;
				LineY.End = endY.Valid ? endY : Perspective.Origin;
				LineZ.End = endZ.Valid ? endZ : Perspective.Origin;
			}
			else
			{
				LineX.End = LineX.Start + new Vector2(Perspective.Image.Height * 0.1, 0);
				LineY.End = LineY.Start + new Vector2(0, Perspective.Image.Height * 0.1);
				LineZ.End = LineZ.Start;
			}

			CoordSystemUpdateEvent?.Invoke();
		}

		private void SetActive(bool active)
		{
			LineA1.Visible = active;
			LineA2.Visible = active;
			LineB1.Visible = active;
			LineB2.Visible = active;

			LineX.Visible = active;
			LineY.Visible = active;
			LineZ.Visible = active;
		}

		public void Dispose()
		{
			Perspective = null;
		}

		public  void CalibrationAxes_Changed(CalibrationAxes calibrationAxes)
		{
			Perspective.CalibrationAxes = calibrationAxes;
			Window.DisplayCalibrationAxes(Perspective.CalibrationAxes);
			Window.DisplayInvertedAxes(Perspective.CalibrationAxes, Perspective.InvertedAxes);

			UpdateCoordSystemLines();

			Tuple<ApplicationColor, ApplicationColor> colors = GetColorsFromCalibrationAxes(Perspective.CalibrationAxes);
			LineA1.Color = colors.Item1;
			LineA2.Color = colors.Item1;
			LineB1.Color = colors.Item2;
			LineB2.Color = colors.Item2;
		}

		public void InvertedAxes_Changed(InvertedAxes invertedAxes)
		{
			Perspective.InvertedAxes = invertedAxes;
			Window.DisplayInvertedAxes(Perspective.CalibrationAxes, Perspective.InvertedAxes);

			UpdateCoordSystemLines();
		}
	}

	class ModelCreationHandler
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

		private Model Model;

		private List<Tuple<ILine, Edge, EdgeEventListener>> ModelLines = new List<Tuple<ILine, Edge, EdgeEventListener>>();
		private IEllipse ModelHoverEllipse;
		private ModelCreationTool ModelCreationTool;
		private Vector2 LastMouseCoord;

		private Vertex ModelDraggingVertex = null;
		private Ray2D ModelDraggingXAxis, ModelDraggingYAxis, ModelDraggingZAxis, LastRay;
		private Vertex ModelDraggingLineStart;
		private ILine ModelDraggingLine;
		private bool HoldDirection;
		private Vector3 LastDirection = new Vector3(1, 0, 0);

		private PerspectiveData Perspective;
		private IWindow Window;

		private double PointGrabRadius;
		private double PointDrawRadius;

		public ModelCreationHandler(Model model, PerspectiveData perspective, IWindow window, double pointGrabRadius, double pointDrawRadius)
		{
			this.Model = model;
			this.Perspective = perspective;
			this.Window = window;
			this.PointGrabRadius = pointGrabRadius;
			this.PointDrawRadius = pointDrawRadius;

			CreateModelLines();
			this.ModelHoverEllipse = Window.CreateEllipse(new Vector2(), PointDrawRadius, ApplicationColor.Model);
			this.ModelHoverEllipse.Visible = false;

			this.Active = false;
			SetActive(Active);
		}

		private void HandleHoverEllipse(Vector2 mouseCoord)
		{
			ModelHoverEllipse.Visible = false;
			foreach (Vertex point in Model.Vertices)
			{
				Vector2 pointPos = Perspective.WorldToScreen(point.Position);
				if (Window.ScreenDistance(mouseCoord, pointPos) < PointGrabRadius)
				{
					ModelHoverEllipse.Position = pointPos;
					ModelHoverEllipse.Visible = true;
					break;
				}
			}
		}

		public void MouseMove(Vector2 mouseCoord)
		{
			if (Active)
			{
				this.LastMouseCoord = mouseCoord;

				switch (ModelCreationTool)
				{
					case ModelCreationTool.Delete:
						HandleHoverEllipse(mouseCoord);
						break;
					case ModelCreationTool.Edge:
						MouseMoveEdge(mouseCoord);
						HandleHoverEllipse(mouseCoord);
						break;
					default:
						throw new Exception("Unknown switch case.");
				}
			}
		}

		public void MouseDown(Vector2 mouseCoord, MouseButton button)
		{

			if (Active)
			{
				this.LastMouseCoord = mouseCoord;

				switch (ModelCreationTool)
				{
					case ModelCreationTool.Delete:
						MouseDownDelete(mouseCoord, button);
						HandleHoverEllipse(mouseCoord);
						break;
					case ModelCreationTool.Edge:
						MouseDownEdge(mouseCoord, button);
						HandleHoverEllipse(mouseCoord);
						break;
					default:
						throw new Exception("Unknown switch case.");
				}
			}
		}

		private void MouseDownDelete(Vector2 mouseCoord, MouseButton button)
		{
			if (button != MouseButton.Left)
				return;

			Vertex foundPoint = GetVertexUnderMouse(mouseCoord);

			if (foundPoint != null)
				foundPoint.Remove();
		}

		private void MouseMoveEdge(Vector2 mouseCoord)
		{
			if (ModelDraggingVertex != null)
			{
				Vertex foundPoint = GetVertexUnderMouse(mouseCoord);

				if (foundPoint != null && foundPoint != ModelDraggingVertex)
				{
					if (HoldDirection)
					{
						Vector3Proj foundPointProj = Intersections3D.ProjectVectorToRay(foundPoint.Position, new Ray3D(ModelDraggingLineStart.Position, LastDirection));
						ModelDraggingVertex.Position = foundPointProj.Projection;
					}
					else
					{
						ModelDraggingVertex.Position = foundPoint.Position;
						ModelDraggingLine.Color = ApplicationColor.Model;
						LastDirection = (foundPoint.Position - ModelDraggingLineStart.Position).Normalized();

						Vector2 startScreen = Perspective.WorldToScreen(ModelDraggingLineStart.Position);
						Vector2 endScreen = Perspective.WorldToScreen(foundPoint.Position);

						LastRay = new Ray2D(startScreen, (endScreen - startScreen));
					}
				}
				else if (HoldDirection)
				{
					Vector2Proj mouseProj = Intersections2D.ProjectVectorToRay(mouseCoord, LastRay);
					ModelDraggingVertex.Position = Intersections3D.GetRayRayClosest(Perspective.ScreenToWorldRay(mouseProj.Projection), new Ray3D(ModelDraggingLineStart.Position, LastDirection)).RayBClosest;
				}
				else
				{
					Vector2Proj projX = Intersections2D.ProjectVectorToRay(mouseCoord, ModelDraggingXAxis);
					Vector2Proj projY = Intersections2D.ProjectVectorToRay(mouseCoord, ModelDraggingYAxis);
					Vector2Proj projZ = Intersections2D.ProjectVectorToRay(mouseCoord, ModelDraggingZAxis);

					if (projX.Distance < projY.Distance)
					{
						if (projX.Distance < projZ.Distance)
						{
							LastDirection = new Vector3(1, 0, 0);
							LastRay = ModelDraggingXAxis;
							ModelDraggingVertex.Position = Intersections3D.GetRayRayClosest(Perspective.ScreenToWorldRay(projX.Projection), new Ray3D(ModelDraggingLineStart.Position, LastDirection)).RayBClosest;
							ModelDraggingLine.Color = ApplicationColor.XAxis;
						}
						else
						{
							LastDirection = new Vector3(0, 0, 1);
							LastRay = ModelDraggingZAxis;
							ModelDraggingVertex.Position = Intersections3D.GetRayRayClosest(Perspective.ScreenToWorldRay(projZ.Projection), new Ray3D(ModelDraggingLineStart.Position, LastDirection)).RayBClosest;
							ModelDraggingLine.Color = ApplicationColor.ZAxis;
						}
					}
					else if (projY.Distance < projZ.Distance)
					{
						LastDirection = new Vector3(0, 1, 0);
						LastRay = ModelDraggingYAxis;
						ModelDraggingVertex.Position = Intersections3D.GetRayRayClosest(Perspective.ScreenToWorldRay(projY.Projection), new Ray3D(ModelDraggingLineStart.Position, LastDirection)).RayBClosest;
						ModelDraggingLine.Color = ApplicationColor.YAxis;
					}
					else
					{
						LastDirection = new Vector3(0, 0, 1);
						LastRay = ModelDraggingZAxis;
						ModelDraggingVertex.Position = Intersections3D.GetRayRayClosest(Perspective.ScreenToWorldRay(projZ.Projection), new Ray3D(ModelDraggingLineStart.Position, LastDirection)).RayBClosest;
						ModelDraggingLine.Color = ApplicationColor.ZAxis;
					}
				}
			}
		}

		private void MouseDownEdge(Vector2 mouseCoord, MouseButton button)
		{
			if (button != MouseButton.Left)
				return;

			Vertex foundPoint = GetVertexUnderMouse(mouseCoord);

			if (ModelDraggingVertex != null)
			{
				if (foundPoint != null && foundPoint != ModelDraggingVertex && !HoldDirection)
				{
					ModelDraggingVertex.Remove();
					Model.AddEdge(ModelDraggingLineStart, foundPoint);
				}

				ModelDraggingVertex = null;
				ModelDraggingLine.Color = ApplicationColor.Model;
			}
			else
			{
				if (foundPoint != null)
				{
					Vector2 screenPos = Perspective.WorldToScreen(foundPoint.Position);

					ModelDraggingVertex = Model.AddVertex(foundPoint.Position);
					ModelDraggingXAxis = new Ray2D(screenPos, Perspective.GetXDirAt(screenPos));
					ModelDraggingYAxis = new Ray2D(screenPos, Perspective.GetYDirAt(screenPos));
					ModelDraggingZAxis = new Ray2D(screenPos, Perspective.GetZDirAt(screenPos));
					ModelDraggingLineStart = foundPoint;

					Model.AddEdge(foundPoint, ModelDraggingVertex);
				}
			}
		}

		private Vertex GetVertexUnderMouse(Vector2 mouseCoord)
		{

			foreach (Vertex point in Model.Vertices)
			{
				Vector2 pointPos = Perspective.WorldToScreen(point.Position);
				if (Window.ScreenDistance(mouseCoord, pointPos) < PointGrabRadius)
				{
					return point;
				}
			}

			return null;
		}

		private void CancelLineCreate()
		{
			ModelDraggingVertex.Remove();
			ModelDraggingVertex = null;
			HandleHoverEllipse(LastMouseCoord);
		}

		public void MouseUp(Vector2 mouseCoord, MouseButton button) { }

		public void KeyDown(KeyboardKey key)
		{
			switch (key)
			{
				case KeyboardKey.LeftShift:
					HoldDirection = true;
					break;
				case KeyboardKey.Escape:
					CancelLineCreate();
					break;
			}
		}

		public void KeyUp(KeyboardKey key)
		{
			switch (key)
			{
				case KeyboardKey.LeftShift:
					HoldDirection = false;
					break;
			}
		}

		private void EdgeRemoved(Edge edge)
		{
			edge.EdgeRemovedEvent -= EdgeRemoved;
			var tuple = ModelLines.Find((edgeTuple) => edgeTuple.Item2 == edge);

			edge.StartPositionChangedEvent -= tuple.Item3.StartPositionChanged;
			edge.EndPositionChangedEvent -= tuple.Item3.EndPositionChanged;

			tuple.Item1.Dispose();
			ModelLines.Remove(tuple);
		}

		private void EdgeAdderHelper(Edge edge)
		{
			Vector2 start = Perspective.WorldToScreen(edge.Start.Position);
			Vector2 end = Perspective.WorldToScreen(edge.End.Position);
			ILine windowLine = Window.CreateLine(start, end, 0, ApplicationColor.Model);
			EdgeEventListener edgeEventListener = new EdgeEventListener(windowLine, Perspective);
			edge.StartPositionChangedEvent += edgeEventListener.StartPositionChanged;
			edge.EndPositionChangedEvent += edgeEventListener.EndPositionChanged;
			edge.EdgeRemovedEvent += EdgeRemoved;
			ModelLines.Add(new Tuple<ILine, Edge, EdgeEventListener>(windowLine, edge, edgeEventListener));
			ModelDraggingLine = windowLine;
		}

		private void CreateModelLines()
		{
			Model.AddEdgeEvent += EdgeAdderHelper;

			foreach (Edge line in Model.Edges)
				EdgeAdderHelper(line);
		}

		private void SetActive(bool active)
		{
			ShowModel(active);
		}

		public void ShowModel(bool show)
		{
			foreach (var lineTuple in ModelLines)
				lineTuple.Item1.Visible = show;
		}

		public void UpdateDisplayedLines()
		{
			foreach (var lineTuple in ModelLines)
			{
				lineTuple.Item1.Start = Perspective.WorldToScreen(lineTuple.Item2.Start.Position);
				lineTuple.Item1.End = Perspective.WorldToScreen(lineTuple.Item2.End.Position);
			}
		}

		public void Dispose()
		{
			foreach (var lineTuple in ModelLines)
			{
				lineTuple.Item2.StartPositionChangedEvent -= lineTuple.Item3.StartPositionChanged;
				lineTuple.Item2.EndPositionChangedEvent -= lineTuple.Item3.EndPositionChanged;
				lineTuple.Item2.EdgeRemovedEvent -= EdgeRemoved;
			}

			ModelLines.Clear();

			Model.AddEdgeEvent -= EdgeAdderHelper;

			Perspective = null;
		}

		public void CreationTool_Changed(ModelCreationTool newModelCreationTool)
		{
			this.ModelCreationTool = newModelCreationTool;
		}
	}

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

	public class ImageWindow
	{
		private static readonly double PointGrabRadius = 8;
		private static readonly double PointDrawRadius = 4;

		private MasterGUI Gui;
		private ILogger Logger;
		private IWindow Window { get; }
		private PerspectiveData Perspective;

		private ModelCreationHandler ModelCreationHandler;
		private CameraCalibrationHandler CameraCalibrationHandler;
		private CameraModelCalibrationHandler CameraModelCalibrationHandler;

		private bool Initialized = false;

		public ISafeSerializable<PerspectiveData> PerspectiveSafeSerializable
		{
			get => Perspective;
		}

		public ImageWindow(PerspectiveData perspective, MasterGUI gui, ILogger logger, Model model, DesignTool currentDesignTool, ModelCreationTool currentModelCreationTool)
		{
			this.Gui = gui;
			this.Logger = logger;
			this.Window = Gui.CreateImageWindow(this, Path.GetFileName(perspective.ImagePath));
			this.Perspective = perspective;
			this.Window.SetImage(perspective.Image);

			this.ModelCreationHandler = new ModelCreationHandler(model, Perspective, Window, PointGrabRadius, PointDrawRadius);
			this.CameraCalibrationHandler = new CameraCalibrationHandler(Perspective, Window, PointGrabRadius, PointDrawRadius);
			this.CameraModelCalibrationHandler = new CameraModelCalibrationHandler(ModelCreationHandler);

			this.CameraCalibrationHandler.CoordSystemUpdateEvent += ModelCreationHandler.UpdateDisplayedLines;

			this.DesignTool_Changed(currentDesignTool);
			this.ModelCreationTool_Changed(currentModelCreationTool);

			this.Initialized = true;
		}

		public void MouseMove(Vector2 mouseCoord)
		{
			CameraCalibrationHandler.MouseMove(mouseCoord);
			ModelCreationHandler.MouseMove(mouseCoord);
		}

		public void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			CameraCalibrationHandler.MouseDown(mouseCoord, button);
			ModelCreationHandler.MouseDown(mouseCoord, button);
		}

		public void MouseUp(Vector2 mouseCoord, MouseButton button)
		{
			CameraCalibrationHandler.MouseUp(mouseCoord, button);
			ModelCreationHandler.MouseUp(mouseCoord, button);
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
			CameraCalibrationHandler.CoordSystemUpdateEvent -= ModelCreationHandler.UpdateDisplayedLines;

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

		public void CameraModelCalibrationTool_Click(CameraModelCalibrationTool cameraModelCalibrationTool)
		{
			CameraModelCalibrationHandler.CalibrationTool_Click(cameraModelCalibrationTool);
		}
	}

	public class MasterControl : Actions
	{
		private static readonly ulong ProjectFileChecksum = 0x54_07_02_47_23_43_94_42;
		private static readonly string NewProjectName = "new project...";

		private MasterGUI Gui;
		private ILogger Logger;
		private List<ImageWindow> Windows;
		private ProjectState State;
		private string ProjectPath;
		private Model Model;
		private DesignTool DesignTool;
		private ModelCreationTool ModelCreationTool;

		public MasterControl(MasterGUI gui)
		{
			this.Gui = gui;
			this.Logger = gui;
			this.Windows = new List<ImageWindow>();
			this.State = ProjectState.None;
			this.ProjectPath = null;
			this.Model = new Model();
			this.Model.AddVertex(new Vector3());
			this.DesignTool = DesignTool.CameraCalibration;
			this.ModelCreationTool = ModelCreationTool.Edge;

			Gui.DisplayProjectName(NewProjectName);
		}

		public void NewProject_Pressed() => Reset();

		public void LoadImage_Pressed()
		{
			string filePath = Gui.GetImageFilePath();
			if (filePath == null)
			{
				Logger.Log("Load Image", "No file was selected.", LogType.Info);
				return;
			}

			Image image = null;
			try
			{
				image = Image.Load(filePath);
			}
			catch (Exception ex)
			{
				if (ex is FileNotFoundException)
					Logger.Log("Load Image", "File not found.", LogType.Warning);
				else if (ex is UnknownImageFormatException)
					Logger.Log("Load Image", "Incorrect or unsupported image format.", LogType.Warning);
				else
					throw ex;
			}

			if (image != null)
			{
				Logger.Log("Load Image", "File loaded successfully.", LogType.Info);
				Windows.Add(new ImageWindow(new PerspectiveData(image, filePath), Gui, Logger, Model, DesignTool, ModelCreationTool));

				if (State == ProjectState.None)
					State = ProjectState.NewProject;

				Gui.DisplayModelCreationTool(ModelCreationTool);
				Gui.DisplayDesignTool(DesignTool);
			}
		}

		public void SaveProject_Pressed()
		{
			switch (State)
			{
				case ProjectState.None:
					Logger.Log("Save Project", "Nothing to save.", LogType.Warning);
					return;
				case ProjectState.NewProject:
					SaveProjectAs_Pressed();
					break;
				case ProjectState.NamedProject:
					if (!SaveProject(ProjectPath))
						return;
					break;
				default:
					throw new NotImplementedException("Unknown ProjectState");
			}

			Logger.Log("Save Project", "Successfully saved project.", LogType.Info);
		}

		public void SaveProjectAs_Pressed()
		{
			switch (State)
			{
				case ProjectState.None:
					Logger.Log("Save Project", "Nothing to save.", LogType.Warning);
					return;
				case ProjectState.NewProject:
				case ProjectState.NamedProject:
					string filePath = Gui.GetSaveProjectFilePath();
					if (filePath == null)
					{
						Logger.Log("Save Project", "No file was selected.", LogType.Info);
						return;
					}
					if (!SaveProject(filePath))
						return;
					State = ProjectState.NamedProject;
					ProjectPath = filePath;

					string projectName = Path.GetFileName(filePath);
					Gui.DisplayProjectName(projectName);
					break;
				default:
					throw new NotImplementedException("Unknown ProjectState");
			}

			Logger.Log("Save Project", "Successfully saved project.", LogType.Info);
		}

		private bool SaveProject(string fileName)
		{
			try
			{
				using (var fileStream = File.Create(fileName))
				{
					var writer = new BinaryWriter(fileStream);

					writer.Write(ProjectFileChecksum);
					Model.Serialize(writer);
					writer.Write((int)DesignTool);
					writer.Write(Windows.Count);
					foreach (ImageWindow window in Windows)
					{
						window.PerspectiveSafeSerializable.Serialize(writer);
					}
				}
			}
			catch (Exception ex)
			{
				if (ex is UnauthorizedAccessException)
					Logger.Log("Save Project", "Unauthorized access to file.", LogType.Warning);
				else if (ex is IOException)
					Logger.Log("Save Project", "Save operation was not successful.", LogType.Warning);
				else if (ex is ArgumentException || ex is DirectoryNotFoundException || ex is NotSupportedException)
					Logger.Log("Save Project", "Path is invalid.", LogType.Warning);
				else if (ex is PathTooLongException)
					Logger.Log("Save Project", "Path is too long.", LogType.Warning);
				else throw ex;

				return false;
			}

			return true;
		}

		public void LoadProject_Pressed()
		{
			string filePath = Gui.GetLoadProjectFilePath();
			if (filePath == null)
			{
				Logger.Log("Load Project", "No file was selected.", LogType.Info);
				return;
			}

			this.Reset();

			try
			{
				using (var fileStream = File.OpenRead(filePath))
				{
					var reader = new BinaryReader(fileStream);

					ulong checksum = reader.ReadUInt64();
					if (ProjectFileChecksum != checksum)
					{
						Logger.Log("Load Project", "Invalid file.", LogType.Warning);
						return;
					}

					Model = ISafeSerializable<Model>.CreateDeserialize(reader);

					DesignTool = (DesignTool)reader.ReadInt32();
					Gui.DisplayDesignTool(DesignTool);

					int windowCount = reader.ReadInt32();
					for (int i = 0; i < windowCount; i++)
					{
						PerspectiveData perspective = ISafeSerializable<PerspectiveData>.CreateDeserialize(reader);
						Windows.Add(new ImageWindow(perspective, Gui, Logger, Model, DesignTool, ModelCreationTool));
					}
				}

				string projectName = Path.GetFileName(filePath);
				Gui.DisplayProjectName(projectName);

				State = ProjectState.NamedProject;
				ProjectPath = filePath;
				Logger.Log("Load Project", $"Successfully loaded project {projectName}.", LogType.Info);
			}
			catch (Exception ex)
			{
				this.Reset();

				if (ex is UnauthorizedAccessException)
					Logger.Log("Load Project", "Unauthorized access to file or path was directory.", LogType.Warning);
				else if (ex is IOException || ex is ArgumentOutOfRangeException)
					Logger.Log("Load Project", "Invalid file.", LogType.Warning);
				else if (ex is ArgumentException || ex is DirectoryNotFoundException || ex is NotSupportedException)
					Logger.Log("Load Project", "Path is invalid.", LogType.Warning);
				else if (ex is PathTooLongException)
					Logger.Log("Load Project", "Path is too long.", LogType.Warning);
				else if (ex is FileNotFoundException)
					Logger.Log("Load Project", "File not found.", LogType.Warning);
				else
					Logger.Log("Load Project", "Invalid file.", LogType.Warning);
			}
		}

		public void DesignTool_Changed(DesignTool newDesignTool)
		{
			if (this.DesignTool != newDesignTool)
			{
				this.DesignTool = newDesignTool;

				Gui.DisplayDesignTool(newDesignTool);

				foreach (ImageWindow window in Windows)
					window.DesignTool_Changed(newDesignTool);
			}
		}

		public void ModelCreationTool_Changed(ModelCreationTool newModelCreationTool)
		{
			if (this.ModelCreationTool != newModelCreationTool)
			{
				this.ModelCreationTool = newModelCreationTool;

				foreach (ImageWindow window in Windows)
					window.ModelCreationTool_Changed(newModelCreationTool);
			}
		}

		public void Reset()
		{
			foreach (ImageWindow window in Windows)
				window.Dispose();

			Windows.Clear();
			State = ProjectState.None;
			ProjectPath = null;
			Model.Dispose();
			Model.AddVertex(new Vector3());

			Gui.DisplayProjectName(NewProjectName);

			DesignTool = DesignTool.CameraCalibration;
			ModelCreationTool = ModelCreationTool.Edge;
			Gui.DisplayModelCreationTool(ModelCreationTool);
			Gui.DisplayDesignTool(DesignTool);
		}

		public void CameraModelCalibrationTool_Click(CameraModelCalibrationTool cameraModelCalibrationTool)
		{
			foreach (ImageWindow window in Windows)
				window.CameraModelCalibrationTool_Click(cameraModelCalibrationTool);
		}
	}
}
