using System;
using WebSockets.Common.Common;

namespace WebSockets.DemoApp
{
    public class ConsoleWriteLineBasedLogger : IWebSocketLogger
    {
        private void Log(Type type, string level, string msg, params object[] args)
        {
            string msgFormatted;

            try
            {
                msgFormatted = string.Format(msg, args);
            } catch(Exception)
            {
                msgFormatted = "Failed to format message" + msg;
            }

            Console.WriteLine(type.FullName+ " " + level + " " + msgFormatted);
        }
        
        public void Debug(Type type, string format, params object[] args)
        {
            Log(type, "DEBUG", format, args);
        }
        
        public void Info(Type type, string format, params object[] args)
        {
            Log(type, "INFO", format, args);
        }
        
        public void Warn(Type type, string format, params object[] args)
        {
            Log(type, "WARN", format, args);
        }

        public void Err(Type type, string format, params object[] args)
        {
            Log(type, "ERROR", format, args);
        }

        public void Err(Type type, Exception ex)
        {
            Log(type, "ERR", "Exception {0}", ex);
        }
    }
}
