using PhotomatchCore.Gui.GuiControls.Helper;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using PhotomatchCore.Logic.Model;
using PhotomatchCore.Logic.Perspective;

namespace PhotomatchCore.Gui.GuiControls.ToolHandlers.ModelCreationToolHandlers
{
	class ModelCreationFaceNormalsHandler : BaseModelCreationToolHandler
	{
		private readonly static double FacePointEllipseRadius = 3;

		public override ModelCreationTool ToolType => ModelCreationTool.FaceNormals;

		private ModelVisualization ModelVisualization;
		private Model Model;
		private IImageView Window;
		private PerspectiveData Perspective;

		private List<Tuple<ILine, Face>> NormalLines = new List<Tuple<ILine, Face>>();
		private List<IEllipse> FacePointEllipses = new List<IEllipse>();
		private IPolygon Polygon;
		private Face SelectedFace;
		private ILine SelectedLine;

		public ModelCreationFaceNormalsHandler(ModelVisualization modelVisualization, Model model, IImageView window, PerspectiveData perspective)
		{
			ModelVisualization = modelVisualization;
			Model = model;
			Window = window;
			Perspective = perspective;

			Model.ModelChangedEvent += ModelChanged;
		}

		public override void MouseMove(Vector2 mouseCoord)
		{
			if (Active)
			{
				var faceTuple = ModelVisualization.GetFaceUnderMouse(mouseCoord);
				if (faceTuple != null)
				{
					if (Polygon != null)
					{
						if (SelectedFace == faceTuple.Item1)
							return;
						else
							Polygon.Dispose();
					}

					Polygon = Window.CreateFilledPolygon(ApplicationColor.Face);
					SelectedFace = faceTuple.Item1;

					List<Vector2> vertices = new List<Vector2>();
					for (int i = 0; i < faceTuple.Item2.Count; i++)
					{
						Polygon.Add(faceTuple.Item2[i]);
						vertices.Add(faceTuple.Item2[i]);
					}

					if (Intersections2D.IsClockwise(vertices) ^ faceTuple.Item1.Reversed)
						Polygon.Color = ApplicationColor.NormalOutside;
					else
						Polygon.Color = ApplicationColor.NormalInside;

					ResetSelectedLine();
					SelectedLine = NormalLines.Find(tuple => tuple.Item2 == SelectedFace).Item1;
					SelectedLine.Color = ApplicationColor.Highlight;
				}
				else if (Polygon != null)
				{
					Polygon.Dispose();
					Polygon = null;
					SelectedFace = null;

					ResetSelectedLine();
				}
			}
		}

		private void ResetSelectedLine()
		{
			if (SelectedLine != null)
			{
				SelectedLine.Color = ApplicationColor.NormalLine;
				SelectedLine = null;
			}
		}

		public override void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			if (Active)
			{
				var faceTuple = ModelVisualization.GetFaceUnderMouse(mouseCoord);
				if (faceTuple != null)
				{
					faceTuple.Item1.UserReverse();

					List<Vector2> vertices = new List<Vector2>();
					for (int i = 0; i < faceTuple.Item2.Count; i++)
						vertices.Add(faceTuple.Item2[i]);

					if (Intersections2D.IsClockwise(vertices) ^ faceTuple.Item1.Reversed)
						Polygon.Color = ApplicationColor.YAxis;
					else
						Polygon.Color = ApplicationColor.XAxis;

					foreach (var lineTuple in NormalLines)
					{
						if (lineTuple.Item2 == faceTuple.Item1)
						{
							lineTuple.Item1.End = Perspective.WorldToScreen(lineTuple.Item2.FacePoint + lineTuple.Item2.Normal * (lineTuple.Item2.Reversed ? -0.1 : 0.1));
						}
					}
				}
			}
		}

		private void GenerateNormals()
		{
			foreach (Face face in Model.Faces)
			{
				Vector2 facePointScreen = Perspective.WorldToScreen(face.FacePoint);
				Vector2 lineEnd = Perspective.WorldToScreen(face.FacePoint + face.Normal * (face.Reversed ? -0.1 : 0.1));
				FacePointEllipses.Add(Window.CreateEllipse(facePointScreen, FacePointEllipseRadius, ApplicationColor.NormalLine));
				NormalLines.Add(new Tuple<ILine, Face>(Window.CreateLine(facePointScreen, lineEnd, 0, ApplicationColor.NormalLine), face));
			}
		}

		private void DisposeNormals()
		{
			foreach (var lineTuple in NormalLines)
				lineTuple.Item1.Dispose();
			NormalLines.Clear();

			foreach (IEllipse ellipse in FacePointEllipses)
				ellipse.Dispose();
			FacePointEllipses.Clear();
		}

		internal override void SetActive(bool active)
		{
			if (active)
				GenerateNormals();
			else
			{
				DisposeNormals();

				if (Polygon != null)
				{
					Polygon.Dispose();
					Polygon = null;
					SelectedFace = null;
				}
			}
		}

		private void ModelChanged()
		{
			if (NormalLines == null)
				return;

			foreach (var lineTuple in NormalLines)
			{
				lineTuple.Item1.End = Perspective.WorldToScreen(lineTuple.Item2.FacePoint + lineTuple.Item2.Normal * (lineTuple.Item2.Reversed ? -0.1 : 0.1));
			}
		}

		public override void UpdateModel(Model model)
		{
			Model = model;

			if (Active)
			{
				DisposeNormals();
				GenerateNormals();
			}
		}
	}
}
