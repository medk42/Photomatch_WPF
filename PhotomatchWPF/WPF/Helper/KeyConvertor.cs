using PhotomatchCore.Gui;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace PhotomatchWPF.WPF.Helper
{
	public static class KeyConvertor
	{
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
