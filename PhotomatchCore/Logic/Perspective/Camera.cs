using System.IO;
using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using PhotomatchCore.Utilities;

namespace PhotomatchCore.Logic.Perspective
{
	/// <summary>
	/// Class containing data about world-to-screen and screen-to-world transformations.
	/// </summary>
	public class Camera
	{
		private Matrix3x3 IntrinsicMatrix;
		private Matrix3x3 RotationMatrix;

		private Matrix3x3 IntrinsicMatrixInverse;
		private Matrix3x3 RotationMatrixInverse;

		private Vector3 Translate;

		private double Scale;

		/// <summary>
		/// Update transformation matrices based on calibration variables.
		/// </summary>
		/// <param name="viewRatio">Ratio between x and y coordinates.</param>
		/// <param name="principalPoint">Principal point of the image.</param>
		/// <param name="vanishingPointA">Vanishing point of the first axis.</param>
		/// <param name="vanishingPointB">Vanishing point of the second axis.</param>
		/// <param name="origin">World origin position on the image.</param>
		/// <param name="axes">Specification of the first and second axis.</param>
		/// <param name="inverted">Specification of axis inversion.</param>
		public void UpdateView(double viewRatio, Vector2 principalPoint, Vector2 vanishingPointA, Vector2 vanishingPointB, Vector2 origin, CalibrationAxes axes, InvertedAxes inverted)
		{
			double scale = GetInstrinsicParametersScale(principalPoint, viewRatio, vanishingPointA, vanishingPointB);
			IntrinsicMatrix = GetIntrinsicParametersMatrix(principalPoint, scale, viewRatio);
			IntrinsicMatrixInverse = GetInvertedIntrinsicParametersMatrix(principalPoint, scale, viewRatio);
			RotationMatrix = GetRotationalMatrix(IntrinsicMatrixInverse, vanishingPointA, vanishingPointB, principalPoint, axes, inverted);
			RotationMatrixInverse = RotationMatrix.Transposed();

			Translate = IntrinsicMatrixInverse * new Vector3(origin.X, origin.Y, 1);
		}

		/// <summary>
		/// Update model scale to a new value.
		/// </summary>
		public void UpdateScale(double newScale) => Scale = newScale;

		/// <summary>
		/// Transform vector from world space to screen space (with z normalized).
		/// </summary>
		public Vector2 WorldToScreen(Vector3 worldPoint)
		{
			Vector3 point = RotationMatrix * worldPoint * Scale + Translate;
			point = point / point.Z;
			point = IntrinsicMatrix * point;
			return new Vector2(point.X, point.Y);
		}

		/// <summary>
		/// Transform vector from screen space (with 1 as z coordinate) to world space.
		/// </summary>
		public Vector3 ScreenToWorld(Vector2 screenPoint)
		{
			return RotationMatrixInverse * (IntrinsicMatrixInverse * new Vector3(screenPoint.X, screenPoint.Y, 1) - Translate) / Scale;
		}

		/// <summary>
		/// Create a ray in world space from Vector3(screenPoint, 1) in the direction 
		/// of the view of the camera at that point. 
		/// </summary>
		public Ray3D ScreenToWorldRay(Vector2 screenPoint)
		{
			Vector3 origin = RotationMatrixInverse * (IntrinsicMatrixInverse * new Vector3(screenPoint.X, screenPoint.Y, 1) - Translate) / Scale;
			Vector3 behindOrigin = RotationMatrixInverse * (IntrinsicMatrixInverse * new Vector3(screenPoint.X, screenPoint.Y, 1) * 2 - Translate) / Scale;
			return new Ray3D(origin, behindOrigin - origin);
		}

		/// <summary>
		/// Calculate perspective origin screen position, based on transformation matrices and scale, 
		/// so that the WorldToScreen projection of the worldPoint is at screenPoint.
		/// </summary>
		public Vector2 MatchScreenWorldPoint(Vector2 screenPoint, Vector3 worldPoint)
		{
			Vector3 rightHandSide = IntrinsicMatrix * (RotationMatrix * (Scale * worldPoint)) + new Vector3(0, 0, 1);
			return new Vector2(screenPoint.X * rightHandSide.Z - rightHandSide.X, screenPoint.Y * rightHandSide.Z - rightHandSide.Y);
		}

