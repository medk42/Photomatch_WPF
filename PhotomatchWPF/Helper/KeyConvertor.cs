using PhotomatchCore.Gui;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace PhotomatchWPF.Helper
{
	/// <summary>
	/// Class for converting keys from WPF to the format used by PhotomatchCore.
	/// </summary>
	public static class KeyConvertor
	{
		/// <summary>
		/// Convert System.Windows.Input.Key to PhotomatchCore.Gui.KeyboardKey.
		/// </summary>
		public static KeyboardKey? AsKeyboardKey(this Key key)
		{
			switch (key)
			{
				case Key.LeftShift:
					return KeyboardKey.LeftShift;
				case Key.Escape:
					return KeyboardKey.Escape;
				case Key.LeftCtrl:
					return KeyboardKey.LeftCtrl;
				case Key.Z:
					return KeyboardKey.Z;
				case Key.Y:
					return KeyboardKey.Y;
				case Key.S:
					return KeyboardKey.S;
				default:
					return null;
			}
		}
	}
}
