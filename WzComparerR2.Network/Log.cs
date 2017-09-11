using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.Network
{
    public class Log
    {
        public static IList<ILogger> Loggers { get; private set; } = new List<ILogger>();

        public static void Debug(string format, params object[] args)
        {
            Log.Write(LogLevel.Debug, format, args);
        }

        public static void Info(string format, params object[] args)
        {
            Log.Write(LogLevel.Info, format, args);
        }

        public static void Warn(string format, params object[] args)
        {
            Log.Write(LogLevel.Warn, format, args);
        }

        public static void Error(string format, params object[] args)
        {
            Log.Write(LogLevel.Error, format, args);
        }

        public static void Write(LogLevel logLevel, string format, params object[] args)
        {
            foreach(var logger in Loggers)
            {
                try
                {
                    lock (logger)
                    {
                        logger.Write(logLevel, format, args);
                    }
                }
                catch
                {
                }
            }
        }
    }

    public interface ILogger
    {
        void Write(LogLevel logLevel, string format, params object[] args);
    }

    public enum LogLevel
    {
        All = 0,
        Debug,
        Info,
        Warn,
        Error,
        None,
    }
}
