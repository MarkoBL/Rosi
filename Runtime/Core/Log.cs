using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Rosi.Runtime.Core
{
    public static class Log
    {
        public static LogLevels ConsoleLogLevel = LogLevels.Trace;
        public static LogLevels FileLogLevel = LogLevels.Warning;

        public static bool LogTrace => ConsoleLogLevel <= LogLevels.Trace; // Can be used to avoid costly string operations
        public static bool LogDebug => ConsoleLogLevel <= LogLevels.Debug;
        public static bool LogInfo => ConsoleLogLevel <= LogLevels.Info;

        public static bool ShowConsoleOutput = true;
        public static bool ConsoleExtendedMessage = false;

        static readonly HashSet<string> _ignores = new HashSet<string>();
        static StreamWriter _logStream = null;

        static readonly object _consoleLock = new object();

        static Log()
        {
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
            {
                var exception = GetInnerException(e.ExceptionObject as Exception);
                HandleException(exception, LogLevels.Fatal);
            };

            TaskScheduler.UnobservedTaskException += (object sender, UnobservedTaskExceptionEventArgs e) =>
            {
                var exception = GetInnerException(e.Exception);
                HandleException(exception, LogLevels.Fatal);
            };
        }

        public static bool SetLogFile(FileInfo filepath, bool append)
        {
            try
            {
                _logStream = new StreamWriter(filepath.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                {
                    AutoFlush = true
                };

                if (append)
                    _logStream.BaseStream.Position = _logStream.BaseStream.Length;
                else
                    _logStream.BaseStream.SetLength(0);

                return true;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }

            return false;
        }

        static Exception GetInnerException(Exception exception)
        {
            while (true)
            {
                if (exception.InnerException == null)
                    return exception;

                exception = exception.InnerException;
            }
        }

        static void Output(LogLevels logLevel, string output, string originalMessage, bool forceConsoleOutput)
        {
            if (logLevel >= ConsoleLogLevel && ShowConsoleOutput || forceConsoleOutput)
            {
                lock (_consoleLock)
                {
                    var color = Console.ForegroundColor;

                    if (logLevel >= LogLevels.Warning)
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    if (logLevel > LogLevels.Warning)
                        Console.ForegroundColor = ConsoleColor.Red;

                    Console.Error.WriteLine(ConsoleExtendedMessage ? output : originalMessage);
                    Console.ForegroundColor = color;
                }
            }

            try
            {
                if(logLevel >= FileLogLevel && !forceConsoleOutput)
                    _logStream?.WriteLine(output);
            }
            catch { }
        }

        static void LogEvent(LogLevels logLevel, string message, ILogger logger, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            if (logLevel >= ConsoleLogLevel || logLevel >= FileLogLevel)
            {
                var name = string.Empty;
                if (logger != null)
                {
                    if (logLevel < LogLevels.Error)
                    {
                        foreach (var ignore in _ignores)
                        {
                            if (logger.Logname.StartsWith(ignore, StringComparison.Ordinal))
                                return;
                        }
                    }
                    name = $" {logger.Logname}:";
                }
                var sender = $"@{memberName}():{Path.GetFileName(sourceFilePath)}:{sourceLineNumber}";
                var output = $"[{logLevel} {DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}]{name} {message} ({sender})";

                Output(logLevel, output, message, false);
            }
        }

        public static void AddIgnoreList(IEnumerable<string> ignoredLoggers)
        {
            if (ignoredLoggers != null)
            {
                foreach (var ignore in ignoredLoggers)
                {
                    if (!string.IsNullOrWhiteSpace(ignore))
                        _ignores.Add(ignore);
                }
            }
        }

        public static void Write(string message, ILogger logger = null)
        {
            var name = string.Empty;
            if (logger != null)
            {
                foreach (var ignore in _ignores)
                {
                    if (logger.Logname.StartsWith(ignore, StringComparison.Ordinal))
                        return;
                }
                name = $" {logger.Logname}:";
            }

            var output = $"[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}]{name} {message}";
            Output(LogLevels.Trace, output, message, true);
        }

        public static void Write(object message, ILogger logger = null)
        {
            Write(message.ToString(), logger);
        }

        public static void HandleException(Exception exception, LogLevels logLevel = LogLevels.Error, ILogger logger = null, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogEvent(logLevel, GetInnerException(exception).ToString(), logger, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void HandleException(Exception exception, ILogger logger, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogEvent(LogLevels.Error, GetInnerException(exception).ToString(), logger, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Trace(string message, ILogger logger = null, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogEvent(LogLevels.Trace, message, logger, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Debug(string message, ILogger logger = null, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogEvent(LogLevels.Debug, message, logger, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Info(string message, ILogger logger = null, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogEvent(LogLevels.Info, message, logger, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Warn(string message, ILogger logger = null, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogEvent(LogLevels.Warning, message, logger, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Error(string message, ILogger logger = null, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogEvent(LogLevels.Error, message, logger, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Fatal(string message, ILogger logger = null, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogEvent(LogLevels.Fatal, message, logger, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void LogMessage(LogLevels logLevel, string message, ILogger logger = null, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogEvent(logLevel, message, logger, memberName, sourceFilePath, sourceLineNumber);
        }
    }
}
