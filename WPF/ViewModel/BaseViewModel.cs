using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.WPF.ViewModel
{
	public class BaseViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
