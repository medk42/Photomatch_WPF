using Photomatch_ProofOfConcept_WPF.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Gui.GuiControls.Helper
{
	class ModelHoverEllipse
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

		private Model Model;
		private PerspectiveData Perspective;
		private IWindow Window;
		private double PointGrabRadius;
		private double PointDrawRadius;
		private Vector2 LastMouse;

		public ModelHoverEllipse(Model model, PerspectiveData perspective, IWindow window, double pointGrabRadius, double pointDrawRadius)
		{
			this.Model = model;
			this.Window = window;
			this.PointGrabRadius = pointGrabRadius;
			this.PointDrawRadius = pointDrawRadius;
			this.Perspective = perspective;

			this.Ellipse = Window.CreateEllipse(new Vector2(), PointDrawRadius, ApplicationColor.Model);
			this.Ellipse.Visible = false;
		}

		public void MouseEvent(Vector2 mouseCoord)
		{
			this.LastMouse = mouseCoord;

			if (!Active)
				return;

			Ellipse.Visible = false;
			foreach (Vertex point in Model.Vertices)
			{
				Vector2 pointPos = Perspective.WorldToScreen(point.Position);
				if (Window.ScreenDistance(mouseCoord, pointPos) < PointGrabRadius)
				{
					Ellipse.Position = pointPos;
					Ellipse.Visible = true;
					break;
				}
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
