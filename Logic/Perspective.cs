using System.IO;
using System;
using SixLabors.ImageSharp;

using Photomatch_ProofOfConcept_WPF.Utilities;

namespace Photomatch_ProofOfConcept_WPF.Logic
{
	public enum CalibrationAxes { XY, YX, XZ, ZX, YZ, ZY };

	public struct InvertedAxes : ISafeSerializable<InvertedAxes>
	{
		public bool X;
		public bool Y;
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

	public class PerspectiveData : ISafeSerializable<PerspectiveData>
	{
		public Image Image { get; private set; }

		private Camera _camera = new Camera();
		private Vector2 _origin;
		private Line2D _lineA1 = new Line2D(new Vector2(0.52, 0.19), new Vector2(0.76, 0.28));
		private Line2D _lineA2 = new Line2D(new Vector2(0.35, 0.67), new Vector2(0.46, 0.82));
		private Line2D _lineB1 = new Line2D(new Vector2(0.27, 0.31), new Vector2(0.48, 0.21));
		private Line2D _lineB2 = new Line2D(new Vector2(0.55, 0.78), new Vector2(0.71, 0.68));
		private CalibrationAxes _calibrationAxes = CalibrationAxes.XY;
		private InvertedAxes _invertedAxes;
		private double _scale = 1;

		public Vector2 Origin
		{
			get => _origin;
			set
			{
				_origin = value;
				RecalculateProjection();
			}
		}

		public Line2D LineA1
		{
			get => _lineA1;
			set
			{
				_lineA1 = value;
				RecalculateProjection();
			}
		}

		public Line2D LineA2
		{
			get => _lineA2;
			set
			{
				_lineA2 = value;
				RecalculateProjection();
			}
		}

		public Line2D LineB1
		{
			get => _lineB1;
			set
			{
				_lineB1 = value;
				RecalculateProjection();
			}
		}

		public Line2D LineB2
		{
			get => _lineB2;
			set
			{
				_lineB2 = value;
				RecalculateProjection();
			}
		}

		public CalibrationAxes CalibrationAxes
		{
			get => _calibrationAxes;
			set
			{
				if (_calibrationAxes != value)
				{
					_calibrationAxes = value;
					RecalculateProjection();
				}
			}
		}

		public InvertedAxes InvertedAxes
		{
			get => _invertedAxes;
			set
			{
				_invertedAxes = value;
				RecalculateProjection();
			}
		}

		public double Scale
		{
			get => _scale;
			set
			{
				_scale = value;
				_camera.UpdateScale(value);
			}
		}

		public string ImagePath { get; private set; }

		public PerspectiveData(Image image, string imagePath)
		{
			Image = image;
			ImagePath = imagePath;

			LineA1 = ScaleLine(LineA1, image.Width, image.Height);
			LineA2 = ScaleLine(LineA2, image.Width, image.Height);
			LineB1 = ScaleLine(LineB1, image.Width, image.Height);
			LineB2 = ScaleLine(LineB2, image.Width, image.Height);

			Origin = new Vector2(image.Width / 2, image.Height / 2);

			RecalculateProjection();
		}

		/// <summary>
		/// Only for deserialization!
		/// </summary>
		public PerspectiveData() { }

		public void Serialize(BinaryWriter writer)
		{
			byte[] imageData = null;
			using (var stream = new MemoryStream())
			{
				Image.SaveAsPng(stream);
				imageData = stream.ToArray();
			}

			if (imageData == null)
				throw new Exception("Image serialization failed.");

			writer.Write(imageData.Length);
			writer.Write(imageData);

			writer.Write(ImagePath);

			_origin.Serialize(writer);
			_lineA1.Serialize(writer);
			_lineA2.Serialize(writer);
			_lineB1.Serialize(writer);
			_lineB2.Serialize(writer);

			writer.Write(_scale);

			writer.Write((int)_calibrationAxes);
			_invertedAxes.Serialize(writer);
		}

		public void Deserialize(BinaryReader reader)
		{
			int imageDataLength = reader.ReadInt32();
			byte[] imageData = reader.ReadBytes(imageDataLength);
			using (var stream = new MemoryStream(imageData))
			{
				Image = Image.Load(stream);
			}

			ImagePath = reader.ReadString();

			_origin = ISafeSerializable<Vector2>.CreateDeserialize(reader);
			_lineA1 = ISafeSerializable<Line2D>.CreateDeserialize(reader);
			_lineA2 = ISafeSerializable<Line2D>.CreateDeserialize(reader);
			_lineB1 = ISafeSerializable<Line2D>.CreateDeserialize(reader);
			_lineB2 = ISafeSerializable<Line2D>.CreateDeserialize(reader);

			_scale = reader.ReadDouble();

			_calibrationAxes = (CalibrationAxes)reader.ReadInt32();
			_invertedAxes = ISafeSerializable<InvertedAxes>.CreateDeserialize(reader);

			RecalculateProjection();
		}

		private Line2D ScaleLine(Line2D line, double xStretch, double yStretch)
		{
			var newStart = new Vector2(line.Start.X * xStretch, line.Start.Y * yStretch);
			var newEnd = new Vector2(line.End.X * xStretch, line.End.Y * yStretch);
			return new Line2D(newStart, newEnd);
		}

		public void RecalculateProjection()
		{
			Vector2 vanishingPointA = Intersections2D.GetLineLineIntersection(LineA1, LineA2).Intersection;
			Vector2 vanishingPointB = Intersections2D.GetLineLineIntersection(LineB1, LineB2).Intersection;
			Vector2 principalPoint = new Vector2(Image.Width / 2, Image.Height / 2);
			double viewRatio = 1;

			_camera.UpdateView(viewRatio, principalPoint, vanishingPointA, vanishingPointB, Origin, CalibrationAxes, InvertedAxes);
			_camera.UpdateScale(Scale);
		}

