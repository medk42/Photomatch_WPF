using System.IO;

using MatrixVector;
using System;
using Lines;
using SixLabors.ImageSharp;
using Serializables;

namespace Perspective
{
	public class PerspectiveData : ISafeSerializable<PerspectiveData> 
	{
		public Image Image { get; private set; }

		private Camera _camera = new Camera();
		private Vector2 _origin;
		private Line2D _lineX1 = new Line2D(new Vector2(0.52, 0.19), new Vector2(0.76, 0.28));
		private Line2D _lineX2 = new Line2D(new Vector2(0.35, 0.67), new Vector2(0.46, 0.82));
		private Line2D _lineY1 = new Line2D(new Vector2(0.27, 0.31), new Vector2(0.48, 0.21));
		private Line2D _lineY2 = new Line2D(new Vector2(0.55, 0.78), new Vector2(0.71, 0.68));

		public Vector2 Origin
		{
			get => _origin;
			set
			{
				_origin = value;
				RecalculateProjection();
			}
		}

		public Line2D LineX1
		{
			get => _lineX1;
			set
			{
				_lineX1 = value;
				RecalculateProjection();
			}
		}

		public Line2D LineX2
		{
			get => _lineX2;
			set
			{
				_lineX2 = value;
				RecalculateProjection();
			}
		}

		public Line2D LineY1
		{
			get => _lineY1;
			set
			{
				_lineY1 = value;
				RecalculateProjection();
			}
		}

		public Line2D LineY2
		{
			get => _lineY2;
			set
			{
				_lineY2 = value;
				RecalculateProjection();
			}
		}

		public PerspectiveData(Image image)
		{
			Image = image;

			LineX1 = ScaleLine(LineX1, image.Width, image.Height);
			LineX2 = ScaleLine(LineX2, image.Width, image.Height);
			LineY1 = ScaleLine(LineY1, image.Width, image.Height);
			LineY2 = ScaleLine(LineY2, image.Width, image.Height);

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

			_origin.Serialize(writer);
			_lineX1.Serialize(writer);
			_lineX2.Serialize(writer);
			_lineY1.Serialize(writer);
			_lineY2.Serialize(writer);
		}

		public void Deserialize(BinaryReader reader)
		{
			int imageDataLength = reader.ReadInt32();
			byte[] imageData = reader.ReadBytes(imageDataLength);
			using (var stream = new MemoryStream(imageData))
			{
				Image = Image.Load(stream);
			}

			_origin = ISafeSerializable<Vector2>.CreateDeserialize(reader);
			_lineX1 = ISafeSerializable<Line2D>.CreateDeserialize(reader);
			_lineX2 = ISafeSerializable<Line2D>.CreateDeserialize(reader);
			_lineY1 = ISafeSerializable<Line2D>.CreateDeserialize(reader);
			_lineY2 = ISafeSerializable<Line2D>.CreateDeserialize(reader);

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
			Vector2 vanishingPointX = Intersections2D.GetLineLineIntersection(LineX1, LineX2).Intersection;
			Vector2 vanishingPointY = Intersections2D.GetLineLineIntersection(LineY1, LineY2).Intersection;
			Vector2 principalPoint = new Vector2(Image.Width / 2, Image.Height / 2);
			double viewRatio = 1;

			_camera.UpdateView(viewRatio, principalPoint, vanishingPointX, vanishingPointY, Origin);
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

		public void UpdateView(double viewRatio, Vector2 principalPoint, Vector2 firstVanishingPoint, Vector2 secondVanishingPoint, Vector2 origin)
		{
			double scale = GetInstrinsicParametersScale(principalPoint, viewRatio, firstVanishingPoint, secondVanishingPoint);
			intrinsicMatrix = GetIntrinsicParametersMatrix(principalPoint, scale, viewRatio);
			intrinsicMatrixInverse = GetInvertedIntrinsicParametersMatrix(principalPoint, scale, viewRatio);
			rotationMatrix = GetRotationalMatrix(intrinsicMatrixInverse, firstVanishingPoint, secondVanishingPoint, principalPoint);
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

		public static Matrix3x3 GetRotationalMatrix(Matrix3x3 invertedIntrinsicMatrix, Vector2 firstVanishingPoint, Vector2 secondVanishingPoint, Vector2 principalPoint)
		{
			Vector2 thirdVanishingPoint = GetTriangleThirdVertexFromOrthocenter(firstVanishingPoint, secondVanishingPoint, principalPoint);

			Matrix3x3 rotationMatrix = new Matrix3x3();

			Vector3 firstCol = (invertedIntrinsicMatrix * new Vector3(firstVanishingPoint.X, firstVanishingPoint.Y, 1)).Normalized();
			Vector3 secondCol = (invertedIntrinsicMatrix * new Vector3(secondVanishingPoint.X, secondVanishingPoint.Y, 1)).Normalized();
			Vector3 thirdCol = (invertedIntrinsicMatrix * new Vector3(thirdVanishingPoint.X, thirdVanishingPoint.Y, 1)).Normalized();

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