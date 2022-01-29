using System;
using System.Collections.Generic;
using System.Text;

using Photomatch_ProofOfConcept_WPF.Logic;

namespace Photomatch_ProofOfConcept_WPF.Gui
{
	public delegate void UpdateValue<T>(T value);
	public delegate T GetValue<T>();

	public interface IPoint
	{
		public Vector2 Position { get; set; }
	}

	public class ActionPoint : IPoint
	{
		private Vector2 position_;
		public Vector2 Position
		{
			get => position_;
			set
			{
				if (position_ != value)
				{
					position_ = value;
					UpdateValue(position_);
				}
			}
		}

		private UpdateValue<Vector2> UpdateValue;
		private GetValue<Vector2> GetValue;

		public ActionPoint(Vector2 position, UpdateValue<Vector2> updateValue, GetValue<Vector2> getValue)
		{
			this.UpdateValue = updateValue;
			this.GetValue = getValue;
			this.Position = position;
		}

		public void UpdateSelf()
		{
			Position = GetValue();
		}
	}

	public class DraggablePoints
	{
		public List<IPoint> Points { get; } = new List<IPoint>();

		private IPoint CurrentPoint = null;
		private Vector2 DraggingOffset;
		private IWindow Window;
		private double MaxMouseDistance;

		public DraggablePoints(IWindow window, double maxMouseDistance)
		{
			this.Window = window;
			this.MaxMouseDistance = maxMouseDistance;
		}

		public void MouseMove(Vector2 mouseCoord)
		{
			if (CurrentPoint != null)
			{
				CurrentPoint.Position = mouseCoord + DraggingOffset;
			}
		}

		public void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			if (button == MouseButton.Left)
			{
				foreach (IPoint p in Points)
				{
					if (Window.ScreenDistance(mouseCoord, p.Position) < MaxMouseDistance)
					{
						DraggingOffset = p.Position - mouseCoord;
						CurrentPoint = p;
						break;
					}
				}
			}
		}

		public void MouseUp(Vector2 mouseCoord, MouseButton button)
		{
			if (button == MouseButton.Left)
			{
				CurrentPoint = null;
			}
		}
	}
}
