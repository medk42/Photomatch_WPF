using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch.WPF.ViewModel
{
    public class MainViewModel
    {
        public DockManagerViewModel DockManagerViewModel { get; private set; }

        public MainViewModel()
        {
            var documents = new List<BaseViewModel>();
            this.DockManagerViewModel = new DockManagerViewModel(documents);
        }
    }
}
