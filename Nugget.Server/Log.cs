using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Nugget.Server
{
    [Flags]
    public enum LogLevels
    {
        None,
        Error,
        Info,
        Debug,
        Warning,
    }

    public static class Log
    {
        static TextWriter _logStream = Console.Out;
        static TextWriter LogStream { get { return _logStream; } }
        public const LogLevels Level = LogLevels.Error | LogLevels.Debug | LogLevels.Info | LogLevels.Warning;

        public static void Warn(string message)
        {
            LogLine(LogLevels.Warning, String.Format("{0} {1} {2}", DateTime.Now, LogLevels.Warning.ToString(), message));
        }

        public static void Error(string message)
        {
            LogLine(LogLevels.Error, String.Format("{0} {1} {2}", DateTime.Now, LogLevels.Error.ToString(), message));
        }

        public static void Debug(string message)
        {
            LogLine(LogLevels.Debug, String.Format("{0} {1} {2}", DateTime.Now, LogLevels.Debug.ToString(), message));
        }

        public static void Info(string message)
        {
            LogLine(LogLevels.Info, String.Format("{0} {1} {2}", DateTime.Now, LogLevels.Info.ToString(), message));
        }

        private static void LogLine(LogLevels level, string message)
        {
            if ((Level & level) == level)
            {
                LogStream.WriteLine(message);
            }
        }
    }
}
