using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using MatrixVector;

namespace WpfExtensions
{
	static class PointExtensions
	{
		/// <summary>
		/// Convert Point to Vector2.
		/// </summary>
		public static Vector2 AsVector2(this Point p, double dpiScale = 1) => new Vector2(p.X, p.Y) * dpiScale;
	}

	static class Vector2Extensions
	{
		/// <summary>
		/// Convert Vector2 to Point. No validity checks performed.
		/// </summary>
		public static Point AsPoint(this Vector2 v, double dpiScale = 1) => new Point(v.X / dpiScale, v.Y / dpiScale);
	}
}
