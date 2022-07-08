using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Utilities
{
	/// <summary>
	/// Extension methods for ImageSharp library.
	/// </summary>
	public static class ImageSharpExtension
	{
		/// <summary>
		/// Convert color from Rgb24 struct to Vector3.
		/// </summary>
		/// <param name="color">color to convert</param>
		/// <returns>Vector3 with the same values as color (X=R, Y=G, Z=B).</returns>
		public static Vector3 AsVector3(this Rgb24 color) => new Vector3(color.R, color.G, color.B);
	}
}
