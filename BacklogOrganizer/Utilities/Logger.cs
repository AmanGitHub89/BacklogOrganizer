using System;

using log4net;
using log4net.Core;

using Newtonsoft.Json;


namespace BacklogOrganizer.Utilities
{
    internal static class Logger
    {
        private static readonly ILog myLogger = LogManager.GetLogger(typeof(Logger));

        public static void Debug(string message)
        {
            myLogger.Debug(GetLogMessage(Level.Debug, message));
        }

        public static void Info(string message)
        {
            myLogger.Info(GetLogMessage(Level.Info, message));
        }

        public static void Warn(string message, Exception ex = null)
        {
            if (string.IsNullOrEmpty(message) && ex == null)
            {
                return;
            }
            myLogger.Warn(GetLogMessage(Level.Warn, message, null, ex));
        }

        public static void Error(Exception ex)
        {
            myLogger.Error(GetLogMessage(Level.Error, null, null, ex));
        }

        public static void Error(string message, Exception ex)
        {
            myLogger.Error(GetLogMessage(Level.Error, message, null, ex));
        }

        public static void Error(string message, Exception ex, object inputs)
        {
            myLogger.Error(GetLogMessage(Level.Error, message, inputs, ex));
        }

        public static void Fatal(string message, Exception ex)
        {
            myLogger.Fatal(GetLogMessage(Level.Fatal, message, null, ex));
        }

        public static void Fatal(string message, Exception ex, object inputs)
        {
            myLogger.Fatal(GetLogMessage(Level.Fatal, message, inputs, ex));
        }

        private static string GetLogMessage(Level level, string message, object data = null, Exception ex = null, string comment = null)
        {
            var jsonData = string.Empty;
            comment = comment ?? string.Empty;
            if (data != null)
            {
                try
                {
                    jsonData = JsonConvert.SerializeObject(data);
                }
                catch
                {
                    comment += $"{Environment.NewLine}Could not serialize input data to be logged.";
                }
            }
            var logMessage = new LogMessage
            {
                Type = level.ToString(),
                Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff"),
                Message = message ?? string.Empty,
                Data = jsonData,
                Exception = GetExceptionMessage(ex),
                InnerException = GetExceptionMessage(ex?.InnerException),
                Comment = comment
            };
            return JsonConvert.SerializeObject(logMessage, Formatting.Indented);
        }

        private static string GetExceptionMessage(Exception ex)
        {
            return ex == null ? string.Empty : $"{Environment.NewLine}{ex.GetType()}: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
        }

        private class LogMessage
        {
            public string Type { get; set; }
            public string Date { get; set; }
            public string Message { get; set; }
            public string Data { get; set; }
            public string Exception { get; set; }
            public string InnerException { get; set; }
            public string Comment { get; set; }
        }
    }
}
