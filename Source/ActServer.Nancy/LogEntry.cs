using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainbowMage.ActServer.Nancy
{
    public class LogEntry
    {
        public LogLevel Level { get; private set; }
        public string Message { get; private set; }
        public DateTime Timestamp { get; private set; }

        public LogEntry(LogLevel level, string message, DateTime timestamp)
        {
            this.Level = level;
            this.Message = message;
            this.Timestamp = timestamp;
        }
    }
}
