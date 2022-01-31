using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Logic
{
	public static class ImageSharpExtension
	{
		public static Vector3 AsVector3(this Rgb24 color) => new Vector3(color.R, color.G, color.B);
	}
}
