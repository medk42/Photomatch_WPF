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

        public MeshGeometry3D MeshGeometry { get; } = new MeshGeometry3D();

        private Model Model;
        private Vector3DCollection VertexNormals = new Vector3DCollection();
        private Point3DCollection VertexPositions = new Point3DCollection();
        private Int32Collection TriangleIndices = new Int32Collection();

        public ModelViewModel(Model model)
		{
            this.Model = model;
            Model.AddFaceEvent += Model_AddFaceEvent;
            Model.AddVertexEvent += Model_AddVertexEvent;

            MeshGeometry.Normals = VertexNormals;
            MeshGeometry.Positions = VertexPositions;
            MeshGeometry.TriangleIndices = TriangleIndices;
        }

		private void Model_AddVertexEvent(Vertex vertex)
		{
            /*Vector3 pos = vertex.Position;
            myPositionCollection.Add(new Point3D(pos.X, pos.Y, pos.Z));*/
        }

		private void Model_AddFaceEvent(Face face)
		{
            /*Vector3 normal = face.Reversed ? -face.Normal : face.Normal;

            foreach (Triangle t in face.Triangulated)
            {
                int aIndex = Model.Vertices.IndexOf(t.A);
                int bIndex = Model.Vertices.IndexOf(t.B);
                int cIndex = Model.Vertices.IndexOf(t.C);

                if (face.Reversed)
                    (aIndex, bIndex) = (bIndex, aIndex);

                myNormalCollection.Add(new Vector3D(normal.X, normal.Y, normal.Z));
                myNormalCollection.Add(new Vector3D(normal.X, normal.Y, normal.Z));
                myNormalCollection.Add(new Vector3D(normal.X, normal.Y, normal.Z));

                myTriangleIndicesCollection.Add(aIndex);
                myTriangleIndicesCollection.Add(bIndex);
                myTriangleIndicesCollection.Add(cIndex);
            }*/
		}

		public void KeyUp(object sender, KeyEventArgs e)
		{
		}

		public void KeyDown(object sender, KeyEventArgs e)
        {
        }

		public void MouseDown(object sender, MouseButtonEventArgs e)
		{
            if (e.ClickCount == 2)
            {
                int triangleCount = 0;

                foreach (var face in Model.Faces)
                {
                    Vector3 normal = face.Reversed ? face.Normal : -face.Normal;

                    foreach (var triangle in face.Triangulated)
					{
                        VertexNormals.Add(new Vector3D(normal.X, normal.Z, normal.Y));
                        VertexNormals.Add(new Vector3D(normal.X, normal.Z, normal.Y));
                        VertexNormals.Add(new Vector3D(normal.X, normal.Z, normal.Y));

                        VertexPositions.Add(new Point3D(triangle.A.Position.X, triangle.A.Position.Z, triangle.A.Position.Y));
                        VertexPositions.Add(new Point3D(triangle.B.Position.X, triangle.B.Position.Z, triangle.B.Position.Y));
                        VertexPositions.Add(new Point3D(triangle.C.Position.X, triangle.C.Position.Z, triangle.C.Position.Y));

                        int aIndex = 0 + 3 * triangleCount;
                        int bIndex = 1 + 3 * triangleCount;
                        int cIndex = 2 + 3 * triangleCount;

                        if (!face.Reversed)
                            (aIndex, bIndex) = (bIndex, aIndex);

                        TriangleIndices.Add(aIndex);
                        TriangleIndices.Add(bIndex);
                        TriangleIndices.Add(cIndex);

                        triangleCount++;
                    }
                }
            }
		}

		public void MouseUp(object sender, MouseButtonEventArgs e)
		{
		}

		public void MouseMove(object sender, MouseEventArgs e)
		{
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
            this.Model = model;
            VertexNormals.Clear();
            VertexPositions.Clear();
            TriangleIndices.Clear();
        }
	}
}
