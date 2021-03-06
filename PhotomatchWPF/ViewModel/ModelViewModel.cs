using PhotomatchCore.Gui;
using PhotomatchCore.Logic;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using PhotomatchCore.Logic.Model;
using PhotomatchWPF.ViewModel.Helper;

namespace PhotomatchWPF.ViewModel
{

	/// <summary>
	/// Represents the window with a 3d model at the View layer.
	/// </summary>
	class ModelViewModel : BaseViewModel, IKeyboardHandler, IMouseHandler, IModelView
	{
		private static readonly double CameraZOffset = 1;

		public IMouseHandler MouseHandler
		{
			get => this;
		}

		/// <summary>
		/// Position of the camera.
		/// </summary>
		public Point3D CameraPosition
		{
			get => CameraPosition_;
			set
			{
				CameraPosition_ = value;
				OnPropertyChanged(nameof(CameraPosition));
			}
		}
		private Point3D CameraPosition_ = new Point3D(0, 0, 10);

		/// <summary>
		/// Rotation of the model.
		/// </summary>
		public Vector3 ModelRotate
		{
			get => ModelRotate_;
			set
			{
				ModelRotate_ = value;
				OnPropertyChanged(nameof(ModelRotate));
			}
		}
		private Vector3 ModelRotate_;

		/// <summary>
		/// Translation of the model.
		/// </summary>
		public Vector3 Translate
		{
			get => Translate_;
			set
			{
				Translate_ = value;
				OnPropertyChanged(nameof(Translate));
			}
		}
		private Vector3 Translate_;

		public ICommand ViewportLoaded { get; }

		/// <summary>
		/// Front faces (green).
		/// </summary>
		public MeshGeometry3D MeshGeometryFront { get; } = new MeshGeometry3D();

		/// <summary>
		/// Back faces (red).
		/// </summary>
		public MeshGeometry3D MeshGeometryBack { get; } = new MeshGeometry3D();

		private Model Model;
		private Viewport3D Viewport;

		private Vector3DCollection VertexNormalsFront = new Vector3DCollection();
		private Point3DCollection VertexPositionsFront = new Point3DCollection();
		private Int32Collection TriangleIndicesFront = new Int32Collection();
		private Vector3DCollection VertexNormalsBack = new Vector3DCollection();
		private Point3DCollection VertexPositionsBack = new Point3DCollection();
		private Int32Collection TriangleIndicesBack = new Int32Collection();

		private Vector2 DraggingOffset = Vector2.InvalidInstance;

		/// <param name="model">Model to be displayed.</param>
		public ModelViewModel(Model model)
		{
			SetModel(model);

			MeshGeometryFront.Normals = VertexNormalsFront;
			MeshGeometryFront.Positions = VertexPositionsFront;
			MeshGeometryFront.TriangleIndices = TriangleIndicesFront;
			MeshGeometryBack.Normals = VertexNormalsBack;
			MeshGeometryBack.Positions = VertexPositionsBack;
			MeshGeometryBack.TriangleIndices = TriangleIndicesBack;

			ViewportLoaded = new RelayCommand(ViewportLoaded_);
		}

		public void ViewportLoaded_(object obj)
		{
			Viewport3D viewport = obj as Viewport3D;
			if (viewport == null)
			{
				throw new ArgumentException("obj is not of type " + nameof(Viewport3D));
			}

			Viewport = viewport;
		}

		/// <summary>
		/// Set model, register for change event, recalculate.
		/// </summary>
		/// <param name="model"></param>
		private void SetModel(Model model)
		{
			Model = model;

			model.ModelChangedEvent += ModelChanged;

			Recalculate();
		}

		/// <summary>
		/// Recalculate.
		/// </summary>
		private void ModelChanged() => Recalculate();

		/// <summary>
		/// Clear all data and rebuild faces from model and translation as average of vertices.
		/// </summary>
		private void Recalculate()
		{
			VertexNormalsFront.Clear();
			VertexPositionsFront.Clear();
			TriangleIndicesFront.Clear();
			VertexNormalsBack.Clear();
			VertexPositionsBack.Clear();
			TriangleIndicesBack.Clear();

			foreach (Face face in Model.Faces)
				AddFace(face);

			Vector3 vertexPositionsSum = new Vector3();
			foreach (Vertex vertex in Model.Vertices)
				vertexPositionsSum += vertex.Position;

			Translate = -vertexPositionsSum / Model.Vertices.Count;
		}

