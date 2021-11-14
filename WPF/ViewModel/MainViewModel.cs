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

            for (int i = 0; i < 6; i++)
                documents.Add(new ImageViewModel(null) { Title = "Sample " + i.ToString() });

            this.DockManagerViewModel = new DockManagerViewModel(documents);
        }
    }
}
