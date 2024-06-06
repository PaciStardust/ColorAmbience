using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ColorAmbience
{
    internal static class Logger
    {
        private static readonly StreamWriter? _logWriter;

        #region Logger Start
        static Logger()
        {
            _logWriter = StartLogger();
        }

        /// <summary>
        /// Starts the logging service
        /// </summary>
        /// <returns>Streamwriter for logging</returns>
        private static StreamWriter? StartLogger()
        {
            try
            {
                var writer = File.CreateText(Config.LogPath);
                writer.AutoFlush = true;
                PInfo("Created logging file at " + Config.LogPath);
                return writer;
            }
            catch (Exception e)
            {
                Error(e);
                return null;
            }
        }
        #endregion

        #region Logging Function

        private static readonly object _lock = new();

        private static void Log(LogMessage message)
        {
            if (!LogLevelAllowed(message.Severity)) return;

            lock (_lock) //Making sure log writing is not impacted by multithreading
            {
                var lowerMessage = message.Message.ToLower();
                foreach (var filter in Config.Debug.LogFilter)
                    if (lowerMessage.Contains(filter))
                        return;

                var messageString = message.ToString().Replace("\n", " ").Replace("\r", "");
                Console.WriteLine($"\u001b[38;2;{GetLogColor(message.Severity)}m{messageString}\u001b[39;49m");
                _logWriter?.WriteLine(messageString);
            }
        }
        public static void Log(string message, LogSeverity severity = LogSeverity.Log, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
            => Log(new(severity, file, member, line, message));

        public static void Info(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
            => Log(message, LogSeverity.Info, file, member, line);
        public static void PInfo(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
            => Log(message, LogSeverity.PrioInfo, file, member, line);
        public static void Warning(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
            => Log(message, LogSeverity.Warning, file, member, line);
        public static void Debug(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
            => Log(message, LogSeverity.Debug, file, member, line);

        // ERROR LOGGING
        public static void Error(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0) //Error with basic message
            => Log(message, LogSeverity.Error, file, member, line);

        public static void Error(Exception error, string descriptiveError = "", [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0) //Error using exception
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(descriptiveError))
                sb.AppendLine(descriptiveError + "\n\n\nError details:\n");

            sb.AppendLine(error.Message);

            if (error.InnerException != null)
                sb.AppendLine($"\n(Inner {error.InnerException.GetType()}: {error.Message}{(error.Source == null ? "" : $" at {error.Source}")})");

            if (error.StackTrace != null)
                sb.AppendLine("\n\nStack trace:\n\n" + error.StackTrace);

            Error(sb.ToString(), file, member, line);
        }
        #endregion

        #region Utils
        /// <summary>
        /// Get the color of the log message for the log window
        /// </summary>
        /// <param name="severity">Severity of log</param>
        /// <returns>Corresponding log color</returns>
        private static string GetLogColor(LogSeverity severity) => severity switch
        {
            LogSeverity.Error => "255;0;55",
            LogSeverity.Critical => "122;0;27",
            LogSeverity.Warning => "255;165;0",
            LogSeverity.PrioInfo => "0;165;255",
            LogSeverity.Log => "110,110,110",
            LogSeverity.Info => "255;255;255",
            LogSeverity.Debug => "110,110,110",
            _ => "255;255;255"
        };

        /// <summary>
        /// Check if logging is allowed for severity
        /// </summary>
        /// <param name="severity">Severity of log</param>
        /// <returns>Logging enabled?</returns>
        private static bool LogLevelAllowed(LogSeverity severity) => severity switch
        {
            LogSeverity.Error => Config.Debug.Error,
            LogSeverity.Warning => Config.Debug.Warning,
            LogSeverity.PrioInfo => Config.Debug.PrioInfo,
            LogSeverity.Info => Config.Debug.Info,
            LogSeverity.Log => Config.Debug.Log,
            LogSeverity.Debug => Config.Debug.Debug,
            _ => true
        };
        #endregion
    }

    /// <summary>
    /// Logging message class
    /// </summary>
    public readonly struct LogMessage
    {
        public static int MaxSeverityLength => 8;
        public string SourceFile { get; private init; }
        public string SourceMember { get; private init; }
        public int SourceLine { get; private init; }
        public string Message { get; private init; }
        public LogSeverity Severity { get; private init; }
        private readonly string _sevString;
        public DateTime Time { get; private init; }

        public LogMessage(LogSeverity serverity, string sourceFile, string sourcMember, int sourceLine, string message)
        {
            Message = message;
            SourceFile = Path.GetFileName(sourceFile);
            SourceMember = sourcMember;
            SourceLine = sourceLine;
            Severity = serverity;
            _sevString = Pad(serverity.ToString(), MaxSeverityLength);
            Time = DateTime.Now;
        }

        public override string ToString()
            => $"{Time:HH:mm:ss.fff} {_sevString} [{GetLocation()}] {Message}";

        public string GetLocation()
            => $"{SourceFile}::{SourceMember}:{SourceLine}";

        /// <summary>
        /// Padding utility for log messages to have consistent width
        /// </summary>
        /// <param name="value">String to shorten</param>
        /// <param name="len">Length for padding</param>
        /// <returns></returns>
        private static string Pad(string value, int len)
        {
            if (value.Length > len)
                value = value[..len];
            value = value.PadRight(len, ' ');
            return value;
        }
    }

    public enum LogSeverity
    {
        Error,
        Warning,
        Info,
        Log,
        Debug,
        Critical,
        PrioInfo
    }
}