		/// <summary>
		/// Create triangles for specified face.
		/// </summary>
		private void AddFace(Face face)
		{
			foreach (var triangle in face.Triangulated)
			{
				Triangle reversedTriangle = new Triangle { A = triangle.B, B = triangle.A, C = triangle.C };

				AddTriangle(reversedTriangle, face.Normal, !face.Reversed);
				AddTriangle(triangle, -face.Normal, face.Reversed);
			}
		}

		/// <summary>
		/// Add vertex normals, positions and indices for a triangle.
		/// </summary>
		private void AddTriangle(Triangle triangle, Vector3 normal, bool front)
		{
			Vector3DCollection vertexNormals = front ? VertexNormalsFront : VertexNormalsBack;
			Point3DCollection vertexPositions = front ? VertexPositionsFront : VertexPositionsBack;
			Int32Collection triangleIndices = front ? TriangleIndicesFront : TriangleIndicesBack;

			vertexNormals.Add(new Vector3D(normal.X, normal.Z, normal.Y));
			vertexNormals.Add(new Vector3D(normal.X, normal.Z, normal.Y));
			vertexNormals.Add(new Vector3D(normal.X, normal.Z, normal.Y));

			vertexPositions.Add(new Point3D(triangle.A.Position.X, triangle.A.Position.Z, triangle.A.Position.Y));
			vertexPositions.Add(new Point3D(triangle.B.Position.X, triangle.B.Position.Z, triangle.B.Position.Y));
			vertexPositions.Add(new Point3D(triangle.C.Position.X, triangle.C.Position.Z, triangle.C.Position.Y));

			int aIndex = 0 + triangleIndices.Count;
			int bIndex = 1 + triangleIndices.Count;
			int cIndex = 2 + triangleIndices.Count;

			triangleIndices.Add(aIndex);
			triangleIndices.Add(bIndex);
			triangleIndices.Add(cIndex);
		}

		public void KeyUp(object sender, KeyEventArgs e) { }

		public void KeyDown(object sender, KeyEventArgs e) { }

		/// <summary>
		/// Get mouse position as Vector2 from MouseEventArgs.
		/// </summary>
		private Vector2 GetMousePosition(MouseEventArgs e)
		{
			return e.GetPosition(Viewport).AsVector2(); ;
		}

		/// <summary>
		/// Start rotation of the model.
		/// </summary>
		public void MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
				DraggingOffset = GetMousePosition(e);
		}

		/// <summary>
		/// End rotation of the model.
		/// </summary>
		public void MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
				DraggingOffset = Vector2.InvalidInstance;
		}

		/// <summary>
		/// Handle rotation of the model. Clamp rotation up/down to +-90 degrees.
		/// </summary>
		public void MouseMove(object sender, MouseEventArgs e)
		{
			if (DraggingOffset.Valid)
			{
				Vector2 newOffset = GetMousePosition(e);
				ModelRotate += new Vector3(newOffset - DraggingOffset, 0);
				ModelRotate = ModelRotate.WithY(Math.Clamp(ModelRotate.Y, -90, 90));
				DraggingOffset = newOffset;
			}
		}

		public void MouseEnter(object sender, MouseEventArgs e) { }

		public void MouseLeave(object sender, MouseEventArgs e) { }

		/// <summary>
		/// Move camera towards/away from the model.
		/// </summary>
		public void MouseWheel(object sender, MouseWheelEventArgs e)
		{
			CameraPosition = new Point3D(0, 0, Math.Pow(CameraPosition.Z + CameraZOffset, 1 - e.Delta / 120.0 / 10) - CameraZOffset);
		}

		/// <summary>
		/// Update displayed model to model passed by parameter.
		/// </summary>
		public void UpdateModel(Model model)
		{
			Model.ModelChangedEvent -= ModelChanged;
			SetModel(model);
		}
	}
}
