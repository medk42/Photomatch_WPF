using System;
using System.Collections.Generic;
using System.Text;
using PhotomatchCore.Utilities;

namespace PhotomatchCore.Gui.GuiControls.ToolHandlers.Helper
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
			UpdateValue = updateValue;
			GetValue = getValue;
			Position = position;
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
		private IImageView Window;
		private double MaxMouseDistance;
		private double PointEdgeLimit;

		/// <param name="window">Window where the points are being dragged.</param>
		/// <param name="maxMouseDistance">Maximum distance between mouse and point to start dragging it, in pixels on screen.</param>
		/// <param name="pointEdgeLimit">The closest allowed distance between a point and an edge of the image, in pixels on screen.</param>
		public DraggablePoints(IImageView window, double maxMouseDistance, double pointEdgeLimit)
		{
			Window = window;
			MaxMouseDistance = maxMouseDistance;
			PointEdgeLimit = pointEdgeLimit;
		}

		private double Clip(double value, double min, double max)
		{
			if (value < min)
				return min;
			if (value > max)
				return max;
			return value;
		}

		/// <summary>
		/// If holding a point, move it to a new position (keeping the same offset from mouse). 
		/// Don't let the point go outside the window.
		/// </summary>
		public void MouseMove(Vector2 mouseCoord)
		{
			if (CurrentPoint != null)
			{
				Vector2 newPosition = mouseCoord + DraggingOffset;

				double scale = Window.ScreenDistance(new Vector2(), new Vector2(1, 0));

				double maxDistance = PointEdgeLimit / scale;


				CurrentPoint.Position = new Vector2(
					Clip(newPosition.X, maxDistance, Window.Width - maxDistance - 1),
					Clip(newPosition.Y, maxDistance, Window.Height - maxDistance - 1)
				);
			}
		}

		/// <summary>
		/// Select a point that is closest to mouse and at most MaxMouseDistance away (in pixels).
		/// </summary>
		public void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			if (button == MouseButton.Left)
			{
				IPoint best = null;
				double bestDistance = double.MaxValue;

				foreach (IPoint p in Points)
				{
					double distance = Window.ScreenDistance(mouseCoord, p.Position);
					if (distance < MaxMouseDistance && distance < bestDistance)
					{
						best = p;
						bestDistance = distance;
					}
				}

				if (best != null)
				{
					DraggingOffset = best.Position - mouseCoord;
					CurrentPoint = best;
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
