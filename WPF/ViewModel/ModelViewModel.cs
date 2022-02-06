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

        private Model Model;
        private PerspectiveCamera myPCamera;
        private Vector3DCollection myNormalCollection = new Vector3DCollection();
        private Point3DCollection myPositionCollection = new Point3DCollection();
        private Int32Collection myTriangleIndicesCollection = new Int32Collection();


        public ModelViewModel(Model model)
		{
            this.Model = model;

			Model.AddFaceEvent += Model_AddFaceEvent;
			Model.AddVertexEvent += Model_AddVertexEvent;

            // Declare scene objects.
            Viewport3D myViewport3D = new Viewport3D();
            Model3DGroup myModel3DGroup = new Model3DGroup();
            GeometryModel3D myGeometryModel = new GeometryModel3D();
            ModelVisual3D myModelVisual3D = new ModelVisual3D();
            // Defines the camera used to view the 3D object. In order to view the 3D object,
            // the camera must be positioned and pointed such that the object is within view
            // of the camera.
            myPCamera = new PerspectiveCamera();

            // Specify where in the 3D scene the camera is.
            myPCamera.Position = new Point3D(0, 0, 10);

            // Specify the direction that the camera is pointing.
            myPCamera.LookDirection = new Vector3D(0, 0, -1);

            // Define camera's horizontal field of view in degrees.
            myPCamera.FieldOfView = 60;

            // Asign the camera to the viewport
            myViewport3D.Camera = myPCamera;
            // Define the lights cast in the scene. Without light, the 3D object cannot
            // be seen. Note: to illuminate an object from additional directions, create
            // additional lights.
            DirectionalLight myDirectionalLight = new DirectionalLight();
            myDirectionalLight.Color = Colors.White;
            myDirectionalLight.Direction = new Vector3D(-0.61, -0.5, -0.61);

            myModel3DGroup.Children.Add(myDirectionalLight);

            // The geometry specifes the shape of the 3D plane. In this sample, a flat sheet
            // is created.
            MeshGeometry3D myMeshGeometry3D = new MeshGeometry3D();

            // Create a collection of normal vectors for the MeshGeometry3D.
            myMeshGeometry3D.Normals = myNormalCollection;

            // Create a collection of vertex positions for the MeshGeometry3D.
            myMeshGeometry3D.Positions = myPositionCollection;

            // Create a collection of triangle indices for the MeshGeometry3D.
            myMeshGeometry3D.TriangleIndices = myTriangleIndicesCollection;

            // Apply the mesh to the geometry model.
            myGeometryModel.Geometry = myMeshGeometry3D;
            myGeometryModel.Material = new DiffuseMaterial(Brushes.Gray);

            // Apply a transform to the object. In this sample, a rotation transform is applied,
            // rendering the 3D object rotated.
            RotateTransform3D myRotateTransform3D = new RotateTransform3D();
            AxisAngleRotation3D myAxisAngleRotation3d = new AxisAngleRotation3D();
            myAxisAngleRotation3d.Axis = new Vector3D(0, 3, 0);
            myAxisAngleRotation3d.Angle = 40;
            myRotateTransform3D.Rotation = myAxisAngleRotation3d;
            myGeometryModel.Transform = myRotateTransform3D;

            // Add the geometry model to the model group.
            myModel3DGroup.Children.Add(myGeometryModel);

            // Add the group of models to the ModelVisual3d.
            myModelVisual3D.Content = myModel3DGroup;

            //
            myViewport3D.Children.Add(myModelVisual3D);

            // Apply the viewport to the page so it will be rendered.
            ModelContent = myViewport3D;
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
                        myNormalCollection.Add(new Vector3D(normal.X, normal.Z, normal.Y));
                        myNormalCollection.Add(new Vector3D(normal.X, normal.Z, normal.Y));
                        myNormalCollection.Add(new Vector3D(normal.X, normal.Z, normal.Y));

                        myPositionCollection.Add(new Point3D(triangle.A.Position.X, triangle.A.Position.Z, triangle.A.Position.Y));
                        myPositionCollection.Add(new Point3D(triangle.B.Position.X, triangle.B.Position.Z, triangle.B.Position.Y));
                        myPositionCollection.Add(new Point3D(triangle.C.Position.X, triangle.C.Position.Z, triangle.C.Position.Y));

                        int aIndex = 0 + 3 * triangleCount;
                        int bIndex = 1 + 3 * triangleCount;
                        int cIndex = 2 + 3 * triangleCount;

                        if (!face.Reversed)
                            (aIndex, bIndex) = (bIndex, aIndex);

                        myTriangleIndicesCollection.Add(aIndex);
                        myTriangleIndicesCollection.Add(bIndex);
                        myTriangleIndicesCollection.Add(cIndex);

                        triangleCount++;
                    }
                }

                /*myNormalCollection.Add(new Vector3D(0, 0, 1));
                myNormalCollection.Add(new Vector3D(0, 0, 1));
                myNormalCollection.Add(new Vector3D(0, 0, 1));
                myNormalCollection.Add(new Vector3D(0, 0, 1));
                myNormalCollection.Add(new Vector3D(0, 0, 1));
                myNormalCollection.Add(new Vector3D(0, 0, 1));

                myPositionCollection.Add(new Point3D(-0.5, -0.5, 0.5));
                myPositionCollection.Add(new Point3D(0.5, -0.5, 0.5));
                myPositionCollection.Add(new Point3D(0.5, 0.5, 0.5));
                myPositionCollection.Add(new Point3D(0.5, 0.5, 0.5));
                myPositionCollection.Add(new Point3D(-0.5, 0.5, 0.5));
                myPositionCollection.Add(new Point3D(-0.5, -0.5, 0.5));

                myTriangleIndicesCollection.Add(0);
                myTriangleIndicesCollection.Add(1);
                myTriangleIndicesCollection.Add(2);
                myTriangleIndicesCollection.Add(3);
                myTriangleIndicesCollection.Add(4);
                myTriangleIndicesCollection.Add(5);*/
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
            myPCamera.Position = new Point3D(0, 0, myPCamera.Position.Z + e.Delta / 240.0);
		}

		public void UpdateModel(Model model)
		{
            this.Model = model;
		}
	}
}
