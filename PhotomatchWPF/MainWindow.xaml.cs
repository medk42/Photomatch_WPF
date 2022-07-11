using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;

using PhotomatchWPF.WPF;
using PhotomatchWPF.WPF.ViewModel;
using PhotomatchWPF.WPF.Helper;
using PhotomatchCore.Gui;
using PhotomatchCore.Gui.GuiControls;
using System.ComponentModel;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Input;
using PhotomatchCore.Utilities;
using PhotomatchCore.Logic.Perspective;
using PhotomatchCore.Logic.Model;

namespace PhotomatchWPF
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, IMasterView
	{
		private static readonly string PhotomatcherProjectFileFilter = "Photomatcher Project Files (*.ppf)|*.ppf";
		private static readonly string ImageFileFilter = "Image Files(*.BMP;*.JPG;*.JPEG;*.GIF;*.PNG;*.TIFF)|*.BMP;*.JPG;*.JPEG;*.GIF;*.PNG;*.TIFF";
		private static readonly string ModelExportFileFilter = "3D Files(*.obj)|*.obj";
		private static readonly string MainTitle = "Photomatcher";

		private IMasterControlActions ActionListener;
		private ILogger Logger = null;
		private MainViewModel MainViewModel;
		private bool InvertedAxesCheckboxIgnore = false;
		private BackgroundWorker CurrentBackgroundWorker = null;
		private AutoResetEvent CurrentBackgroundWorkerResetEvent = new AutoResetEvent(false);

		private HashSet<Key> PressedKeys = new HashSet<Key>();

		private bool Active_ = true;
		private bool Active
		{
			get => Active_;
			set {
				if (Active_ != value)
				{
					Active_ = value;
					SetActive(value);
				}
			}
		}

		public MainWindow()
		{
			InitializeComponent();
			MyMainWindow.Title = MainTitle;

			var multiLogger = new MultiLogger();
			multiLogger.Loggers.Add(new StatusStripLogger(StatusText));
			multiLogger.Loggers.Add(new WarningErrorGUILogger());
			Logger = multiLogger;

			MainViewModel = new MainViewModel();
			this.DataContext = MainViewModel;

			ActionListener = new MasterControl(this);

			MainDockMgr.ActiveContentChanged += MainDockMgr_ActiveContentChanged;
			XInvertedCheckbox.Checked += AnyInvertedCheckbox_Changed;
			YInvertedCheckbox.Checked += AnyInvertedCheckbox_Changed;
			ZInvertedCheckbox.Checked += AnyInvertedCheckbox_Changed;
			XInvertedCheckbox.Unchecked += AnyInvertedCheckbox_Changed;
			YInvertedCheckbox.Unchecked += AnyInvertedCheckbox_Changed;
			ZInvertedCheckbox.Unchecked += AnyInvertedCheckbox_Changed;
		}

		private void RunBackgroundChecked(Action action)
		{
			if (CurrentBackgroundWorker != null)
				CurrentBackgroundWorker.ReportProgress(0, action);
			else
				action();
		}

		public void Log(string title, string message, LogType type)
		{
			RunBackgroundChecked(() => {
				Logger.Log(title, message, type);
			});
		}

		private void SetActive(bool active)
		{
			MainToolbar.IsEnabled = active;
			MainMenu.IsEnabled = active;
			MainDockMgr.IsEnabled = active;
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
		public string GetModelExportFilePath() => SaveFilePath(ModelExportFileFilter);

		public IImageView CreateImageWindow(ImageWindow imageWindow, string title)
		{
			var window = new ImageViewModel(imageWindow, Logger, this) { Title = title };
			MainViewModel.DockManagerViewModel.AddDocument(window);

			MainDockMgr.ActiveContent = window;

			return window;
		}

		public IModelView CreateModelWindow(Model model)
		{
			ModelViewModel modelView = new ModelViewModel(model) { Title = "Model visualization", CanClose = false };
			MainViewModel.DockManagerViewModel.AddDocument(modelView);

			return modelView;
		}

		public void DisplayProjectName(string projectName)
		{
			RunBackgroundChecked(() => {
				MyMainWindow.Title = $"{MainTitle} - {projectName}";
			});
		}

		private void LoadImage_Click(object sender, RoutedEventArgs e) => ActionListener?.LoadImage_Pressed();
		private void NewProject_Click(object sender, RoutedEventArgs e) => ActionListener?.NewProject_Pressed();
		private void SaveProject_Click(object sender, RoutedEventArgs e) => RunAtBackground(() => ActionListener?.SaveProject_Pressed());
		private void SaveProjectAs_Click(object sender, RoutedEventArgs e) => RunAtBackground(() => ActionListener?.SaveProjectAs_Pressed());
		private void LoadProject_Click(object sender, RoutedEventArgs e) => ActionListener?.LoadProject_Pressed();
		private void ExportModel_Click(object sender, RoutedEventArgs e) => RunAtBackground(() => ActionListener?.ExportModel_Pressed());
		private void Undo_Click(object sender, RoutedEventArgs e) => ActionListener?.Undo_Pressed();
		private void Redo_Click(object sender, RoutedEventArgs e) => ActionListener?.Redo_Pressed();
		private void CameraRadioButton_Checked(object sender, RoutedEventArgs e) => ActionListener?.DesignTool_Changed(DesignTool.CameraCalibration);
		private void CameraModelRadioButton_Checked(object sender, RoutedEventArgs e) => ActionListener?.DesignTool_Changed(DesignTool.CameraModelCalibration);
		private void ModelRadioButton_Checked(object sender, RoutedEventArgs e) => ActionListener?.DesignTool_Changed(DesignTool.ModelCreation);
		private void DeleteRadioButton_Checked(object sender, RoutedEventArgs e) => ActionListener?.ModelCreationTool_Changed(ModelCreationTool.Delete);
		private void EdgeRadioButton_Checked(object sender, RoutedEventArgs e) => ActionListener?.ModelCreationTool_Changed(ModelCreationTool.Edge);
		private void TriangleFaceRadioButton_Checked(object sender, RoutedEventArgs e) => ActionListener?.ModelCreationTool_Changed(ModelCreationTool.TriangleFace);
		private void ComplexFaceRadioButton_Checked(object sender, RoutedEventArgs e) => ActionListener?.ModelCreationTool_Changed(ModelCreationTool.ComplexFace);
		private void FaceNormalRadioButton_Checked(object sender, RoutedEventArgs e) => ActionListener?.ModelCreationTool_Changed(ModelCreationTool.FaceNormals);
		private void CalibrateOriginRadioButton_Checked(object sender, RoutedEventArgs e) => ActionListener?.CameraModelCalibrationTool_Changed(CameraModelCalibrationTool.CalibrateOrigin);
		private void CalibrateScaleRadioButton_Checked(object sender, RoutedEventArgs e) => ActionListener?.CameraModelCalibrationTool_Changed(CameraModelCalibrationTool.CalibrateScale);

		private void RunAtBackground(Action action)
		{
			CurrentBackgroundWorker = new BackgroundWorker() { WorkerReportsProgress = true };
			CurrentBackgroundWorker.DoWork += (sender, e) =>
			{
				action();
				CurrentBackgroundWorkerResetEvent.Set();
			};
			CurrentBackgroundWorker.ProgressChanged += (sender, e) => (e.UserState as Action)();
			CurrentBackgroundWorker.RunWorkerCompleted += (sender, e) =>
			{
				CurrentBackgroundWorker = null;
				Active = true;
			};

			Active = false;
			CurrentBackgroundWorker.RunWorkerAsync();
		}

		private void MainDockMgr_ActiveContentChanged(object sender, EventArgs e)
		{
			if (MainDockMgr.ActiveContent != null)
			{
				MainToolbar.IsEnabled = true;
				
				ImageViewModel imageViewModel = MainDockMgr.ActiveContent as ImageViewModel;
				if (imageViewModel != null)
				{
					DisplayCalibrationAxes(imageViewModel, imageViewModel.CurrentCalibrationAxes);
					DisplayInvertedAxes(imageViewModel, imageViewModel.CurrentCalibrationAxes, imageViewModel.CurrentInvertedAxes);

					CameraCalibrationTools.IsEnabled = true;
				}
				else
				{
					CameraCalibrationTools.IsEnabled = false;
				}

				if (MainViewModel.DockManagerViewModel.Documents.Count == 1)
				{
					MainToolbar.IsEnabled = false;
				}
			}
			else
			{
				MainToolbar.IsEnabled = false;
			}
		}

		public void DisplayCalibrationAxes(ImageViewModel source, CalibrationAxes calibrationAxes)
		{
			if (source != MainDockMgr.ActiveContent)
				return;

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

		public void DisplayInvertedAxes(ImageViewModel source, CalibrationAxes calibrationAxes, InvertedAxes invertedAxes)
		{
			if (source != MainDockMgr.ActiveContent)
				return;

			InvertedAxesCheckboxIgnore = true;

			XInvertedCheckbox.IsChecked = invertedAxes.X;
			YInvertedCheckbox.IsChecked = invertedAxes.Y;
			ZInvertedCheckbox.IsChecked = invertedAxes.Z;
			XInvertedCheckbox.IsEnabled = true;
			YInvertedCheckbox.IsEnabled = true;
			ZInvertedCheckbox.IsEnabled = true;

			switch (calibrationAxes)
			{
				case CalibrationAxes.XY:
				case CalibrationAxes.YX:
					ZInvertedCheckbox.IsEnabled = false;
					ZInvertedCheckbox.IsChecked = false;
					break;
				case CalibrationAxes.XZ:
				case CalibrationAxes.ZX:
					YInvertedCheckbox.IsEnabled = false;
					YInvertedCheckbox.IsChecked = false;
					break;
				case CalibrationAxes.YZ:
				case CalibrationAxes.ZY:
					XInvertedCheckbox.IsEnabled = false;
					XInvertedCheckbox.IsChecked = false;
					break;
				default:
					InvertedAxesCheckboxIgnore = false;
					throw new Exception("Unknown switch case.");
			}

			InvertedAxesCheckboxIgnore = false;
		}

		public void DisplayDesignTool(DesignTool designTool)
		{
			CameraCalibrationTools.Visibility = Visibility.Collapsed;
			ModelCreationTools.Visibility = Visibility.Collapsed;
			CameraModelCalibrationTools.Visibility = Visibility.Collapsed;

			switch (designTool)
			{
				case DesignTool.CameraCalibration:
					CameraRadioButton.IsChecked = true;
					CameraCalibrationTools.Visibility = Visibility.Visible;
					break;
				case DesignTool.CameraModelCalibration:
					CameraModelRadioButton.IsChecked = true;
					CameraModelCalibrationTools.Visibility = Visibility.Visible;
					break;
				case DesignTool.ModelCreation:
					ModelRadioButton.IsChecked = true;
					ModelCreationTools.Visibility = Visibility.Visible;
					break;
				default:
					throw new Exception("Unknown switch case.");
			}
		}

		public void DisplayModelCreationTool(ModelCreationTool modelCreationTool)
		{
			switch (modelCreationTool)
			{
				case ModelCreationTool.Delete:
					DeleteRadioButton.IsChecked = true;
					break;
				case ModelCreationTool.Edge:
					EdgeRadioButton.IsChecked = true;
					break;
				case ModelCreationTool.TriangleFace:
					TriangleFaceRadioButton.IsChecked = true;
					break;
				case ModelCreationTool.ComplexFace:
					ComplexFaceRadioButton.IsChecked = true;
					break;
				case ModelCreationTool.FaceNormals:
					FaceNormalRadioButton.IsChecked = true;
					break;
				default:
					throw new Exception("Unknown switch case.");
			}
		}

		public void DisplayCameraModelCalibrationTool(CameraModelCalibrationTool cameraModelCalibrationTool)
		{
			switch (cameraModelCalibrationTool)
			{
				case CameraModelCalibrationTool.CalibrateOrigin:
					CalibrateOriginRadioButton.IsChecked = true;
					break;
				case CameraModelCalibrationTool.CalibrateScale:
					CalibrateScaleRadioButton.IsChecked = true;
					break;
				default:
					throw new Exception("Unknown switch case.");
			}
		}

		public bool DisplayWarningProceedMessage(string title, string message)
		{
			return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
		}

		private void AnyInvertedCheckbox_Changed(object sender, RoutedEventArgs e)
		{
			if (MainDockMgr.ActiveContent != null && !InvertedAxesCheckboxIgnore)
			{
				ImageViewModel imageViewModel = MainDockMgr.ActiveContent as ImageViewModel;
				if (imageViewModel == null)
					throw new Exception("Dock manager should only show " + nameof(ImageViewModel));

				InvertedAxes invertedAxes = new InvertedAxes()
				{
					X = XInvertedCheckbox.IsChecked ?? false,
					Y = YInvertedCheckbox.IsChecked ?? false,
					Z = ZInvertedCheckbox.IsChecked ?? false,
				};

				switch (imageViewModel.CurrentCalibrationAxes)
				{
					case CalibrationAxes.XY:
					case CalibrationAxes.YX:
						invertedAxes = new InvertedAxes() { X = invertedAxes.X, Y = invertedAxes.Y, Z = imageViewModel.CurrentInvertedAxes.Z };
						break;
					case CalibrationAxes.XZ:
					case CalibrationAxes.ZX:
						invertedAxes = new InvertedAxes() { X = invertedAxes.X, Y = imageViewModel.CurrentInvertedAxes.Y, Z = invertedAxes.Z };
						break;
					case CalibrationAxes.YZ:
					case CalibrationAxes.ZY:
						invertedAxes = new InvertedAxes() { X = imageViewModel.CurrentInvertedAxes.X, Y = invertedAxes.Y, Z = invertedAxes.Z };
						break;
					default:
						throw new Exception("Unknown switch case.");
				}

				imageViewModel.InvertedAxes_Changed(invertedAxes);
			}
		}

		private void MyMainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (!PressedKeys.Contains(e.Key))
			{
				PressedKeys.Add(e.Key);

				KeyboardKey? key = e.Key.AsKeyboardKey();
				if (key.HasValue)
					ActionListener.KeyDown(key.Value);

				if (MainDockMgr.ActiveContent != null)
				{
					IKeyboardHandler keyboardHandler = MainDockMgr.ActiveContent as IKeyboardHandler;
					if (keyboardHandler == null)
						throw new Exception("Active window doesn't implement " + nameof(IKeyboardHandler));
					keyboardHandler.KeyDown(sender, e);
				}
			}
		}

		private void MyMainWindow_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
		{
			PressedKeys.Remove(e.Key);

			KeyboardKey? key = e.Key.AsKeyboardKey();
			if (key.HasValue)
				ActionListener.KeyUp(key.Value);

			if (MainDockMgr.ActiveContent != null)
			{
				IKeyboardHandler keyboardHandler = MainDockMgr.ActiveContent as IKeyboardHandler;
				if (keyboardHandler == null)
					throw new Exception("Active window doesn't implement " + nameof(IKeyboardHandler));
				keyboardHandler.KeyUp(sender, e);
			}
		}

		private void MyMainWindow_Closing(object sender, CancelEventArgs e)
		{
			if (CurrentBackgroundWorker != null)
				CurrentBackgroundWorkerResetEvent.WaitOne();					
			ActionListener.Exit_Pressed();
		}
	}
}