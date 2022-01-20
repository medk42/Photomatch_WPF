using Photomatch_ProofOfConcept_WPF.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Utilities
{
	public static class ImageUtils
	{
        /// <summary>
        /// using https://math.stackexchange.com/questions/296794/finding-the-transform-matrix-from-4-projected-points-with-javascript/339033#339033
        /// </summary>
        public static Matrix3x3 CalculateProjectiveTransformationMatrix(Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight, Vector2 newTopLeft, Vector2 newTopRight, Vector2 newBottomLeft, Vector2 newBottomRight)
		{
            Matrix3x3 map = CalculateMap(topLeft, topRight, bottomLeft, bottomRight);
            Matrix3x3 newMap = CalculateMap(newTopLeft, newTopRight, newBottomLeft, newBottomRight);

            Matrix3x3 remap = newMap * map.Adjugate();
            return remap;
        }

        private static Matrix3x3 CalculateMap(Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight)
		{
            Matrix3x3 leftHandSide = new Matrix3x3()
            {
                A0_ = new Vector3(topLeft.X, topRight.X, bottomLeft.X),
                A1_ = new Vector3(topLeft.Y, topRight.Y, bottomLeft.Y),
                A2_ = new Vector3(1, 1, 1)
            };

            Vector3 rightHandSide = new Vector3(
                bottomRight.X,
                bottomRight.Y,
                1
            );

            Vector3 solution = Solver.Solve(leftHandSide, rightHandSide);

            Matrix3x3 map = new Matrix3x3() { A_0 = solution.X * leftHandSide.A_0, A_1 = solution.Y * leftHandSide.A_1, A_2 = solution.Z * leftHandSide.A_2 };

            return map;
        }
    }
}
