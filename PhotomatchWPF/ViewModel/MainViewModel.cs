using System;
using System.Collections.Generic;
using System.Text;

namespace PhotomatchWPF.ViewModel
{
	public class MainViewModel
	{
		public DockManagerViewModel DockManagerViewModel { get; private set; }

		public MainViewModel()
		{
			var documents = new List<BaseViewModel>();
			DockManagerViewModel = new DockManagerViewModel(documents);
		}
	}
}
