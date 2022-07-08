using PhotomatchCore.Logic;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchCore.Utilities
{
	/// <summary>
	/// Class for solving sets of linear equations.
	/// </summary>
	public static class Solver
	{
		/// <summary>
		/// Solve 3 linear equations. Switch rows around to not have zeros on first column of the first 
		/// row and second column of the second row.
		/// </summary>
		/// <param name="leftHandSide">Each row represents left hand side coefficients in order x/y/z.</param>
		/// <param name="rightHandSide">Each value represents right hand side coefficient.</param>
		/// <returns>Solutions for variables x/y/z.</returns>
		public static Vector3 Solve(Matrix3x3 leftHandSide, Vector3 rightHandSide)
		{
			if (leftHandSide.A00 == 0)
			{
				if (leftHandSide.A10 != 0)
				{
					leftHandSide = new Matrix3x3() { A0_ = leftHandSide.A1_, A1_ = leftHandSide.A0_, A2_ = leftHandSide.A2_ };
					rightHandSide = new Vector3() { X = rightHandSide.Y, Y = rightHandSide.X, Z = rightHandSide.Z };
				}
				else
				{
					leftHandSide = new Matrix3x3() { A0_ = leftHandSide.A2_, A1_ = leftHandSide.A1_, A2_ = leftHandSide.A0_ };
					rightHandSide = new Vector3() { X = rightHandSide.Z, Y = rightHandSide.Y, Z = rightHandSide.X };
				}
			}

			if (leftHandSide.A11 == 0)
			{
				leftHandSide = new Matrix3x3() { A0_ = leftHandSide.A0_, A1_ = leftHandSide.A2_, A2_ = leftHandSide.A1_ };
				rightHandSide = new Vector3() { X = rightHandSide.X, Y = rightHandSide.Z, Z = rightHandSide.Y };
			}

			double sub0, sub1, sub2;

			sub1 = -leftHandSide.A10 / leftHandSide.A00;
			sub2 = -leftHandSide.A20 / leftHandSide.A00;

			leftHandSide.AddToRow(1, sub1 * leftHandSide.A0_);
			leftHandSide.AddToRow(2, sub2 * leftHandSide.A0_);
			rightHandSide.Y += sub1 * rightHandSide.X;
			rightHandSide.Z += sub2 * rightHandSide.X;

			sub0 = -leftHandSide.A01 / leftHandSide.A11;
			sub2 = -leftHandSide.A21 / leftHandSide.A11;
			leftHandSide.AddToRow(0, sub0 * leftHandSide.A1_);
			leftHandSide.AddToRow(2, sub2 * leftHandSide.A1_);
			rightHandSide.X += sub0 * rightHandSide.Y;
			rightHandSide.Z += sub2 * rightHandSide.Y;

			sub0 = -leftHandSide.A02 / leftHandSide.A22;
			sub1 = -leftHandSide.A12 / leftHandSide.A22;
			leftHandSide.AddToRow(0, sub0 * leftHandSide.A2_);
			leftHandSide.AddToRow(1, sub1 * leftHandSide.A2_);
			rightHandSide.X += sub0 * rightHandSide.Z;
			rightHandSide.Y += sub1 * rightHandSide.Z;

			rightHandSide.X /= leftHandSide.A00;
			rightHandSide.Y /= leftHandSide.A11;
			rightHandSide.Z /= leftHandSide.A22;

			return rightHandSide;
		}
	}
}
