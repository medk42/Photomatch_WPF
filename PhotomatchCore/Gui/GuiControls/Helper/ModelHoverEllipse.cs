using PhotomatchCore.Logic;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using PhotomatchCore.Logic.Model;

namespace PhotomatchCore.Gui.GuiControls.Helper
{
	/// <summary>
	/// Class for displaying the vertex under mouse.
	/// </summary>
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

		/// <summary>
		/// Reference to the ellipse, for changing properties (for example color).
		/// </summary>
		public IEllipse Ellipse { get; set; }

		private ModelVisualization ModelVisualization;
		private IImageView Window;
		private double PointDrawRadius;
		private Vector2 LastMouse;

		/// <param name="modelVisualization">ModelVisualization object for passed window.</param>
		/// <param name="window">Window in which the point will be displayed.</param>
		/// <param name="pointDrawRadius">Radius of the displayed point in pixels on screen.</param>
		public ModelHoverEllipse(ModelVisualization modelVisualization, IImageView window, double pointDrawRadius)
		{
			this.ModelVisualization = modelVisualization;
			this.Window = window;
			this.PointDrawRadius = pointDrawRadius;

			this.Ellipse = Window.CreateEllipse(new Vector2(), PointDrawRadius, ApplicationColor.Vertex);
			this.Ellipse.Visible = false;
		}

		/// <summary>
		/// Get vertex under mouse and move the point above it.
		/// </summary>
		/// <returns>True if there is a vertex under mouse.</returns>
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
