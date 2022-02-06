using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.WPF.ViewModel
{
    public class DockManagerViewModel
    {
        public ObservableCollection<BaseViewModel> Documents { get; private set; }

        public ObservableCollection<object> Anchorables { get; private set; }

        public DockManagerViewModel(IEnumerable<BaseViewModel> dockWindowViewModels)
        {
            this.Documents = new ObservableCollection<BaseViewModel>();
            this.Anchorables = new ObservableCollection<object>();

            foreach (var document in dockWindowViewModels)
            {
                AddDocument(document);
            }
        }

        public void AddDocument(BaseViewModel doc)
        {
            doc.PropertyChanged += DockWindowViewModel_PropertyChanged;
            if (!doc.IsClosed)
                this.Documents.Add(doc);
        }

		private void DockWindowViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(BaseViewModel.IsClosed))
			{
                BaseViewModel document = sender as BaseViewModel;

                if (document == null)
				{
                    throw new ArgumentException("sender needs to be of type " + nameof(BaseViewModel));
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
