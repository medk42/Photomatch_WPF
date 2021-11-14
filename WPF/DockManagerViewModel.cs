using Photomatch_ProofOfConcept_WPF.WPF.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.WPF
{
    public class DockManagerViewModel
    {
        public ObservableCollection<ImageViewModel> Documents { get; private set; }

        public ObservableCollection<object> Anchorables { get; private set; }

        public DockManagerViewModel(IEnumerable<ImageViewModel> dockWindowViewModels)
        {
            this.Documents = new ObservableCollection<ImageViewModel>();
            this.Anchorables = new ObservableCollection<object>();

            foreach (var document in dockWindowViewModels)
            {
                AddDocument(document);
            }
        }

        public void AddDocument(ImageViewModel doc)
        {
            if (!doc.IsClosed)
                this.Documents.Add(doc);
        }
    }
}
