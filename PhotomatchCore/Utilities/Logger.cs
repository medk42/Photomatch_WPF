using System.Collections.Generic;

namespace PhotomatchCore.Utilities
{
	/// <summary>
	/// Type of the log message. Loggers may choose to only log certain messages (or display certain types differently and so on).
	/// </summary>
	public enum LogType
	{
		/// <summary>
		/// Should be reported to user visibly.
		/// </summary>
		Error,

		/// <summary>
		/// Should be reported to user visibly.
		/// </summary>
		SevereWarning, 
		
		/// <summary>
		/// Should be reported to user in a non-intrusive way. Less serious warning.
		/// </summary>
		Warning,

		/// <summary>
		/// Information message. Should be reported to user in a non-intrusive way.
		/// </summary>
		Info,

		/// <summary>
		/// Progress message. Should be reported to user in a non-intrusive way.
		/// </summary>
		Progress
	}

	/// <summary>
	/// Interface for any class using or implementing logging.
	/// </summary>
	public interface ILogger
	{
		/// <summary>
		/// Log a message.
		/// </summary>
		/// <param name="title">Message title.</param>
		/// <param name="message">Message content.</param>
		/// <param name="type">Message type.</param>
		void Log(string title, string message, LogType type);
	}

	/// <summary>
	/// Logger that sends logged message to multiple registered loggers.
	/// </summary>
	public class MultiLogger : ILogger
	{
		/// <summary>
		/// All registered ILogger instances will receive logged messaged sent to MultiLogger.
		/// </summary>
		public List<ILogger> Loggers { get; } = new List<ILogger>();

		/// <summary>
		/// Send logged message to all registered ILogger instances.
		/// </summary>
		/// <param name="title">Message title.</param>
		/// <param name="message">Message content.</param>
		/// <param name="type">Message type.</param>
		public void Log(string title, string message, LogType type)
		{
			foreach (var logger in Loggers)
			{
				logger.Log(title, message, type);
			}
		}
	}
}