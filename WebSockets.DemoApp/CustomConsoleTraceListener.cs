using System;
using System.Diagnostics;
using System.Threading;

namespace WebSocketsCmd
{
    public class CustomConsoleTraceListener : TraceListener
    {
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            var message = string.Format(format, args);

            // write the localised date and time but include the time zone in brackets (good for combining logs from different timezones)
            var utcOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
            var plusOrMinus = (utcOffset < TimeSpan.Zero) ? "-" : "+";
            var utcHourOffset = utcOffset.TotalHours == 0 ? string.Empty : string.Format(" ({0}{1:hh})", plusOrMinus, utcOffset);
            var dateWithOffset = string.Format(@"{0:yyyy/MM/dd HH:mm:ss.fff}{1}", DateTime.Now, utcHourOffset);

            // display the threadid
            var log = string.Format(@"{0} [{1}] {2}", dateWithOffset, Thread.CurrentThread.ManagedThreadId, message);

            switch (eventType)
            {
                case TraceEventType.Critical:
                case TraceEventType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(log);
                    Console.ResetColor();
                    break;

                case TraceEventType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(log);
                    Console.ResetColor();
                    break;

                default:
                    Console.WriteLine(log);
                    break;
            }
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            TraceEvent(eventCache, source, eventType, id, message, new object[] {});
        }

        public override void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public override void Write(string message)
        {
            Console.Write(message);
        }
    }
}
