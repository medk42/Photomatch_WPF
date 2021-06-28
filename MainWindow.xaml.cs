using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Photomatch_ProofOfConcept_WPF
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private ILogger Logger = null;
		private List<Perspective> perspectives = new List<Perspective>();

		public MainWindow()
		{
			InitializeComponent();

			var multiLogger = new MultiLogger();
			multiLogger.Loggers.Add(new StatusStripLogger(StatusText));
			multiLogger.Loggers.Add(new WarningErrorGUILogger());
			Logger = multiLogger;
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			string filePath = null;
			BitmapImage image = null;

			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Image Files (*.BMP;*.JPG;*.GIF;*.PNG;*.TIFF)|*.BMP;*.JPG;*.GIF;*.PNG;*.TIFF";
			openFileDialog.RestoreDirectory = true;
			if (openFileDialog.ShowDialog() ?? false)
			{
				filePath = openFileDialog.FileName;
			}

			if (filePath == null) // user closed dialog
			{
				Logger.Log("Load Image", "User closed dialog before selecting file.", LogType.Info);
			}
			else
			{
				try
				{
					image = new BitmapImage(new Uri(filePath));
				}
				catch (Exception ex)
				{
					if (ex is FileNotFoundException)
						Logger.Log("Load Image", "File not found.", LogType.Warning);
					else if (ex is NotSupportedException)
						Logger.Log("Load Image", "Incorrect or unsupported image format.", LogType.Warning);
					else
						throw ex;
				}
			}

			if (image != null)
			{
				Logger.Log("Load Image", "File loaded successfully.", LogType.Info);

				var perspective = new Perspective(image);
				perspectives.Add(perspective);
				perspective.Apply(MainImage);
			}
		}


		// testing  stuff START

		List<Line> lines = new List<Line>();

		Line line = null;

		private void MainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				Point eventPos = e.GetPosition(MainCanvas);

				line = new Line();
				line.Stroke = Brushes.Red;
				line.X1 = eventPos.X;
				line.Y1 = eventPos.Y;
				line.X2 = eventPos.X;
				line.Y2 = eventPos.Y;
				line.StrokeThickness = MainCanvas.ActualWidth / MainViewbox.ActualWidth;

				MainCanvas.Children.Add(line);
				lines.Add(line);
			}

			Logger.Log("Mouse Event", $"Mouse Down at {e.GetPosition(MainCanvas).X}, {e.GetPosition(MainCanvas).Y} by {e.ChangedButton}", LogType.Info);
		}

		private void MainCanvas_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left && line != null)
			{
				Point eventPos = e.GetPosition(MainCanvas);

				line.X2 = eventPos.X;
				line.Y2 = eventPos.Y;

				line = null;
			}

			Logger.Log("Mouse Event", $"Mouse Up at {e.GetPosition(MainCanvas).X}, {e.GetPosition(MainCanvas).Y} by {e.ChangedButton}", LogType.Info);
		}

		private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
		{
			if (line != null)
			{
				Point eventPos = e.GetPosition(MainCanvas);

				line.X2 = eventPos.X;
				line.Y2 = eventPos.Y;
			}

			Logger.Log("Mouse Event", $"Mouse Move at {e.GetPosition(MainCanvas).X}, {e.GetPosition(MainCanvas).Y} (MainCanvas) and at {e.GetPosition(MainImage).X}, {e.GetPosition(MainImage).Y} (MainImage)", LogType.Info);
		}

		private void MainViewbox_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Logger.Log("Size Change Event", $"{MainCanvas.RenderSize} => {MainViewbox.RenderSize}", LogType.Info);
			foreach (var line in lines)
			{
				line.StrokeThickness = MainCanvas.ActualWidth / MainViewbox.ActualWidth;
			}
		}
	}

	enum LogType
	{
		Error, Warning, Info
	}

	interface ILogger
	{
		void Log(string title, string message, LogType type);
	}

	class MultiLogger : ILogger
	{
		public List<ILogger> Loggers { get; } = new List<ILogger>();

		public void Log(string title, string message, LogType type)
		{
			foreach (var logger in Loggers)
			{
				logger.Log(title, message, type);
			}
		}
	}

	class WarningErrorGUILogger : ILogger
	{
		public void Log(string title, string message, LogType type)
		{
			switch (type)
			{
				case LogType.Error:
					MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
					break;
				case LogType.Warning:
					MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
					break;
				case LogType.Info:
					break;
				default:
					throw new Exception("Unknown enum type");
			}
		}
	}

	class StatusStripLogger : ILogger
	{
		private readonly TextBlock StatusLabel = null;
		private System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();

		public StatusStripLogger(TextBlock strip)
		{
			StatusLabel = strip;

			timer.Interval = new TimeSpan(hours: 0, minutes: 0, seconds: 5);
			timer.Tick += Timer_Tick;
		}

		public void Log(string title, string message, LogType type)
		{
			switch (type)
			{
				case LogType.Error:
					message = $"ERROR ({title}): {message}";
					break;
				case LogType.Warning:
					message = $"WARNING ({title}): {message}";
					break;
				case LogType.Info:
					message = $"INFO ({title}): {message}";
					break;
				default:
					throw new Exception("Unknown enum type");
			}

			StatusLabel.Text = message;
			timer.Stop();
			timer.Start();
		}

		private void Timer_Tick(object sender, EventArgs e)
		{
			StatusLabel.Text = "";
			timer.Stop();
		}
	}

	class Perspective // should contain image and perspective data
	{
		public BitmapImage Image { get; }

		public Perspective(BitmapImage image)
		{
			Image = image;
		}

		public void Apply(Image imageGUI)
		{
			imageGUI.Source = Image;
		}
	}
}
