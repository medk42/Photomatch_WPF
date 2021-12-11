using System;
using System.Collections.Generic;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.WPF.ViewModel
{
    public class MainViewModel
    {
        public DockManagerViewModel DockManagerViewModel { get; private set; }

        public MainViewModel()
        {
            var documents = new List<ImageViewModel>();
            this.DockManagerViewModel = new DockManagerViewModel(documents);
        }
    }
}