		public Vector3 ScreenToWorld(Vector2 point) => _camera.ScreenToWorld(point);

		public Vector2 WorldToScreen(Vector3 point) => _camera.WorldToScreen(point);

		public Ray3D ScreenToWorldRay(Vector2 screenPoint) => _camera.ScreenToWorldRay(screenPoint);

		public Vector2 MatchScreenWorldPoint(Vector2 screenPoint, Vector3 worldPoint) => _camera.MatchScreenWorldPoint(screenPoint, worldPoint);

		public Vector3 MatchScreenWorldPoints(Vector2 screenPointPos, Vector3 worldPointPos, Vector2 screenPointScale, Vector3 worldPointScale) => _camera.MatchScreenWorldPoints(screenPointPos, worldPointPos, screenPointScale, worldPointScale);

		public Vector2 GetXDirAt(Vector2 screenPoint)
		{
			Vector2 screenPointMoved = _camera.WorldToScreen(_camera.ScreenToWorld(screenPoint) + new Vector3(1, 0, 0));
			Vector2 direction = (screenPointMoved - screenPoint).Normalized();
			return direction;
		}

		public Vector2 GetYDirAt(Vector2 screenPoint)
		{
			Vector2 screenPointMoved = _camera.WorldToScreen(_camera.ScreenToWorld(screenPoint) + new Vector3(0, 1, 0));
			Vector2 direction = (screenPointMoved - screenPoint).Normalized();
			return direction;
		}

		public Vector2 GetZDirAt(Vector2 screenPoint)
		{
			Vector2 screenPointMoved = _camera.WorldToScreen(_camera.ScreenToWorld(screenPoint) + new Vector3(0, 0, 1));
			Vector2 direction = (screenPointMoved - screenPoint).Normalized();
			return direction;
		}
	}

	public class Camera
	{
		private Matrix3x3 IntrinsicMatrix;
		private Matrix3x3 RotationMatrix;

		private Matrix3x3 IntrinsicMatrixInverse;
		private Matrix3x3 RotationMatrixInverse;

		private Vector3 Translate;

		private double Scale;

		public void UpdateView(double viewRatio, Vector2 principalPoint, Vector2 vanishingPointA, Vector2 vanishingPointB, Vector2 origin, CalibrationAxes axes, InvertedAxes inverted)
		{
			double scale = GetInstrinsicParametersScale(principalPoint, viewRatio, vanishingPointA, vanishingPointB);
			IntrinsicMatrix = GetIntrinsicParametersMatrix(principalPoint, scale, viewRatio);
			IntrinsicMatrixInverse = GetInvertedIntrinsicParametersMatrix(principalPoint, scale, viewRatio);
			RotationMatrix = GetRotationalMatrix(IntrinsicMatrixInverse, vanishingPointA, vanishingPointB, principalPoint, axes, inverted);
			RotationMatrixInverse = RotationMatrix.Transposed();

			Translate = IntrinsicMatrixInverse * new Vector3(origin.X, origin.Y, 1);
		}

		public void UpdateScale(double newScale) => Scale = newScale;

		public Vector2 WorldToScreen(Vector3 worldPoint)
		{
			Vector3 point = RotationMatrix * worldPoint * Scale + Translate;
			point = point / point.Z;
			point = IntrinsicMatrix * point;
			return new Vector2(point.X, point.Y);
		}

		public Vector3 ScreenToWorld(Vector2 screenPoint)
		{
			return RotationMatrixInverse * (IntrinsicMatrixInverse * new Vector3(screenPoint.X, screenPoint.Y, 1) - Translate) / Scale;
		}

		public Ray3D ScreenToWorldRay(Vector2 screenPoint)
		{
			Vector3 origin = RotationMatrixInverse * (IntrinsicMatrixInverse * new Vector3(screenPoint.X, screenPoint.Y, 1) - Translate) / Scale;
			Vector3 behindOrigin = RotationMatrixInverse * (IntrinsicMatrixInverse * new Vector3(screenPoint.X, screenPoint.Y, 1) * 2 - Translate) / Scale;
			return new Ray3D(origin, behindOrigin - origin);
		}

		public Vector2 MatchScreenWorldPoint(Vector2 screenPoint, Vector3 worldPoint)
		{
			Vector3 rightHandSide = IntrinsicMatrix * (RotationMatrix * (Scale * worldPoint)) + new Vector3(0, 0, 1);
			return new Vector2(screenPoint.X * rightHandSide.Z - rightHandSide.X, screenPoint.Y * rightHandSide.Z - rightHandSide.Y);
		}

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
				Vector3 mid = (v1.X >= 0.5 || v1.X <= -0.5) ? (v1 + new Vector3(0, 0.5, 0)).Normalized() : (v1 + new Vector3(0.5, 0, 0)).Normalized();
				return RotateAlign(mid, v2) * RotateAlign(v1, mid);
			}

			return new Matrix3x3()
			{
				A0_ = new Vector3((axis.X * axis.X * k) + cosA, (axis.Y * axis.X * k) - axis.Z, (axis.Z * axis.X * k) + axis.Y),
				A1_ = new Vector3((axis.X * axis.Y * k) + axis.Z, (axis.Y * axis.Y * k) + cosA, (axis.Z * axis.Y * k) - axis.X),
				A2_ = new Vector3((axis.X * axis.Z * k) - axis.Y, (axis.Y * axis.Z * k) + axis.X, (axis.Z * axis.Z * k) + cosA)
			};
		}
	}
}