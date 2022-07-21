using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PhotomatchCore.Utilities;

namespace PhotomatchWPF.Helper
{
	/// <summary>
	/// Class which implements logging with a pop-up window.
	/// </summary>
	public class WarningErrorGUILogger : ILogger
	{
		/// <summary>
		/// Log Error and SevereWarning messages with a pop-up window.
		/// </summary>
		public void Log(string title, string message, LogType type)
		{
			switch (type)
			{
				case LogType.Error:
					MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
					break;
				case LogType.SevereWarning:
					MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
					break;
				case LogType.Info:
					break;
				case LogType.Progress:
					break;
				case LogType.Warning:
					break;
				default:
					throw new Exception("Unknown enum type");
			}
		}
	}

	/// <summary>
	/// Class which implements logging with a TextBlock.
	/// </summary>
	public class StatusStripLogger : ILogger
	{
		private readonly TextBlock StatusLabel = null;
		private System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();

		/// <param name="strip">TextBlock to show messages in.</param>
		public StatusStripLogger(TextBlock strip)
		{
			StatusLabel = strip;

			timer.Interval = new TimeSpan(hours: 0, minutes: 0, seconds: 5);
			timer.Tick += Timer_Tick;
		}

		/// <summary>
		/// Log SevereWarning, Warning and Progress messages with red, bold font and Error and Info with black font.
		/// Hide message after 5s (unless type is Progress).
		/// </summary>
		public void Log(string title, string message, LogType type)
		{
			StatusLabel.Foreground = Brushes.Black;
			StatusLabel.FontWeight = FontWeights.Normal;

			switch (type)
			{
				case LogType.Error:
					message = $"ERROR ({title}): {message}";
					break;
				case LogType.SevereWarning:
					message = $"WARNING ({title}): {message}";
					StatusLabel.Foreground = Brushes.Red;
					StatusLabel.FontWeight = FontWeights.Bold;
					break;
				case LogType.Warning:
					message = $"WARNING ({title}): {message}";
					StatusLabel.Foreground = Brushes.Red;
					StatusLabel.FontWeight = FontWeights.Bold;
					break;
				case LogType.Info:
					message = $"INFO ({title}): {message}";
					break;
				case LogType.Progress:
					message = $"PROGRESS ({title}): {message}";
					StatusLabel.Foreground = Brushes.Red;
					StatusLabel.FontWeight = FontWeights.Bold;
					break;
				default:
					throw new Exception("Unknown enum type");
			}

			StatusLabel.Text = message;
			timer.Stop();

			if (type != LogType.Progress)
				timer.Start();
		}

		private void Timer_Tick(object sender, EventArgs e)
		{
			StatusLabel.Text = "";
			timer.Stop();
		}
	}
}