		/// <summary>
		/// Calculate perspective origin screen position and scale, based on transformation matrices,
		/// so that the WorldToScreen projection of the worldPointPos is at screenPointPos and
		/// the WorldToScreen projection of worldPointScale is closest to screenPointScale.
		/// </summary>
		/// <returns>
		/// Vector3 containing the calculated perspective origin as x and y coordinates and 
		/// the calculated scale as the z coordinate.
		/// </returns>
		public Vector3 MatchScreenWorldPoints(Vector2 screenPointPos, Vector3 worldPointPos, Vector2 screenPointScale, Vector3 worldPointScale)
		{
			Vector3 a = IntrinsicMatrix * RotationMatrix * worldPointPos;
			Vector2 s = screenPointPos;
			Vector3 a2 = IntrinsicMatrix * RotationMatrix * worldPointScale;
			Vector2 ps = Intersections2D.ProjectVectorToRay(screenPointScale, new Line2D(screenPointPos, WorldToScreen(worldPointScale)).AsRay()).Projection;

			double S = (s.X - ps.X) / (a.X - s.X * a.Z - a2.X + ps.X * a2.Z);

			double X = s.X + (s.X * a.Z - a.X) * S;
			double Y = s.Y + (s.Y * a.Z - a.Y) * S;

			return new Vector3(X, Y, S);
		}

		/// <summary>
		/// Calculate the scale of the intrinsic camera matrix.
		/// </summary>
		/// <param name="principalPoint">Principal point of the image.</param>
		/// <param name="viewRatio">Ratio between x and y coordinates.</param>
		/// <param name="firstVanishingPoint">Vanishing point of the first axis.</param>
		/// <param name="secondVanishingPoint">Vanishing point of the second axis.</param>
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

		/// <summary>
		/// Get the intrinsic camera matrix.
		/// </summary>
		/// <param name="principalPoint">Principal point of the image.</param>
		/// <param name="scale">The scale of the intrinsic camera matrix.</param>
		/// <param name="viewRatio">Ratio between x and y coordinates.</param>
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

		/// <summary>
		/// Get the inverted intrinsic camera matrix.
		/// </summary>
		/// <param name="principalPoint">Principal point of the image.</param>
		/// <param name="scale">The scale of the intrinsic camera matrix.</param>
		/// <param name="viewRatio">Ratio between x and y coordinates.</param>
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

		/// <summary>
		/// Calculate the rotational camera matrix.
		/// </summary>
		/// <param name="invertedIntrinsicMatrix">The inverted intrinsic camera matrix.</param>
		/// <param name="vanishingPointA">Vanishing point of the first axis.</param>
		/// <param name="vanishingPointB">Vanishing point of the second axis.</param>
		/// <param name="principalPoint">Principal point of the image.</param>
		/// <param name="axes">Specification of the first and second axis.</param>
		/// <param name="inverted">Specification of axis inversion.</param>
		/// <returns></returns>
		public static Matrix3x3 GetRotationalMatrix(Matrix3x3 invertedIntrinsicMatrix, Vector2 vanishingPointA, Vector2 vanishingPointB, Vector2 principalPoint, CalibrationAxes axes, InvertedAxes inverted)
		{
			Matrix3x3 rotationMatrix = new Matrix3x3();

			Vector3 firstCol, secondCol, thirdCol;

			Vector3 colA = (invertedIntrinsicMatrix * new Vector3(vanishingPointA.X, vanishingPointA.Y, 1)).Normalized();
			Vector3 colB = (invertedIntrinsicMatrix * new Vector3(vanishingPointB.X, vanishingPointB.Y, 1)).Normalized();

			switch (axes)
			{
				case CalibrationAxes.XY:
					firstCol = inverted.X ? -colA : colA;
					secondCol = inverted.Y ? -colB : colB;
					thirdCol = Vector3.Cross(firstCol, secondCol);
					break;
				case CalibrationAxes.YX:
					firstCol = inverted.X ? -colB : colB;
					secondCol = inverted.Y ? -colA : colA;
					thirdCol = Vector3.Cross(firstCol, secondCol);
					break;
				case CalibrationAxes.XZ:
					firstCol = inverted.X ? -colA : colA;
					thirdCol = inverted.Z ? -colB : colB;
					secondCol = Vector3.Cross(thirdCol, firstCol);
					break;
				case CalibrationAxes.ZX:
					firstCol = inverted.X ? -colB : colB;
					thirdCol = inverted.Z ? -colA : colA;
					secondCol = Vector3.Cross(thirdCol, firstCol);
					break;
				case CalibrationAxes.YZ:
					secondCol = inverted.Y ? -colA : colA;
					thirdCol = inverted.Z ? -colB : colB;
					firstCol = Vector3.Cross(secondCol, thirdCol);
					break;
				case CalibrationAxes.ZY:
					secondCol = inverted.Y ? -colB : colB;
					thirdCol = inverted.Z ? -colA : colA;
					firstCol = Vector3.Cross(secondCol, thirdCol);
					break;
				default:
					throw new Exception("Unexpected switch case.");
			}

			rotationMatrix.A00 = firstCol.X;
			rotationMatrix.A10 = firstCol.Y;
			rotationMatrix.A20 = firstCol.Z;

			rotationMatrix.A01 = secondCol.X;
			rotationMatrix.A11 = secondCol.Y;
			rotationMatrix.A21 = secondCol.Z;

			rotationMatrix.A02 = thirdCol.X;
			rotationMatrix.A12 = thirdCol.Y;
			rotationMatrix.A22 = thirdCol.Z;

			return rotationMatrix;
		}

