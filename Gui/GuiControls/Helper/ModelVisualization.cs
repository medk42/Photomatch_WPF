using Photomatch_ProofOfConcept_WPF.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls.Helper
{
	public class ModelVisualization
	{
		public ModelHoverEllipse ModelHoverEllipse { get; }
		public ILine ModelDraggingLine { get; private set; }

		private Model Model;
		private IWindow Window;
		private PerspectiveData Perspective;

		private double PointGrabRadius;
		private double PointDrawRadius;

		private List<Tuple<ILine, Edge, EdgeEventListener>> ModelLines = new List<Tuple<ILine, Edge, EdgeEventListener>>();
		private List<Tuple<IPolygon, Face, FaceEventListener>> ModelFaces = new List<Tuple<IPolygon, Face, FaceEventListener>>();

		public ModelVisualization(PerspectiveData perspective, IWindow window, Model model, double pointGrabRadius, double pointDrawRadius)
		{
			this.Perspective = perspective;
			this.Window = window;
			this.Model = model;

			this.PointGrabRadius = pointGrabRadius;
			this.PointDrawRadius = pointDrawRadius;

			this.ModelHoverEllipse = new ModelHoverEllipse(Model, Perspective, Window, PointGrabRadius, PointDrawRadius);

			CreateModelLines();
		}

		public Tuple<Vertex, Vector2> GetVertexUnderMouse(Vector2 mouseCoord)
		{
			foreach (Vertex point in Model.Vertices)
			{
				Vector2 pointPos = Perspective.WorldToScreen(point.Position);
				if (Window.ScreenDistance(mouseCoord, pointPos) < PointGrabRadius)
				{
					return new Tuple<Vertex, Vector2>(point, pointPos);
				}
			}

			return new Tuple<Vertex, Vector2>(null, new Vector2());
		}

		public Tuple<Edge, ILine> GetEdgeUnderMouse(Vector2 mouseCoord)
		{
			foreach (var edgeTuple in ModelLines)
			{
				Line2D edgeLine = new Line2D(edgeTuple.Item1.Start, edgeTuple.Item1.End);
				Vector2Proj proj = Intersections2D.ProjectVectorToRay(mouseCoord, edgeLine.AsRay());
				if (proj.RayRelative >= 0 && proj.RayRelative <= edgeLine.Length && Window.ScreenDistance(mouseCoord, proj.Projection) < PointGrabRadius)
					return new Tuple<Edge, ILine>(edgeTuple.Item2, edgeTuple.Item1);
			}

			return null;
		}

		public Tuple<Face, IPolygon> GetFaceUnderMouse(Vector2 mouseCoord)
		{
			foreach (var faceTuple in ModelFaces)
			{
				List<Vector2> vertices = new List<Vector2>();
				for (int i = 0; i < faceTuple.Item1.Count; i++)
					vertices.Add(faceTuple.Item1[i]);

				if (Intersections2D.IsPointInsidePolygon(mouseCoord, vertices))
					return new Tuple<Face, IPolygon>(faceTuple.Item2, faceTuple.Item1);
			}

			return null;
		}

		public void ShowModel(bool show)
		{
			foreach (var lineTuple in ModelLines)
				lineTuple.Item1.Visible = show;

			foreach (var faceTuple in ModelFaces)
				faceTuple.Item1.Visible = show;
		}

		public void UpdateDisplayedGeometry()
		{
			foreach (var lineTuple in ModelLines)
			{
				lineTuple.Item1.Start = Perspective.WorldToScreen(lineTuple.Item2.Start.Position);
				lineTuple.Item1.End = Perspective.WorldToScreen(lineTuple.Item2.End.Position);
			}

			foreach (var faceTuple in ModelFaces)
				for (int i = 0; i < faceTuple.Item2.Count; i++)
					faceTuple.Item1[i] = Perspective.WorldToScreen(faceTuple.Item2[i].Position);
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

		private void FaceRemoved(Face face)
		{
			face.FaceRemovedEvent -= FaceRemoved;

			var tuple = ModelFaces.Find((faceTuple) => faceTuple.Item2 == face);
			face.VertexPositionChangedEvent -= tuple.Item3.FaceVertexPositionChanged;

			tuple.Item1.Dispose();
			ModelFaces.Remove(tuple);
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

		private void FaceAdderHelper(Face face)
		{
			IPolygon windowPolygon = Window.CreateFilledPolygon(ApplicationColor.Face);
			for (int i = 0; i < face.Count; i++)
				windowPolygon.Add(Perspective.WorldToScreen(face[i].Position));
			FaceEventListener faceEventListener = new FaceEventListener(windowPolygon, Perspective);
			face.FaceRemovedEvent += FaceRemoved;
			face.VertexPositionChangedEvent += faceEventListener.FaceVertexPositionChanged;
			ModelFaces.Add(new Tuple<IPolygon, Face, FaceEventListener>(windowPolygon, face, faceEventListener));
		}

		private void CreateModelLines()
		{
			Model.AddEdgeEvent += EdgeAdderHelper;
			Model.AddFaceEvent += FaceAdderHelper;

			foreach (Edge edge in Model.Edges)
				EdgeAdderHelper(edge);

			foreach (Face face in Model.Faces)
				FaceAdderHelper(face);
		}

		public void Dispose()
		{
			foreach (var lineTuple in ModelLines)
			{
				lineTuple.Item2.StartPositionChangedEvent -= lineTuple.Item3.StartPositionChanged;
				lineTuple.Item2.EndPositionChangedEvent -= lineTuple.Item3.EndPositionChanged;
				lineTuple.Item2.EdgeRemovedEvent -= EdgeRemoved;

				lineTuple.Item1.Dispose();
			}

			foreach (var faceTuple in ModelFaces)
			{
				faceTuple.Item2.FaceRemovedEvent -= FaceRemoved;
				faceTuple.Item2.VertexPositionChangedEvent -= faceTuple.Item3.FaceVertexPositionChanged;

				faceTuple.Item1.Dispose();
			}

			ModelLines.Clear();

			Model.AddEdgeEvent -= EdgeAdderHelper;
		}
	}
}
