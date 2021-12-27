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
using Perspective;

namespace Photomatch_ProofOfConcept_WPF
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, MasterGUI
	{
		private static readonly string PhotomatcherProjectFileFilter = "Photomatcher Project Files (*.ppf)|*.ppf";
		private static readonly string ImageFileFilter = "Image Files(*.BMP;*.JPG;*.GIF;*.PNG;*.TIFF)|*.BMP;*.JPG;*.GIF;*.PNG;*.TIFF";
		private static readonly string MainTitle = "Photomatcher";

		private MasterControl AppControl;
		private Actions ActionListener;
		private ILogger Logger = null;
		private MainViewModel MainViewModel;

		public MainWindow()
		{
			InitializeComponent();
			MyMainWindow.Title = MainTitle;
			AppControl = new MasterControl(this);
			ActionListener = AppControl;

			var multiLogger = new MultiLogger();
			multiLogger.Loggers.Add(new StatusStripLogger(StatusText));
			multiLogger.Loggers.Add(new WarningErrorGUILogger());
			Logger = multiLogger;

			MainViewModel = new MainViewModel();
			this.DataContext = MainViewModel;

			MainDockMgr.ActiveContentChanged += MainDockMgr_ActiveContentChanged;
			XInvertedCheckbox.Checked += AnyInvertedCheckbox_Changed;
			YInvertedCheckbox.Checked += AnyInvertedCheckbox_Changed;
			ZInvertedCheckbox.Checked += AnyInvertedCheckbox_Changed;
			XInvertedCheckbox.Unchecked+= AnyInvertedCheckbox_Changed;
			YInvertedCheckbox.Unchecked += AnyInvertedCheckbox_Changed;
			ZInvertedCheckbox.Unchecked += AnyInvertedCheckbox_Changed;
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
			var window = new ImageViewModel(imageWindow, Logger, this) { Title = title };
			MainViewModel.DockManagerViewModel.AddDocument(window);

			MainDockMgr.ActiveContent = window;

			return window;
		}

		public void DisplayProjectName(string projectName)
		{
			MyMainWindow.Title = $"{MainTitle} - {projectName}";
		}

		private void LoadImage_Click(object sender, RoutedEventArgs e) => ActionListener.LoadImage_Pressed();
		private void SaveProject_Click(object sender, RoutedEventArgs e) => ActionListener.SaveProject_Pressed();
		private void SaveProjectAs_Click(object sender, RoutedEventArgs e) => ActionListener.SaveProjectAs_Pressed();
		private void LoadProject_Click(object sender, RoutedEventArgs e) => ActionListener.LoadProject_Pressed();

		private void MainDockMgr_ActiveContentChanged(object sender, EventArgs e)
		{
			if (MainDockMgr.ActiveContent != null)
			{
				AxesComboBox.IsEnabled = true;
				XInvertedCheckbox.IsEnabled = true;
				YInvertedCheckbox.IsEnabled = true;
				ZInvertedCheckbox.IsEnabled = true;

				ImageViewModel imageViewModel = MainDockMgr.ActiveContent as ImageViewModel;
				if (imageViewModel == null)
					throw new Exception("Dock manager should only show " + nameof(ImageViewModel));

				DisplayCalibrationAxes(imageViewModel.CurrentCalibrationAxes);
				DisplayInvertedAxes(imageViewModel.CurrentInvertedAxes);
			}
			else
			{
				AxesComboBox.IsEnabled = false;
				XInvertedCheckbox.IsEnabled = false;
				YInvertedCheckbox.IsEnabled = false;
				ZInvertedCheckbox.IsEnabled = false;
			}
		}

		public void DisplayCalibrationAxes(CalibrationAxes calibrationAxes)
		{
			switch (calibrationAxes)
			{
				case CalibrationAxes.XY:
					AxesComboBox.SelectedIndex = 0;
					break;
				case CalibrationAxes.YX:
					AxesComboBox.SelectedIndex = 1;
					break;
				case CalibrationAxes.XZ:
					AxesComboBox.SelectedIndex = 2;
					break;
				case CalibrationAxes.ZX:
					AxesComboBox.SelectedIndex = 3;
					break;
				case CalibrationAxes.YZ:
					AxesComboBox.SelectedIndex = 4;
					break;
				case CalibrationAxes.ZY:
					AxesComboBox.SelectedIndex = 5;
					break;
				default:
					throw new Exception("Unknown switch case.");
			}
		}

		private void AxesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (MainDockMgr.ActiveContent != null)
			{
				ImageViewModel imageViewModel = MainDockMgr.ActiveContent as ImageViewModel;
				if (imageViewModel == null)
					throw new Exception("Dock manager should only show " + nameof(ImageViewModel));

				CalibrationAxes calibrationAxes;

				switch (AxesComboBox.SelectedIndex)
				{
					case 0:
						calibrationAxes = CalibrationAxes.XY;
						break;
					case 1:
						calibrationAxes = CalibrationAxes.YX;
						break;
					case 2:
						calibrationAxes = CalibrationAxes.XZ;
						break;
					case 3:
						calibrationAxes = CalibrationAxes.ZX;
						break;
					case 4:
						calibrationAxes = CalibrationAxes.YZ;
						break;
					case 5:
						calibrationAxes = CalibrationAxes.ZY;
						break;
					default:
						throw new Exception("Unknown switch case.");
				}

				imageViewModel.CalibrationAxes_Changed(calibrationAxes);
			}
		}

		public void DisplayInvertedAxes(InvertedAxes invertedAxes)
		{
			XInvertedCheckbox.IsChecked = invertedAxes.X;
			YInvertedCheckbox.IsChecked = invertedAxes.Y;
			ZInvertedCheckbox.IsChecked = invertedAxes.Z;
		}

		private void AnyInvertedCheckbox_Changed(object sender, RoutedEventArgs e)
		{
			if (MainDockMgr.ActiveContent != null)
			{
				ImageViewModel imageViewModel = MainDockMgr.ActiveContent as ImageViewModel;
				if (imageViewModel == null)
					throw new Exception("Dock manager should only show " + nameof(ImageViewModel));

				imageViewModel.InvertedAxes_Changed(new InvertedAxes()
				{
					X = XInvertedCheckbox.IsChecked ?? false,
					Y = YInvertedCheckbox.IsChecked ?? false,
					Z = ZInvertedCheckbox.IsChecked ?? false,
				});
			}
		}
	}
}