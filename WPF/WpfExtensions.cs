using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using Photomatch_ProofOfConcept_WPF.Logic;

namespace Photomatch_ProofOfConcept_WPF.WPF
{
	static class PointExtensions
	{
		/// <summary>
		/// Convert Point to Vector2.
		/// </summary>
		public static Vector2 AsVector2(this Point p) => new Vector2(p.X, p.Y);
	}

	static class Vector2Extensions
	{
		/// <summary>
		/// Convert Vector2 to Point. No validity checks performed.
		/// </summary>
		public static Point AsPoint(this Vector2 v) => new Point(v.X, v.Y);
	}
}
