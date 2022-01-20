using System;
using System.IO;

using Photomatch_ProofOfConcept_WPF.Utilities;

namespace Photomatch_ProofOfConcept_WPF.Logic
{
	public struct Matrix3x3 : ISafeSerializable<Matrix3x3>
	{
		public double A00 { get; set; }
		public double A01 { get; set; }
		public double A02 { get; set; }
		public double A10 { get; set; }
		public double A11 { get; set; }
		public double A12 { get; set; }
		public double A20 { get; set; }
		public double A21 { get; set; }
		public double A22 { get; set; }

		public Vector3 A0_
		{
			get => new Vector3(A00, A01, A02);
			set
			{
				A00 = value.X;
				A01 = value.Y;
				A02 = value.Z;
			}
		}

		public Vector3 A1_
		{
			get => new Vector3(A10, A11, A12);
			set
			{
				A10 = value.X;
				A11 = value.Y;
				A12 = value.Z;
			}
		}

		public Vector3 A2_
		{
			get => new Vector3(A20, A21, A22);
			set
			{
				A20 = value.X;
				A21 = value.Y;
				A22 = value.Z;
			}
		}

		public Vector3 A_0
		{
			get => new Vector3(A00, A10, A20);
			set
			{
				A00 = value.X;
				A10 = value.Y;
				A20 = value.Z;
			}
		}

		public Vector3 A_1
		{
			get => new Vector3(A01, A11, A21);
			set
			{
				A01 = value.X;
				A11 = value.Y;
				A21 = value.Z;
			}
		}

		public Vector3 A_2
		{
			get => new Vector3(A02, A12, A22);
			set
			{
				A02 = value.X;
				A12 = value.Y;
				A22 = value.Z;
			}
		}

		public static Matrix3x3 CreateUnitMatrix()
		{
			return new Matrix3x3() { A00 = 1, A11 = 1, A22 = 1 };
		}

		public static Vector3 operator *(Matrix3x3 matrix, Vector3 vector)
		{
			Vector3 result = new Vector3();

			result.X = matrix.A00 * vector.X + matrix.A01 * vector.Y + matrix.A02 * vector.Z;
			result.Y = matrix.A10 * vector.X + matrix.A11 * vector.Y + matrix.A12 * vector.Z;
			result.Z = matrix.A20 * vector.X + matrix.A21 * vector.Y + matrix.A22 * vector.Z;

			return result;
		}

		public static Matrix3x3 operator *(Matrix3x3 matrixA, Matrix3x3 matrixB)
		{
			Matrix3x3 result = new Matrix3x3();

			result.A00 = matrixA.A00 * matrixB.A00 + matrixA.A01 * matrixB.A10 + matrixA.A02 * matrixB.A20;
			result.A10 = matrixA.A10 * matrixB.A00 + matrixA.A11 * matrixB.A10 + matrixA.A12 * matrixB.A20;
			result.A20 = matrixA.A20 * matrixB.A00 + matrixA.A21 * matrixB.A10 + matrixA.A22 * matrixB.A20;

			result.A01 = matrixA.A00 * matrixB.A01 + matrixA.A01 * matrixB.A11 + matrixA.A02 * matrixB.A21;
			result.A11 = matrixA.A10 * matrixB.A01 + matrixA.A11 * matrixB.A11 + matrixA.A12 * matrixB.A21;
			result.A21 = matrixA.A20 * matrixB.A01 + matrixA.A21 * matrixB.A11 + matrixA.A22 * matrixB.A21;

			result.A02 = matrixA.A00 * matrixB.A02 + matrixA.A01 * matrixB.A12 + matrixA.A02 * matrixB.A22;
			result.A12 = matrixA.A10 * matrixB.A02 + matrixA.A11 * matrixB.A12 + matrixA.A12 * matrixB.A22;
			result.A22 = matrixA.A20 * matrixB.A02 + matrixA.A21 * matrixB.A12 + matrixA.A22 * matrixB.A22;

			return result;
		}

		public Matrix3x3 Transposed()
		{
			Matrix3x3 result = new Matrix3x3();

			result.A00 = A00;
			result.A01 = A10;
			result.A02 = A20;

			result.A10 = A01;
			result.A11 = A11;
			result.A12 = A21;

			result.A20 = A02;
			result.A21 = A12;
			result.A22 = A22;

			return result;
		}

		public Matrix3x3 Adjugate()
		{
			return new Matrix3x3()
			{
				A00 = A11 * A22 - A12 * A21,
				A01 = A21 * A02 - A01 * A22,
				A02 = A01 * A12 - A02 * A11,
				A10 = A20 * A12 - A10 * A22,
				A11 = A00 * A22 - A20 * A02,
				A12 = A10 * A02 - A00 * A12,
				A20 = A10 * A21 - A20 * A11,
				A21 = A20 * A01 - A00 * A21,
				A22 = A00 * A11 - A10 * A01
			};
		}

		public void AddToRow(int row, Vector3 vector)
		{
			switch(row)
			{
				case 0:
					A00 += vector.X;
					A01 += vector.Y;
					A02 += vector.Z;
					break;
				case 1:
					A10 += vector.X;
					A11 += vector.Y;
					A12 += vector.Z;
					break;
				case 2:
					A20 += vector.X;
					A21 += vector.Y;
					A22 += vector.Z;
					break;
				default:
					throw new Exception("Unknown switch case.");
			}
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(A00);
			writer.Write(A01);
			writer.Write(A02);
			writer.Write(A10);
			writer.Write(A11);
			writer.Write(A12);
			writer.Write(A20);
			writer.Write(A21);
			writer.Write(A22);
		}

