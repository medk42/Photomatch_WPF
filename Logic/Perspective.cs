using MatrixVector;
using System;

namespace Perspective
{
	class Camera
	{
		private Matrix3x3 projectionInverse = Matrix3x3.CreateUnitMatrix();

		private Matrix3x3 intrinsicMatrix;
		private Matrix3x3 rotationMatrix;

		private Matrix3x3 intrinsicMatrixInverse;
		private Matrix3x3 rotationMatrixInverse;

		private Vector3 translate;

		public void UpdateView(double viewRatio, Vector2 principalPoint, Vector2 firstVanishingPoint, Vector2 secondVanishingPoint, Vector2 origin)
		{
			double scale = GetInstrinsicParametersScale(principalPoint, viewRatio, firstVanishingPoint, secondVanishingPoint);
			intrinsicMatrix = GetIntrinsicParametersMatrix(principalPoint, scale, viewRatio);
			intrinsicMatrixInverse = GetInvertedIntrinsicParametersMatrix(principalPoint, scale, viewRatio);
			rotationMatrix = GetRotationalMatrix(intrinsicMatrixInverse, firstVanishingPoint, secondVanishingPoint);
			rotationMatrixInverse = rotationMatrix.Transposed();

			translate = intrinsicMatrixInverse * new Vector3(origin.X, origin.Y, 1);
		}

		public Vector2 WorldToScreen(Vector3 worldPoint)
		{
			Vector3 point = rotationMatrix * worldPoint + translate;
			point = point / point.Z;
			point = intrinsicMatrix * point;
			return new Vector2(point.X, point.Y);
		}

		public Vector3 ScreenToWorld(Vector2 screenPoint)
		{
			return rotationMatrixInverse * (intrinsicMatrixInverse * new Vector3(screenPoint.X, screenPoint.Y, 1) - translate);
		}

		public static double GetInstrinsicParametersScale(Vector2 principalPoint, double viewRatio, Vector2 firstVanishingPoint, Vector2 secondVanishingPoint)
		{
			return Math.Sqrt(
				-(principalPoint.X * principalPoint.X)
				+ firstVanishingPoint.X * principalPoint.X
				+ secondVanishingPoint.X * principalPoint.X
				- firstVanishingPoint.X * secondVanishingPoint.X
				+ (
					-(principalPoint.Y * principalPoint.Y)
					+ firstVanishingPoint.Y * principalPoint.Y
					+ secondVanishingPoint.Y * principalPoint.Y
					- firstVanishingPoint.Y * secondVanishingPoint.Y
				) / (viewRatio * viewRatio));
		}

		public static Matrix3x3 GetIntrinsicParametersMatrix(Vector2 principalPoint, double scale, double viewRatio)
		{
			Matrix3x3 intrinsicMatrix = new Matrix3x3();

			intrinsicMatrix.A00 = scale;
			intrinsicMatrix.A11 = scale * viewRatio;
			intrinsicMatrix.A22 = 1;
			intrinsicMatrix.A02 = principalPoint.X;
			intrinsicMatrix.A12 = principalPoint.Y;

			return intrinsicMatrix;
		}

		public static Matrix3x3 GetInvertedIntrinsicParametersMatrix(Vector2 principalPoint, double scale, double viewRatio)
		{
			Matrix3x3 intrinsicMatrixInv = new Matrix3x3();

			double scaleInv = 1 / scale;
			double viewRationInv = 1 / viewRatio;

			intrinsicMatrixInv.A00 = scaleInv;
			intrinsicMatrixInv.A11 = scaleInv * viewRationInv;
			intrinsicMatrixInv.A22 = 1;
			intrinsicMatrixInv.A02 = -principalPoint.X * scaleInv;
			intrinsicMatrixInv.A12 = -principalPoint.Y * scaleInv * viewRationInv;

			return intrinsicMatrixInv;
		}

		public static Matrix3x3 GetRotationalMatrix(Matrix3x3 invertedIntrinsicMatrix, Vector2 firstVanishingPoint, Vector2 secondVanishingPoint)
		{
			Matrix3x3 rotationMatrix = new Matrix3x3();

			Vector3 firstCol = (invertedIntrinsicMatrix * new Vector3(firstVanishingPoint.X, firstVanishingPoint.Y, 1)).Normalized();
			Vector3 secondCol = (invertedIntrinsicMatrix * new Vector3(secondVanishingPoint.X, secondVanishingPoint.Y, 1)).Normalized();

			rotationMatrix.A00 = firstCol.X;
			rotationMatrix.A10 = firstCol.Y;
			rotationMatrix.A20 = firstCol.Z;

			rotationMatrix.A01 = secondCol.X;
			rotationMatrix.A11 = secondCol.Y;
			rotationMatrix.A21 = secondCol.Z;

			rotationMatrix.A02 = -Math.Sqrt(1 - firstCol.X * firstCol.X - secondCol.X * secondCol.X);
			rotationMatrix.A12 = Math.Sqrt(1 - firstCol.Y * firstCol.Y - secondCol.Y * secondCol.Y);
			rotationMatrix.A22 = -Math.Sqrt(1 - firstCol.Z * firstCol.Z - secondCol.Z * secondCol.Z);

			return rotationMatrix;
		}
	}
}