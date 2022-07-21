using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace PhotomatchWPF.ViewModel
{
	/// <summary>
	/// Class representing a basic window with properties and interface for AvalonDock.
	/// </summary>
	public class BaseViewModel : INotifyPropertyChanged
	{
		/// <summary>
		/// Can the window be closed.
		/// </summary>
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
		private bool CanClose_;

		/// <summary>
		/// Title of the window.
		/// </summary>
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
		private string Title_;

		/// <summary>
		/// Is the window closed.
		/// </summary>
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
		private bool _IsClosed;

		/// <summary>
		/// Event that should be invoked on any change of a property that should be reflected in the AvalonDock View layer.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Method for easier invocation of PropertyChanged event.
		/// </summary>
		/// <param name="propertyName">Name of the changed property.</param>
		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
