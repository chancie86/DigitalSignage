using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NLog;

namespace Display
{
    public static class Log
    {
        private static readonly Logger NLogLogger;

        static Log()
        {
            NLogLogger = LogManager.GetLogger("DisplayLogger");
            TraceMsg("*** Start of new Log ***");
            TraceMsg("Logs are being written to '{0}'", AppDomain.CurrentDomain.BaseDirectory);
        }

        public static void TraceEntry([CallerMemberName] string memberName = null)
        {
            WriteLine(LogLevel.Trace, memberName + ": Entry");
        }

        public static void TraceExit([CallerMemberName] string memberName = null)
        {
            WriteLine(LogLevel.Trace, memberName + ": Exit");
        }

        public static void TraceExit(object message, [CallerMemberName] string memberName = null)
        {
            WriteLine(LogLevel.Trace, memberName + ": Exit. " + message);
        }

        public static void TraceMsg(string message, params object[] formatItems)
        {
            WriteLine(LogLevel.Trace, message, formatItems);
        }

        public static void TraceErr(string message, params object[] formatItems)
        {
            WriteLine(LogLevel.Error, message, formatItems);
        }

        private static void WriteLine(LogLevel level, string message, params object[] formatItems)
        {
            try
            {
                Debug.WriteLine(message, formatItems);
                NLogLogger.Log(level, message, formatItems);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
