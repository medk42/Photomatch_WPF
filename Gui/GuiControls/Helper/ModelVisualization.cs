﻿using Photomatch_ProofOfConcept_WPF.Logic;
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

		public Vertex GetVertexUnderMouse(Vector2 mouseCoord)
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
		}
	}
}
