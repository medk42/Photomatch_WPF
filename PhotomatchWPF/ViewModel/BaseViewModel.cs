using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace PhotomatchWPF.ViewModel
{
	public class BaseViewModel : INotifyPropertyChanged
	{
		private bool CanClose_;
		public bool CanClose
		{
			get => CanClose_;
			set
			{
				if (CanClose_ != value)
				{
					CanClose_ = value;
					OnPropertyChanged(nameof(CanClose));
				}
			}
		}

		private string Title_;
		public string Title
		{
			get => Title_;
			set
			{
				if (Title_ != value)
				{
					Title_ = value;
					OnPropertyChanged(nameof(Title));
				}
			}
		}

		private bool _IsClosed;
		public bool IsClosed
		{
			get => _IsClosed;
			private protected set
			{
				if (_IsClosed != value)
				{
					_IsClosed = value;
					OnPropertyChanged(nameof(IsClosed));
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
