using Photomatch_ProofOfConcept_WPF.WPF.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
            doc.PropertyChanged += DockWindowViewModel_PropertyChanged;
            if (!doc.IsClosed)
                this.Documents.Add(doc);
        }

		private void DockWindowViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ImageViewModel.IsClosed))
			{
                ImageViewModel document = sender as ImageViewModel;

                if (document == null)
				{
                    throw new ArgumentException("sender needs to be of type " + nameof(ImageViewModel));
				}

                if (!document.IsClosed)
				{
                    Documents.Add(document);
				}
                else
				{
                    Documents.Remove(document);
				}
            }
		}
	}
}