		/// <summary>
		/// Calculate the third triangle vertex from the other two and an orthocenter.
		/// </summary>
		private static Vector2 GetTriangleThirdVertexFromOrthocenter(Vector2 firstVertex, Vector2 secondVertex, Vector2 orthocenter)
		{
			Vector2 firstToSecond = secondVertex - firstVertex;
			Vector2 firstToOrtho = orthocenter - firstVertex;
			Vector2 orthoProj = firstVertex + Vector2.Dot(firstToOrtho, firstToSecond) / Vector2.Dot(firstToSecond, firstToSecond) * firstToSecond;

			Vector2 ortoProjToOrto = orthocenter - orthoProj;
			Vector2 ortoProjToFirst = firstVertex - orthoProj;
			Vector2 secondToOrtho = orthocenter - secondVertex;
			Vector2 thirdVertex = orthoProj + Vector2.Dot(ortoProjToFirst, secondToOrtho) / Vector2.Dot(ortoProjToOrto, secondToOrtho) * ortoProjToOrto;

			return thirdVertex;
		}


		/// <summary>
		/// Create a rotational matrix so that vector v1 is projected to vector v2. (source https://gist.github.com/kevinmoran/b45980723e53edeb8a5a43c49f134724)
		/// </summary>
		/// <param name="v1">A unit vector to be projected.</param>
		/// <param name="v2">A unit vector.</param>
		/// <returns>Rotational matrix.</returns>
		public static Matrix3x3 RotateAlign(Vector3 v1, Vector3 v2)
		{
			Vector3 axis = Vector3.Cross(v1, v2);

			double cosA = Vector3.Dot(v1, v2);
			double k = 1.0 / (1.0 + cosA);

			if (cosA == -1.0)
			{
				Vector3 mid = v1.X >= 0.5 || v1.X <= -0.5 ? (v1 + new Vector3(0, 0.5, 0)).Normalized() : (v1 + new Vector3(0.5, 0, 0)).Normalized();
				return RotateAlign(mid, v2) * RotateAlign(v1, mid);
			}

			return new Matrix3x3()
			{
				A0_ = new Vector3(axis.X * axis.X * k + cosA, axis.Y * axis.X * k - axis.Z, axis.Z * axis.X * k + axis.Y),
				A1_ = new Vector3(axis.X * axis.Y * k + axis.Z, axis.Y * axis.Y * k + cosA, axis.Z * axis.Y * k - axis.X),
				A2_ = new Vector3(axis.X * axis.Z * k - axis.Y, axis.Y * axis.Z * k + axis.X, axis.Z * axis.Z * k + cosA)
			};
		}
	}
}