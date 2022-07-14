using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace PhotomatchWPF.ViewModel.Helper
{
	public interface IKeyboardHandler
	{
		void KeyUp(object sender, KeyEventArgs e);
		void KeyDown(object sender, KeyEventArgs e);
	}
}