		public void Deserialize(BinaryReader reader)
		{
			A00 = reader.ReadDouble();
			A01 = reader.ReadDouble();
			A02 = reader.ReadDouble();
			A10 = reader.ReadDouble();
			A11 = reader.ReadDouble();
			A12 = reader.ReadDouble();
			A20 = reader.ReadDouble();
			A21 = reader.ReadDouble();
			A22 = reader.ReadDouble();
		}
	}

	public struct Vector3 : ISafeSerializable<Vector3>
	{
		public Vector3(double X, double Y, double Z)
		{
			this.X = X;
			this.Y = Y;
			this.Z = Z;
		}

		public double X { get; set; }
		public double Y { get; set; }
		public double Z { get; set; }

		public double MagnitudeSquared => X * X + Y * Y + Z * Z;

		public double Magnitude => Math.Sqrt(MagnitudeSquared);

		public bool Valid => !(double.IsNaN(X) || double.IsNaN(Y) || double.IsNaN(Z));

		public Vector3 Normalized()
		{
			double mag = this.Magnitude;
			return new Vector3() { X = this.X / mag, Y = this.Y / mag, Z = this.Z / mag };
		}

		public Vector3 WithX(double x) => new Vector3() { X = x, Y = Y, Z = Z };
		public Vector3 WithY(double y) => new Vector3() { X = X, Y = y, Z = Z };
		public Vector3 WithZ(double z) => new Vector3() { X = X, Y = Y, Z = z };

		public static Vector3 operator +(Vector3 first, Vector3 second)
		{
			return new Vector3() { X = first.X + second.X, Y = first.Y + second.Y, Z = first.Z + second.Z };
		}

		public static Vector3 operator -(Vector3 first, Vector3 second)
		{
			return new Vector3() { X = first.X - second.X, Y = first.Y - second.Y, Z = first.Z - second.Z };
		}

		public static Vector3 operator -(Vector3 vector)
		{
			return new Vector3() { X = -vector.X, Y = -vector.Y, Z = -vector.Z };
		}

		public static Vector3 operator /(Vector3 vector, double value)
		{
			return new Vector3() { X = vector.X / value, Y = vector.Y / value, Z = vector.Z / value };
		}

		public static Vector3 operator *(Vector3 vector, double value)
		{
			return new Vector3() { X = vector.X * value, Y = vector.Y * value, Z = vector.Z * value };
		}

		public static Vector3 operator *(double value, Vector3 vector) => vector * value;

		public static Vector3 Cross(Vector3 first, Vector3 second)
		{
			return new Vector3(first.Y * second.Z - first.Z * second.Y, first.Z * second.X - first.X * second.Z, first.X * second.Y - first.Y * second.X);
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(X);
			writer.Write(Y);
			writer.Write(Z);
		}

		public void Deserialize(BinaryReader reader)
		{
			X = reader.ReadDouble();
			Y = reader.ReadDouble();
			Z = reader.ReadDouble();
		}

		public override string ToString()
		{
			return $"({X}, {Y}, {Z})";
		}

		internal static double Dot(Vector3 first, Vector3 second)
		{
			return first.X * second.X + first.Y * second.Y + first.Z * second.Z;
		}
	}

	public struct Vector2 : ISafeSerializable<Vector2>
	{
		public static Vector2 InvalidInstance = new Vector2(double.NaN, double.NaN);

		public Vector2(double X, double Y)
		{
			this.X = X;
			this.Y = Y;
		}

		public double X { get; set; }
		public double Y { get; set; }

		public double MagnitudeSquared => X * X + Y * Y;

		public double Magnitude => Math.Sqrt(MagnitudeSquared);

		public bool Valid => !(double.IsNaN(X) || double.IsNaN(Y));

		public Vector2 Normalized()
		{
			double mag = this.Magnitude;
			return new Vector2() { X = this.X / mag, Y = this.Y / mag };
		}

		public static Vector2 operator +(Vector2 first, Vector2 second)
		{
			return new Vector2() { X = first.X + second.X, Y = first.Y + second.Y };
		}

		public static Vector2 operator -(Vector2 first, Vector2 second)
		{
			return new Vector2() { X = first.X - second.X, Y = first.Y - second.Y };
		}

		public static Vector2 operator /(Vector2 vector, double value)
		{
			return new Vector2() { X = vector.X / value, Y = vector.Y / value };
		}

		public static Vector2 operator *(Vector2 vector, double value)
		{
			return new Vector2() { X = vector.X * value, Y = vector.Y * value };
		}

		public static Vector2 operator *(double value, Vector2 vector) => vector * value;

		public static double Dot(Vector2 first, Vector2 second)
		{
			return first.X * second.X + first.Y * second.Y;
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(X);
			writer.Write(Y);
		}

		public void Deserialize(BinaryReader reader)
		{
			X = reader.ReadDouble();
			Y = reader.ReadDouble();
		}

		public override string ToString()
		{
			return $"({X}, {Y})";
		}
	}
}