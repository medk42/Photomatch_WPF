using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace PhotomatchWPF.ViewModel.Helper
{
	public interface IMouseHandler
	{
		void MouseDown(object sender, MouseButtonEventArgs e);
		void MouseUp(object sender, MouseButtonEventArgs e);
		void MouseMove(object sender, MouseEventArgs e);
		void MouseEnter(object sender, MouseEventArgs e);
		void MouseLeave(object sender, MouseEventArgs e);
		void MouseWheel(object sender, MouseWheelEventArgs e);
	}
}
