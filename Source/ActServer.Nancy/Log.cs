using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainbowMage.ActServer.Nancy
{
    public interface ILog
    {
        void Fatal(string format, params object[] args);
        void Error(string format, params object[] args);
        void Warning(string format, params object[] args);
        void Info(string format, params object[] args);
        void Debug(string format, params object[] args);
        void Trace(string format, params object[] args);
        event EventHandler<LogEventArgs> OnLog;
    }

    public class Log : ILog
    {
        public void Fatal(string format, params object[] args)
        {
            Write(LogLevel.Fatal, format, args);
        }

        public void Error(string format, params object[] args)
        {
            Write(LogLevel.Error, format, args);
        }

        public void Warning(string format, params object[] args)
        {
            Write(LogLevel.Warning, format, args);
        }

        public void Info(string format, params object[] args)
        {
            Write(LogLevel.Info, format, args);
        }

        public void Debug(string format, params object[] args)
        {
            Write(LogLevel.Debug, format, args);
        }

        public void Trace(string format, params object[] args)
        {
            Write(LogLevel.Trace, format, args);
        }

        private void Write(LogLevel level, string format, params object[] args)
        {
            var now = DateTime.Now;
            var message = string.Format(format, args);
            var log = new LogEntry(level, message, now);

            if (OnLog != null)
            {
                OnLog(this, new LogEventArgs(log));
            }
        }

        public event EventHandler<LogEventArgs> OnLog;
    }

    public enum LogLevel
    {
        Fatal, Error, Warning, Info, Debug, Trace
    }

    public class LogEventArgs : EventArgs
    {
        public LogEntry Log { get; private set; }
        public LogEventArgs(LogEntry log)
        {
            this.Log = log;
        }
    }
}
