using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PhotomatchCore.Logic.Perspective
{
	/// <summary>
	/// First and second calibration axis written as [first][second].
	/// </summary>
	public enum CalibrationAxes { XY, YX, XZ, ZX, YZ, ZY };

	/// <summary>
	/// Struct containing information about X/Y/Z axes being inverted.
	/// </summary>
	public struct InvertedAxes : ISafeSerializable<InvertedAxes>
	{
		/// <summary>
		/// True if X is inverted. False otherwise.
		/// </summary>
		public bool X;

		/// <summary>
		/// True if Y is inverted. False otherwise.
		/// </summary>
		public bool Y;

		/// <summary>
		/// True if Z is inverted. False otherwise.
		/// </summary>
		public bool Z;

		public void Deserialize(BinaryReader reader)
		{
			X = reader.ReadBoolean();
			Y = reader.ReadBoolean();
			Z = reader.ReadBoolean();
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(X);
			writer.Write(Y);
			writer.Write(Z);
		}
	}
}
