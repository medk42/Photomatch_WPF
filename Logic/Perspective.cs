using System.IO;

using MatrixVector;
using System;
using Lines;
using SixLabors.ImageSharp;
using Serializables;

namespace Perspective
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
		}

		public Vector3 ScreenToWorld(Vector2 point) => _camera.ScreenToWorld(point);

		public Vector2 WorldToScreen(Vector3 point) => _camera.WorldToScreen(point);

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
		private Matrix3x3 intrinsicMatrix;
		private Matrix3x3 rotationMatrix;

		private Matrix3x3 intrinsicMatrixInverse;
		private Matrix3x3 rotationMatrixInverse;

		private Vector3 translate;

		public void UpdateView(double viewRatio, Vector2 principalPoint, Vector2 vanishingPointA, Vector2 vanishingPointB, Vector2 origin, CalibrationAxes axes, InvertedAxes inverted)
		{
			double scale = GetInstrinsicParametersScale(principalPoint, viewRatio, vanishingPointA, vanishingPointB);
			intrinsicMatrix = GetIntrinsicParametersMatrix(principalPoint, scale, viewRatio);
			intrinsicMatrixInverse = GetInvertedIntrinsicParametersMatrix(principalPoint, scale, viewRatio);
			rotationMatrix = GetRotationalMatrix(intrinsicMatrixInverse, vanishingPointA, vanishingPointB, principalPoint, axes, inverted);
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

		public static Matrix3x3 GetRotationalMatrix(Matrix3x3 invertedIntrinsicMatrix, Vector2 vanishingPointA, Vector2 vanishingPointB, Vector2 principalPoint, CalibrationAxes axes, InvertedAxes inverted)
		{
			Matrix3x3 rotationMatrix = new Matrix3x3();

			Vector3 firstCol, secondCol, thirdCol;

			Vector3 colA = (invertedIntrinsicMatrix * new Vector3(vanishingPointA.X, vanishingPointA.Y, 1)).Normalized();
			Vector3 colB = (invertedIntrinsicMatrix * new Vector3(vanishingPointB.X, vanishingPointB.Y, 1)).Normalized();

			switch (axes)
			{
				case CalibrationAxes.XY:
					firstCol = colA;
					secondCol = colB;
					thirdCol = Vector3.Cross(firstCol, secondCol);
					break;
				case CalibrationAxes.YX:
					firstCol = colB;
					secondCol = colA;
					thirdCol = Vector3.Cross(firstCol, secondCol);
					break;
				case CalibrationAxes.XZ:
					firstCol = colA;
					thirdCol = colB;
					secondCol = Vector3.Cross(thirdCol, firstCol);
					break;
				case CalibrationAxes.ZX:
					firstCol = colB;
					thirdCol = colA;
					secondCol = Vector3.Cross(thirdCol, firstCol);
					break;
				case CalibrationAxes.YZ:
					secondCol = colA;
					thirdCol = colB;
					firstCol = Vector3.Cross(secondCol, thirdCol);
					break;
				case CalibrationAxes.ZY:
					secondCol = colB;
					thirdCol = colA;
					firstCol = Vector3.Cross(secondCol, thirdCol);
					break;
				default:
					throw new Exception("Unexpected switch case.");
			}

			if (inverted.X)
				firstCol = -firstCol;
			if (inverted.Y)
				secondCol = -secondCol;
			if (inverted.Z)
				thirdCol = -thirdCol;

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
	}
}