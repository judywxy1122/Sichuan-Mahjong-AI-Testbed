using System;
using System.Collections.Generic;

namespace SichuanMahjong.Core.Model
{
    public class Log
    {
        private class LogEntry
        {
            public readonly DateTime Time;
            public readonly string Message;

            public LogEntry(string message)
            {
                Time = DateTime.Now;
                Message = message;
            }

            public override string ToString()
            {
                return Message;
            }
        }

        private readonly List<LogEntry> logEntries;

        public Log()
        {
            logEntries = new List<LogEntry>();
            AddMessage("Instantiated log");
        }

        public void AddMessage(string message)
        {
            logEntries.Add(new LogEntry(message));
        }

        public List<string> GetLastXMessages(int x)
        {
            List<string> result = new List<string>();
            int start = Math.Max(0, logEntries.Count - x);
            for (int i = start; i < logEntries.Count; i++)
            {
                result.Add(logEntries[i].ToString());
            }
            return result;
        }
    }
}
