using PhotomatchCore.Utilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PhotomatchCore.Logic.Perspective
{
	/// <summary>
	/// Class containing all data about an image and its calibration.
	/// </summary>
	public class PerspectiveData : ISafeSerializable<PerspectiveData>
	{
		public delegate void PerspectiveChangedEventHandler();

		/// <summary>
		/// Called when perspective is changed in any way. 
		/// </summary>
		public event PerspectiveChangedEventHandler PerspectiveChangedEvent;

		/// <summary>
		/// Image for this perspective.
		/// </summary>
		public Image<Rgb24> Image { get; private set; }

		private Camera _camera = new Camera();
		private Vector2 _origin;
		private Line2D _lineA1 = new Line2D(new Vector2(0.52, 0.19), new Vector2(0.76, 0.28));
		private Line2D _lineA2 = new Line2D(new Vector2(0.35, 0.67), new Vector2(0.46, 0.82));
		private Line2D _lineB1 = new Line2D(new Vector2(0.27, 0.31), new Vector2(0.48, 0.21));
		private Line2D _lineB2 = new Line2D(new Vector2(0.55, 0.78), new Vector2(0.71, 0.68));
		private CalibrationAxes _calibrationAxes = CalibrationAxes.XY;
		private InvertedAxes _invertedAxes;
		private double _scale = 1;
		private byte[] _imageData;

		/// <summary>
		/// Projection of world origin point on image.
		/// </summary>
		public Vector2 Origin
		{
			get => _origin;
			set
			{
				_origin = value;
				RecalculateProjection();
				PerspectiveChangedEvent?.Invoke();
			}
		}

		/// <summary>
		/// First (world) parallel line for the first axis on image.
		/// </summary>
		public Line2D LineA1
		{
			get => _lineA1;
			set
			{
				_lineA1 = value;
				RecalculateProjection();
				PerspectiveChangedEvent?.Invoke();
			}
		}

		/// <summary>
		/// Second (world) parallel line for the first axis on image.
		/// </summary>
		public Line2D LineA2
		{
			get => _lineA2;
			set
			{
				_lineA2 = value;
				RecalculateProjection();
				PerspectiveChangedEvent?.Invoke();
			}
		}

		/// <summary>
		/// First (world) parallel line for the second axis on image.
		/// </summary>
		public Line2D LineB1
		{
			get => _lineB1;
			set
			{
				_lineB1 = value;
				RecalculateProjection();
				PerspectiveChangedEvent?.Invoke();
			}
		}

		/// <summary>
		/// Second (world) parallel line for the second axis on image.
		/// </summary>
		public Line2D LineB2
		{
			get => _lineB2;
			set
			{
				_lineB2 = value;
				RecalculateProjection();
				PerspectiveChangedEvent?.Invoke();
			}
		}

		/// <summary>
		/// Specifies the first and second calibration axes - which axes do 
		/// LineA1/2 and LineB1/2 represent.
		/// </summary>
		public CalibrationAxes CalibrationAxes
		{
			get => _calibrationAxes;
			set
			{
				if (_calibrationAxes != value)
				{
					_calibrationAxes = value;
					RecalculateProjection();
					PerspectiveChangedEvent?.Invoke();
				}
			}
		}

		/// <summary>
		/// Specifies axes direction inversion. Default positive direction is 
		/// towards the vanishing point of that axis.
		/// </summary>
		public InvertedAxes InvertedAxes
		{
			get => _invertedAxes;
			set
			{
				_invertedAxes = value;
				RecalculateProjection();
				PerspectiveChangedEvent?.Invoke();
			}
		}

		/// <summary>
		/// Scaling factor between this calibration and model.
		/// </summary>
		public double Scale
		{
			get => _scale;
			set
			{
				_scale = value;
				_camera.UpdateScale(value);
				PerspectiveChangedEvent?.Invoke();
			}
		}

		/// <summary>
		/// Original path to the loaded image.
		/// </summary>
		public string ImagePath { get; private set; }

		/// <summary>
		/// Create a new perspective from specified image, image bytes and image path.
		/// </summary>
		public PerspectiveData(Image<Rgb24> image, byte[] imageData, string imagePath)
		{
			Image = image;
			ImagePath = imagePath;
			_imageData = imageData;

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

			writer.Write(_imageData.Length);
			writer.Write(_imageData);

			writer.Write(ImagePath);

			SerializeWithoutImage(writer);
		}

		/// <summary>
		/// Serialize only calibration data to binary writer.
		/// </summary>
		public void SerializeWithoutImage(BinaryWriter writer)
		{
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
			_imageData = reader.ReadBytes(imageDataLength);
			Image = SixLabors.ImageSharp.Image.Load<Rgb24>(_imageData);

			ImagePath = reader.ReadString();

			DeserializeWithoutImage(reader);
		}

		/// <summary>
		/// De-serialize only calibration data from binary reader to current instance.
		/// </summary>
		public void DeserializeWithoutImage(BinaryReader reader)
		{
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

		/// <summary>
		/// Scale Line2D's x coordinates by xStretch and y coordinates by yStretch.
		/// </summary>
		private Line2D ScaleLine(Line2D line, double xStretch, double yStretch)
		{
			var newStart = new Vector2(line.Start.X * xStretch, line.Start.Y * yStretch);
			var newEnd = new Vector2(line.End.X * xStretch, line.End.Y * yStretch);
			return new Line2D(newStart, newEnd);
		}

		/// <summary>
		/// Recalculate projection matrices based on calibration variables. 
		/// Principal point is chosen as the image midpoint.
		/// </summary>
		public void RecalculateProjection()
		{
			Vector2 vanishingPointA = Intersections2D.GetLineLineIntersection(LineA1, LineA2).Intersection;
			Vector2 vanishingPointB = Intersections2D.GetLineLineIntersection(LineB1, LineB2).Intersection;
			Vector2 principalPoint = new Vector2(Image.Width / 2, Image.Height / 2);
			double viewRatio = 1;

			_camera.UpdateView(viewRatio, principalPoint, vanishingPointA, vanishingPointB, Origin, CalibrationAxes, InvertedAxes);
			_camera.UpdateScale(Scale);
		}

		/// <summary>
		/// Transform vector from screen space (with 1 as z coordinate) to world space.
		/// </summary>
		public Vector3 ScreenToWorld(Vector2 point) => _camera.ScreenToWorld(point);

		/// <summary>
		/// Transform vector from world space to screen space (with z normalized).
		/// </summary>
		public Vector2 WorldToScreen(Vector3 point) => _camera.WorldToScreen(point);

		/// <summary>
		/// Create a ray in world space from Vector3(screenPoint, 1) in the direction 
		/// of the view of the camera at that point. 
		/// </summary>
		public Ray3D ScreenToWorldRay(Vector2 screenPoint) => _camera.ScreenToWorldRay(screenPoint);

		/// <summary>
		/// Calculate perspective origin screen position, based on transformation matrices and scale, 
		/// so that the WorldToScreen projection of the worldPoint is at screenPoint.
		/// </summary>
		public Vector2 MatchScreenWorldPoint(Vector2 screenPoint, Vector3 worldPoint) => _camera.MatchScreenWorldPoint(screenPoint, worldPoint);

		/// <summary>
		/// Calculate perspective origin screen position and scale, based on transformation matrices,
		/// so that the WorldToScreen projection of the worldPointPos is at screenPointPos and
		/// the WorldToScreen projection of worldPointScale is closest to screenPointScale.
		/// </summary>
		/// <returns>
		/// Vector3 containing the calculated perspective origin as x and y coordinates and 
		/// the calculated scale as the z coordinate.
		/// </returns>
		public Vector3 MatchScreenWorldPoints(Vector2 screenPointPos, Vector3 worldPointPos, Vector2 screenPointScale, Vector3 worldPointScale) => _camera.MatchScreenWorldPoints(screenPointPos, worldPointPos, screenPointScale, worldPointScale);

		/// <summary>
		/// Get the direction (on the screen) of the x axis at a certain point on the screen.
		/// </summary>
		public Vector2 GetXDirAt(Vector2 screenPoint)
		{
			Vector2 screenPointMoved = _camera.WorldToScreen(_camera.ScreenToWorld(screenPoint) + new Vector3(1, 0, 0));
			Vector2 direction = (screenPointMoved - screenPoint).Normalized();
			return direction;
		}

		/// <summary>
		/// Get the direction (on the screen) of the y axis at a certain point on the screen.
		/// </summary>
		public Vector2 GetYDirAt(Vector2 screenPoint)
		{
			Vector2 screenPointMoved = _camera.WorldToScreen(_camera.ScreenToWorld(screenPoint) + new Vector3(0, 1, 0));
			Vector2 direction = (screenPointMoved - screenPoint).Normalized();
			return direction;
		}

		/// <summary>
		/// Get the direction (on the screen) of the z axis at a certain point on the screen.
		/// </summary>
		public Vector2 GetZDirAt(Vector2 screenPoint)
		{
			Vector2 screenPointMoved = _camera.WorldToScreen(_camera.ScreenToWorld(screenPoint) + new Vector3(0, 0, 1));
			Vector2 direction = (screenPointMoved - screenPoint).Normalized();
			return direction;
		}
	}
}
