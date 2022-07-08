using PhotomatchCore.Data;
using PhotomatchCore.Gui.GuiControls.Helper;
using PhotomatchCore.Interfaces;
using PhotomatchCore.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Gui.GuiControls.ModelCreationToolHandlers
{
	public class ModelCreationComplexFaceHandler : BaseModelCreationToolHandler
	{
		public override ModelCreationTool ToolType => ModelCreationTool.ComplexFace;

		private ModelVisualization ModelVisualization;
		private Model Model;
		private IWindow Window;
		private ILogger Logger;

		private List<ILine> Lines = new List<ILine>();
		private List<Vertex> Vertices = new List<Vertex>();

		public ModelCreationComplexFaceHandler(ModelVisualization modelVisualization, Model model, IWindow window, ILogger logger)
		{
			this.ModelVisualization = modelVisualization;
			this.Model = model;
			this.Window = window;
			this.Logger = logger;

			this.Active = false;
			SetActive(Active);
		}

		private bool CheckCount()
		{
			if (Vertices.Count < 3)
			{
				Logger.Log("Complex Face Creation", "A face needs at least 3 vertices.", LogType.Warning);
				return false;
			}

			return true;
		}

		private bool IsLineCross(Vector2 position)
		{
			if (Vertices.Count <= 2)
				return false;

			for (int i = ((position == Lines[1].Start) ? 2 : 1); i < Lines.Count - 2; i++)
			{
				if (position == Lines[i].Start || position == Lines[i].End || Lines[Lines.Count - 1].Start == Lines[i].Start || Lines[Lines.Count - 1].Start == Lines[i].End)
					continue;

				IntersectionPoint2D lineCountIntersection = Intersections2D.GetLineLineIntersection(new Line2D(Lines[i].Start, Lines[i].End), new Line2D(Lines[Lines.Count - 1].Start, position));
				if (lineCountIntersection.LineARelative >= 0 && lineCountIntersection.LineARelative <= 1 && lineCountIntersection.LineBRelative >= 0 && lineCountIntersection.LineBRelative <= 1)
					return true;
			}

			return false;
		}

		private bool CheckLineCross(Vector2 position)
		{
			bool lineCross = IsLineCross(position);
			if (lineCross)
				Logger.Log("Complex Face Creation", "Face edges can not cross each other.", LogType.Warning);

			return !lineCross;
		}

		private bool IsNormalValid(Vertex vertex)
		{
			if (Vertices.Count >= 3)
			{
				Vector3 faceNormal = Vector3.Cross(Vertices[1].Position - Vertices[0].Position, Vertices[2].Position - Vertices[0].Position).Normalized();
				RayPlaneIntersectionPoint intersectionPoint = Intersections3D.GetRayPlaneIntersection(new Ray3D(vertex.Position, faceNormal), new Plane3D(Vertices[0].Position, faceNormal));

				if (Math.Abs(intersectionPoint.RayRelative) > 1e-6)
				{
					return false;
				}
			}

			return true;
		}

		private bool CheckNormal(Vertex vertex)
		{
			bool valid = IsNormalValid(vertex);
			if (!valid)
				Logger.Log("Complex Face Creation", "Selected vertex does not lay on the same plane.", LogType.Warning);

			return valid;
		}

		public override void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			if (Active)
			{
				if (button == MouseButton.DoubleLeft)
				{
					if (!CheckCount())
						return;
					if (!CheckLineCross(Lines[1].Start))
						return;

					if (Model.AddFace(Vertices) == null)
					{
						Logger.Log("Complex Face Creation", "Face needs to be clockwise or anticlockwise, not both at the same time.", LogType.Warning);
						return;
					}

					Clear();
					return;
				}
				else if (button == MouseButton.Right || button == MouseButton.DoubleRight)
				{
					if (Vertices.Count > 1)
					{
						Vertices.RemoveAt(Vertices.Count - 1);
						ILine removedLine = Lines[Lines.Count - 1];
						Lines.RemoveAt(Lines.Count - 1);
						Lines[Lines.Count - 1].End = mouseCoord;
						removedLine.Dispose();
					}
					else
						Clear();
					return;
				}
				else if (button != MouseButton.Left)
					return;

				Tuple<Vertex, Vector2> found = ModelVisualization.GetVertexUnderMouse(mouseCoord);
				Vertex foundPoint = found.Item1;
				Vector2 foundPosition = found.Item2;

				if (foundPoint != null)
				{
					if (!CheckNormal(foundPoint))
						return;

					if (!CheckLineCross(foundPosition))
						return;

					if (Vertices.Count > 0)
						Lines[Vertices.Count].End = foundPosition;
					else
						Lines.Add(Window.CreateLine(mouseCoord, foundPosition, 0, ApplicationColor.Selected));

					Vertices.Add(foundPoint);
					Lines.Add(Window.CreateLine(foundPosition, mouseCoord, 0, ApplicationColor.Selected));
				}

				ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);
			}
		}

		public override void MouseMove(Vector2 mouseCoord)
		{
			if (Active)
			{
				ModelVisualization.ModelHoverEllipse.Ellipse.Color = ApplicationColor.Vertex;
				ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);
				var foundVertex = ModelVisualization.GetVertexUnderMouse(mouseCoord);
				if (foundVertex.Item1 != null)
				{
					if (!IsNormalValid(foundVertex.Item1) || IsLineCross(foundVertex.Item2))
					{
						ModelVisualization.ModelHoverEllipse.Ellipse.Color = ApplicationColor.Invalid;
					}
				}

				if (Vertices.Count > 0)
				{
					Lines[0].Start = mouseCoord;
					Lines[Vertices.Count].End = mouseCoord;
				}
			}
		}

		public override void KeyDown(KeyboardKey key)
		{
			if (Active)
			{
				switch (key)
				{
					case KeyboardKey.Escape:
						Clear();
						break;
				}
			}
		}

		private void Clear()
		{
			Vertices.Clear();
			foreach (ILine line in Lines)
				line.Dispose();
			Lines.Clear();
		}

		internal override void SetActive(bool active)
		{
			ModelVisualization.ModelHoverEllipse.Active = active;
			ModelVisualization.ModelHoverEllipse.Ellipse.Color = ApplicationColor.Vertex;

			if (!active)
				Clear();
		}

		public override void UpdateModel(Model model)
		{
			Model = model;
		}
	}
}
