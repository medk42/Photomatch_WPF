using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Interop;

using Logging;
using MatrixVector;

using WpfLogging;
using WpfInterfaces;
using WpfGuiElements;
using WpfExtensions;

using GuiInterfaces;
using GuiControls;
using GuiEnums;
using Photomatch_ProofOfConcept_WPF.WPF.ViewModel;

namespace Photomatch_ProofOfConcept_WPF
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, MasterGUI
	{
		private static readonly string PhotomatcherProjectFileFilter = "Photomatcher Project Files (*.ppf)|*.ppf";
		private static readonly string ImageFileFilter = "Image Files(*.BMP;*.JPG;*.GIF;*.PNG;*.TIFF)|*.BMP;*.JPG;*.GIF;*.PNG;*.TIFF";

		private MasterControl AppControl;
		private Actions ActionListener;
		private ILogger Logger = null;
		private MainViewModel MainViewModel;

		public MainWindow()
		{
			InitializeComponent();

			AppControl = new MasterControl(this);
			ActionListener = AppControl;

			var multiLogger = new MultiLogger();
			multiLogger.Loggers.Add(new StatusStripLogger(StatusText));
			multiLogger.Loggers.Add(new WarningErrorGUILogger());
			Logger = multiLogger;

			MainViewModel = new MainViewModel();
			this.DataContext = MainViewModel;
		}

		public void Log(string title, string message, LogType type)
		{
			Logger.Log(title, message, type);
		}

		private string GetFilePath(string filter)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = filter,
				RestoreDirectory = true
			};
			if (openFileDialog.ShowDialog() ?? false)
			{
				return openFileDialog.FileName;
			}

			return null;
		}

		private string SaveFilePath(string filter)
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				Filter = filter,
				RestoreDirectory = true
			};
			if (saveFileDialog.ShowDialog() ?? false)
			{
				return saveFileDialog.FileName;
			}

			return null;
		}

		public string GetImageFilePath() => GetFilePath(ImageFileFilter);

		public string GetSaveProjectFilePath() => SaveFilePath(PhotomatcherProjectFileFilter);

		public string GetLoadProjectFilePath() => GetFilePath(PhotomatcherProjectFileFilter);

		public IWindow CreateImageWindow(ImageWindow imageWindow, string title)
		{
			var window = new ImageViewModel(imageWindow, Logger) { Title = title };
			MainViewModel.DockManagerViewModel.AddDocument(window);

			MainDockMgr.ActiveContent = window;

			return window;
		}

		private void LoadImage_Click(object sender, RoutedEventArgs e) => ActionListener.LoadImage_Pressed();
		private void SaveProject_Click(object sender, RoutedEventArgs e) => ActionListener.SaveProject_Pressed();
		private void SaveProjectAs_Click(object sender, RoutedEventArgs e) => ActionListener.SaveProjectAs_Pressed();
		private void LoadProject_Click(object sender, RoutedEventArgs e) => ActionListener.LoadProject_Pressed();
	}
}