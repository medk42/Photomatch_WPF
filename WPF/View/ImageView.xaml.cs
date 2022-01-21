using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Photomatch_ProofOfConcept_WPF.WPF.View
{
	/// <summary>
	/// Interaction logic for ImageView.xaml
	/// </summary>
	public partial class ImageView : UserControl
	{
		public ImageView()
		{
			InitializeComponent();
		}

		private void Control_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (MainImage.RenderSize.Width > 0)
			{
				UpdateGeometryTransform();
			} 
			else
			{
				MainImage.Loaded += (sender, e) => UpdateGeometryTransform();
			}
		}

		private void UpdateGeometryTransform()
		{
			Matrix transform = GetRectToRectTransform(new Rect(MainImage.RenderSize), new Rect(MainImage.TranslatePoint(new Point(0, 0), XAxisLines), MainViewbox.RenderSize));
			MatrixTransform matrixTransform = new MatrixTransform(transform);
			XAxisLines.Data.Transform = matrixTransform;
			YAxisLines.Data.Transform = matrixTransform;
			ZAxisLines.Data.Transform = matrixTransform;
			ModelLines.Data.Transform = matrixTransform;
			SelectedLines.Data.Transform = matrixTransform;
			FaceLines.Data.Transform = matrixTransform;
			HighlightLines.Data.Transform = matrixTransform;
		}

		/// <summary>
		/// Source: https://stackoverflow.com/questions/724139/invariant-stroke-thickness-of-path-regardless-of-the-scale
		/// </summary>
		private static Matrix GetRectToRectTransform(Rect from, Rect to)
		{
			Matrix transform = Matrix.Identity;
			transform.Translate(-from.X, -from.Y);
			transform.Scale(to.Width / from.Width, to.Height / from.Height);
			transform.Translate(to.X, to.Y);

			return transform;
		}
	}
}
