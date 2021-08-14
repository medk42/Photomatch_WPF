using System.Collections.Generic;

namespace Logging
{
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
}