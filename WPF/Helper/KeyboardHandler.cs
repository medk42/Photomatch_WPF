using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Photomatch_ProofOfConcept_WPF.WPF.Helper
{
	public interface IKeyboardHandler
	{
		void KeyUp(object sender, KeyEventArgs e);
		void KeyDown(object sender, KeyEventArgs e);
	}
}
