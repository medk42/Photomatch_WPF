using System.Collections.Generic;

namespace Photomatch_ProofOfConcept_WPF.Utilities
{
	public enum LogType
	{
		Error, Warning, Info
	}

	public interface ILogger
	{
		void Log(string title, string message, LogType type);
	}

	public class MultiLogger : ILogger
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