using PhotomatchCore.Gui.GuiControls.Helper;
using PhotomatchCore.Logic;
using PhotomatchCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using PhotomatchCore.Logic.Model;

namespace PhotomatchCore.Gui.GuiControls.ToolHandlers.ModelCreationToolHandlers
{

	/// <summary>
	/// Class for handling vertex/edge/face deletion.
	/// </summary>
	public class ModelCreationDeleteHandler : BaseModelCreationToolHandler
	{
		public override ModelCreationTool ToolType => ModelCreationTool.Delete;

		private ModelVisualization ModelVisualization;

		private ILine HoverEdge = null;
		private IPolygon HoverFace = null;

		/// <param name="modelVisualization">Handler displays the model.</param>
		public ModelCreationDeleteHandler(ModelVisualization modelVisualization)
		{
			ModelVisualization = modelVisualization;

			Active = false;
			SetActive(Active);
		}


		/// <summary>
		/// Reset the color of selected edge.
		/// </summary>
		private void ResetHoverEdge()
		{
			if (HoverEdge != null)
			{
				HoverEdge.Color = ApplicationColor.Model;
				HoverEdge = null;
			}
		}

		/// <summary>
		/// Reset the color of selected face.
		/// </summary>
		private void ResetHoverFace()
		{
			if (HoverFace != null)
			{
				HoverFace.Color = ApplicationColor.Face;
				HoverFace = null;
			}
		}

		/// <summary>
		/// Visualize vertex/edge/face under mouse. Remember selected edge/face so we
		/// can change the color back later.
		/// </summary>
		public override void MouseMove(Vector2 mouseCoord)
		{
			if (Active)
			{
				if (ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord))
				{
					ResetHoverEdge();
					ResetHoverFace();
					return;
				}

				ILine foundEdge = ModelVisualization.GetEdgeUnderMouse(mouseCoord)?.Item2;
				if (foundEdge != null)
				{
					if (foundEdge != HoverEdge)
					{
						ResetHoverEdge();
						HoverEdge = foundEdge;
						foundEdge.Color = ApplicationColor.Highlight;
					}
					ResetHoverFace();
					return;
				}
				else
					ResetHoverEdge();

				IPolygon foundPolygon = ModelVisualization.GetFaceUnderMouse(mouseCoord)?.Item2;
				if (foundPolygon != null)
				{
					if (foundPolygon != HoverFace)
					{
						ResetHoverFace();
						HoverFace = foundPolygon;
						foundPolygon.Color = ApplicationColor.Highlight;
					}
				}
				else
					ResetHoverFace();
			}
		}

		/// <summary>
		/// Delete the vertex/edge/face under mouse on left click.
		/// </summary>
		public override void MouseDown(Vector2 mouseCoord, MouseButton button)
		{
			if (Active)
			{
				if (button != MouseButton.Left)
					return;

				Vertex foundPoint = ModelVisualization.GetVertexUnderMouse(mouseCoord).Item1;

				if (foundPoint != null)
				{
					foundPoint.Remove();
					ModelVisualization.ModelHoverEllipse.MouseEvent(mouseCoord);
					return;
				}

				Edge foundEdge = ModelVisualization.GetEdgeUnderMouse(mouseCoord)?.Item1;
				if (foundEdge != null)
				{
					ResetHoverEdge();
					foundEdge.Remove();
					return;
				}

				Face foundFace = ModelVisualization.GetFaceUnderMouse(mouseCoord)?.Item1;
				if (foundFace != null)
				{
					ResetHoverFace();
					foundFace.Remove();
					return;
				}
			}
		}

		internal override void SetActive(bool active)
		{
			ModelVisualization.ModelHoverEllipse.Active = active;

			if (!active)
			{
				ResetHoverEdge();
				ResetHoverFace();
			}
		}
	}
}
