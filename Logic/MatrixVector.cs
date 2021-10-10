using System;

namespace MatrixVector
{
	public struct Matrix3x3
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
	}

	public struct Vector3
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

		public static Vector3 operator +(Vector3 first, Vector3 second)
		{
			return new Vector3() { X = first.X + second.X, Y = first.Y + second.Y, Z = first.Z + second.Z };
		}

		public static Vector3 operator -(Vector3 first, Vector3 second)
		{
			return new Vector3() { X = first.X - second.X, Y = first.Y - second.Y, Z = first.Z - second.Z };
		}

		public static Vector3 operator /(Vector3 vector, double value)
		{
			return new Vector3() { X = vector.X / value, Y = vector.Y / value, Z = vector.Z / value };
		}
	}

	public struct Vector2
	{
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
	}
}