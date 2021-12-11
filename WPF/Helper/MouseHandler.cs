using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Photomatch_ProofOfConcept_WPF.WPF.Helper
{
	public interface IMouseHandler
	{
		void MouseDown(object sender, MouseButtonEventArgs e);
		void MouseUp(object sender, MouseButtonEventArgs e);
		void MouseMove(object sender, MouseEventArgs e);
	}
}
