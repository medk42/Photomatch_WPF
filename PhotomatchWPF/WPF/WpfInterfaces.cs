using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchWPF.WPF
{
	/// <summary>
	/// Implemented by objects that want to be updated when some scale changes.
	/// </summary>
	public interface IScalable
	{
		/// <summary>
		/// Called to notify about a scale change.
		/// </summary>
		void SetNewScale(double scale);
	}
}
