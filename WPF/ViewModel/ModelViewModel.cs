using Photomatch_ProofOfConcept_WPF.Gui;
using Photomatch_ProofOfConcept_WPF.Logic;
using Photomatch_ProofOfConcept_WPF.WPF.Helper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Photomatch_ProofOfConcept_WPF.WPF.ViewModel
{

	class ModelViewModel : BaseViewModel, IKeyboardHandler, IMouseHandler, IModelView
    {
        public Viewport3D ModelContent { get; private set; }

        public IMouseHandler MouseHandler
        {
            get => this;
        }

        private Point3D CameraPosition_ = new Point3D(0, 0, 10);
        public Point3D CameraPosition
		{
            get => CameraPosition_;
            set
			{
                CameraPosition_ = value;
                OnPropertyChanged(nameof(CameraPosition));
            }
		}

        private Vector3 ModelRotate_;
        public Vector3 ModelRotate
		{
            get => ModelRotate_;
            set
			{
                ModelRotate_ = value;
                OnPropertyChanged(nameof(ModelRotate));
            }

        }

        public ICommand ViewportLoaded { get; }

        public MeshGeometry3D MeshGeometry { get; } = new MeshGeometry3D();

        private Model Model;
        private Viewport3D Viewport;
        private Vector3DCollection VertexNormals = new Vector3DCollection();
        private Point3DCollection VertexPositions = new Point3DCollection();
        private Int32Collection TriangleIndices = new Int32Collection();

        public ModelViewModel(Model model)
		{
            SetModel(model);

            MeshGeometry.Normals = VertexNormals;
            MeshGeometry.Positions = VertexPositions;
            MeshGeometry.TriangleIndices = TriangleIndices;

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

        private void SetModel(Model model)
		{
            this.Model = model;

            model.AddFaceEvent += AddFace;

            foreach (Face face in model.Faces)
                AddFace(face);
		}

        private void FaceRemove(Face face)
		{
            face.FaceRemovedEvent -= FaceRemove;

            UpdateModel(this.Model);
		}

        private void AddTriangle(Triangle triangle, Vector3 normal)
		{
            VertexNormals.Add(new Vector3D(normal.X, normal.Z, normal.Y));
            VertexNormals.Add(new Vector3D(normal.X, normal.Z, normal.Y));
            VertexNormals.Add(new Vector3D(normal.X, normal.Z, normal.Y));

            VertexPositions.Add(new Point3D(triangle.A.Position.X, triangle.A.Position.Z, triangle.A.Position.Y));
            VertexPositions.Add(new Point3D(triangle.B.Position.X, triangle.B.Position.Z, triangle.B.Position.Y));
            VertexPositions.Add(new Point3D(triangle.C.Position.X, triangle.C.Position.Z, triangle.C.Position.Y));

            int aIndex = 0 + TriangleIndices.Count;
            int bIndex = 1 + TriangleIndices.Count;
            int cIndex = 2 + TriangleIndices.Count;

            TriangleIndices.Add(aIndex);
            TriangleIndices.Add(bIndex);
            TriangleIndices.Add(cIndex);
        }

        private void AddFace(Face face)
		{
            face.FaceRemovedEvent += FaceRemove;

            Vector3 normal = face.Reversed ? face.Normal : -face.Normal;

            foreach (var triangle in face.Triangulated)
            {
                Triangle reversedTriangle = new Triangle { A = triangle.B, B = triangle.A, C = triangle.C };
                if (face.Reversed)
				{
                    AddTriangle(reversedTriangle, -face.Normal);
                    AddTriangle(triangle, face.Normal);
				}
                else
				{
                    AddTriangle(triangle, face.Normal);
                    AddTriangle(reversedTriangle, -face.Normal);
                }

            }
        }

		public void KeyUp(object sender, KeyEventArgs e)
		{
		}

		public void KeyDown(object sender, KeyEventArgs e)
        {
        }

		public void MouseDown(object sender, MouseButtonEventArgs e)
		{
		}

		public void MouseUp(object sender, MouseButtonEventArgs e)
		{
		}

		public void MouseMove(object sender, MouseEventArgs e)
		{
            Vector2 mouse = e.GetPosition(Viewport).AsVector2();
            ModelRotate = new Vector3(mouse, 0);
		}

		public void MouseEnter(object sender, MouseEventArgs e)
		{
		}

		public void MouseLeave(object sender, MouseEventArgs e)
		{
		}

		public void MouseWheel(object sender, MouseWheelEventArgs e)
		{
            CameraPosition = new Point3D(0, 0, Math.Pow(CameraPosition.Z, 1 + e.Delta / 120.0 / 10));
        }

		public void UpdateModel(Model model)
		{
            VertexNormals.Clear();
            VertexPositions.Clear();
            TriangleIndices.Clear();

            this.Model.AddFaceEvent -= AddFace;
            SetModel(model);
        }
	}
}
