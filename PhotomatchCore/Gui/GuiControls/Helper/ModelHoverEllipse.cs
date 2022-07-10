using PhotomatchCore.Logic;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using PhotomatchCore.Logic.Model;

namespace PhotomatchCore.Gui.GuiControls.Helper
{
	public class ModelHoverEllipse
	{
		private bool Active_ = true;
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

		public IEllipse Ellipse { get; set; }

		private ModelVisualization ModelVisualization;
		private IImageView Window;
		private double PointDrawRadius;
		private Vector2 LastMouse;

		public ModelHoverEllipse(ModelVisualization modelVisualization, IImageView window, double pointDrawRadius)
		{
			this.ModelVisualization = modelVisualization;
			this.Window = window;
			this.PointDrawRadius = pointDrawRadius;

			this.Ellipse = Window.CreateEllipse(new Vector2(), PointDrawRadius, ApplicationColor.Vertex);
			this.Ellipse.Visible = false;
		}

		public bool MouseEvent(Vector2 mouseCoord)
		{
			this.LastMouse = mouseCoord;

			if (!Active)
				return false;

			Tuple<Vertex, Vector2> foundVertex = ModelVisualization.GetVertexUnderMouse(mouseCoord);

			if (foundVertex.Item1 != null)
			{
				Ellipse.Position = foundVertex.Item2;
				Ellipse.Visible = true;
				return true;
			}
			else
			{
				Ellipse.Visible = false;
				return false;
			}
		}

		private void SetActive(bool active)
		{
			if (active)
				MouseEvent(LastMouse);
			else
				Ellipse.Visible = false;
		}
	}
}
