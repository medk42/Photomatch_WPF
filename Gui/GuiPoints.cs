using System;
using System.Collections.Generic;
using System.Text;

using Photomatch.Logic;

namespace Photomatch.Gui
{
	public delegate void UpdateValue<T>(T value);
	public delegate T GetValue<T>();

	/// <summary>
	/// Interface representing a 2D point.
	/// </summary>
	public interface IPoint
	{
		public Vector2 Position { get; set; }
	}

	/// <summary>
	/// Class representing a 2D point that calls a delegate on position update and 
	/// updates its own position using another delegate when UpdateSelf() is called.
	/// </summary>
	public class ActionPoint : IPoint
	{
		private Vector2 position_;

		/// <summary>
		/// Calls UpdateValue delegate on set.
		/// </summary>
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

		/// <summary>
		/// Create and ActionPoint with initial position, a delegate to be called on position update 
		/// and a delegate that ActionPoint calls when UpdateSelf is called on ActionPoint.
		/// </summary>
		public ActionPoint(Vector2 position, UpdateValue<Vector2> updateValue, GetValue<Vector2> getValue)
		{
			this.UpdateValue = updateValue;
			this.GetValue = getValue;
			this.Position = position;
		}

		/// <summary>
		/// Update own position based on GetValue delegate.
		/// </summary>
		public void UpdateSelf()
		{
			Position = GetValue();
		}
	}

	/// <summary>
	/// Class implementing a mouse dragging operation on a list of points 
	/// by dragging at most one at a time.
	/// </summary>
	public class DraggablePoints
	{
		/// <summary>
		/// Points that can be dragged by a mouse.
		/// </summary>
		public List<IPoint> Points { get; } = new List<IPoint>();

		private IPoint CurrentPoint = null;
		private Vector2 DraggingOffset;
		private IWindow Window;
		private double MaxMouseDistance;

		/// <param name="window">Window where the points are being dragged.</param>
		/// <param name="maxMouseDistance">Maximum distance between mouse and point to start dragging it, in pixels on screen.</param>
		public DraggablePoints(IWindow window, double maxMouseDistance)
		{
			this.Window = window;
			this.MaxMouseDistance = maxMouseDistance;
		}

		private double Clip(double value, double min, double max)
		{
			if (value < min)
				return min;
			if (value > max)
				return max;
			return value;
		}

		public void MouseMove(Vector2 mouseCoord)
		{
			if (CurrentPoint != null)
			{
				Vector2 newPosition = mouseCoord + DraggingOffset;

				double scale = Window.ScreenDistance(new Vector2(), new Vector2(1, 0));

				double maxDistance = MaxMouseDistance / scale;


				CurrentPoint.Position = new Vector2(
					Clip(newPosition.X, maxDistance, Window.Width - maxDistance - 1),
					Clip(newPosition.Y, maxDistance, Window.Height - maxDistance - 1)
				);
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
