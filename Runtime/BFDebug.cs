using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Linq;
using System.Diagnostics;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace ByteForge.Runtime
{
    /// <summary>
    /// Enhanced debug logging system for Unity.
    /// Provides extensive logging capabilities beyond Unity's built-in Debug class.
    /// </summary>
    public static class BFDebug
    {
        #region Enums and Structures

        /// <summary>
        /// Log levels for categorizing messages.
        /// </summary>
        public enum LogLevel
        {
            TRACE = 0, // Most detailed information
            DEBUG = 1, // Debugging information
            INFO = 2, // General information
            WARNING = 3, // Warnings
            ERROR = 4, // Errors
            CRITICAL = 5, // Critical errors
            NONE = 6 // No logging
        }

        /// <summary>
        /// Log output destinations.
        /// </summary>
        [Flags]
        public enum LogDestination
        {
            NONE = 0,
            CONSOLE = 1,
            FILE = 2,
            ALL = CONSOLE | FILE
        }

        /// <summary>
        /// Log message structure for internal queue.
        /// </summary>
        private struct LogMessage
        {
            public LogLevel Level;
            public string Message;
            public string Category;
            public string StackTrace;
            public DateTime Timestamp;
            public int ThreadId;

            public LogMessage(LogLevel level, string message, string category)
            {
                Level = level;
                Message = message;
                Category = string.IsNullOrEmpty(category) ? "Default" : category;
                StackTrace = new StackTrace(2, true).ToString();
                Timestamp = DateTime.Now;
                ThreadId = Thread.CurrentThread.ManagedThreadId;
            }
        }

        #endregion

        #region Static Fields

        // Global settings
        private static LogLevel minimumLogLevel = LogLevel.DEBUG;
        private static LogDestination logDestination = LogDestination.CONSOLE;
        private static Dictionary<string, LogLevel> categoryLogLevels = new();
        private static List<string> excludedCategories = new();
        private static bool includeTimestamp = true;
        private static bool includeLogLevel = true;
        private static bool includeCategory = true;
        private static bool includeThreadId = false;
        private static bool stackTraceForErrors = true;
        private static bool useColours = true;
        private static bool logEnabled = true;
        private static bool asyncFileLogging = true;
        private static bool writeToPlayerLog = true;

        // File logging settings
        private static string logFilePath;
        private static string logFileName = "BFDebug.log";
        private static long maxLogFileSize = 10 * 1024 * 1024; // 10 MB
        private static int maxLogFiles = 5;
        private static bool appendToLogFile = true;
        private static StreamWriter logFileWriter = null;
        private static readonly object fileLock = new();
        private static Queue<LogMessage> asyncLogQueue = new();
        private static Thread logThread = null;
        private static bool logThreadRunning = false;
        private static AutoResetEvent logSignal = new(false);

        // Log message formatting
        private static Dictionary<LogLevel, string> logLevelColours = new()
        {
            { LogLevel.TRACE, "<colour=#AAAAAA>" }, // Gray
            { LogLevel.DEBUG, "<colour=#FFFFFF>" }, // White
            { LogLevel.INFO, "<colour=#00FF00>" }, // Green
            { LogLevel.WARNING, "<colour=#FFFF00>" }, // Yellow
            { LogLevel.ERROR, "<colour=#FF8000>" }, // Orange
            { LogLevel.CRITICAL, "<colour=#FF0000>" } // Red
        };

        // Performance tracking
        private static DateTime lastPerformanceLog = DateTime.MinValue;
        private static int logCountSinceLastPerformance = 0;
        private static TimeSpan performanceLogInterval = TimeSpan.FromMinutes(5);
        private static Dictionary<string, Stopwatch> timerDict = new();
        private static Dictionary<string, long> counterDict = new();

        #endregion

        #region Initialization

        /// <summary>
        /// Static constructor to initialize the BFDebug system.
        /// </summary>
        static BFDebug()
        {
            // Set default log file path
            logFilePath = Path.Combine(Application.persistentDataPath, "Logs");

            // Subscribe to application quit to clean up resources
            Application.quitting += OnApplicationQuit;

            // Start async logging thread if enabled
            if (asyncFileLogging)
                StartLogThread();

            // Log initialization
            Info("BFDebug system initialized", "BFDebug");
        }

        /// <summary>
        /// Initializes the logging system with custom settings.
        /// </summary>
        public static void Initialize(LogLevel minimumLevel = LogLevel.DEBUG,
            LogDestination destination = LogDestination.CONSOLE,
            string logFilePath = null,
            bool appendToFile = true)
        {
            minimumLogLevel = minimumLevel;
            logDestination = destination;
            appendToLogFile = appendToFile;

            if (logFilePath != null)
                BFDebug.logFilePath = logFilePath;

            // Ensure log directory exists
            if ((logDestination & LogDestination.FILE) != 0)
                EnsureLogDirectoryExists();

            // Start async logging thread if enabled and using file destination
            if (asyncFileLogging && (logDestination & LogDestination.FILE) != 0)
                StartLogThread();

            Info($"BFDebug initialized with level: {minimumLevel}, destination: {destination}", "BFDebug");
        }

        /// <summary>
        /// Ensures the log directory exists, creates it if not.
        /// </summary>
        private static void EnsureLogDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(logFilePath))
                    Directory.CreateDirectory(logFilePath);
            }
            catch (Exception ex)
            {
                // Fall back to application persistent path
                logFilePath = Path.Combine(Application.persistentDataPath, "Logs");

                if (!Directory.Exists(logFilePath))
                    Directory.CreateDirectory(logFilePath);

                Error($"Failed to create log directory, falling back to: {logFilePath}. Error: {ex.Message}", "BFDebug");
            }
        }

        /// <summary>
        /// Starts the asynchronous logging thread.
        /// </summary>
        private static void StartLogThread()
        {
            if (logThread != null && logThreadRunning)
                return;

            logThreadRunning = true;
            logThread = new(ProcessLogQueue)
            {
                IsBackground = true,
                Name = "BFDebug_LogThread"
            };
            logThread.Start();
        }

        /// <summary>
        /// Worker method for the asynchronous logging thread.
        /// </summary>
        private static void ProcessLogQueue()
        {
            while (logThreadRunning)
            {
                // Wait for signal or timeout after 1 second
                logSignal.WaitOne(1000);

                if (asyncLogQueue.Count <= 0) continue;
            
                lock (fileLock)
                {
                    try
                    {
                        if (logFileWriter == null)
                            OpenLogFile();

                        while (asyncLogQueue.Count > 0)
                        {
                            LogMessage msg = asyncLogQueue.Dequeue();
                            string formattedMessage = FormatLogMessage(msg, false);
                            logFileWriter.WriteLine(formattedMessage);
                        }

                        logFileWriter.Flush();

                        // Check if we need to roll the log file
                        if (logFileWriter.BaseStream.Length > maxLogFileSize)
                            RollLogFiles();
                    }
                    catch (Exception ex)
                    {
                        // Can't use BF logging system here - would cause recursion
                        UnityEngine.Debug.LogError($"BFDebug: Error writing to log file: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Opens the log file for writing.
        /// </summary>
        private static void OpenLogFile()
        {
            try
            {
                string fullPath = Path.Combine(logFilePath, logFileName);

                // Create or open the file
                FileMode mode = appendToLogFile ? FileMode.Append : FileMode.Create;
                FileStream fs = new(fullPath, mode, FileAccess.Write, FileShare.Read);
                logFileWriter = new(fs, Encoding.UTF8);

                // Write header if creating a new file
                if (!appendToLogFile || fs.Length == 0)
                {
                    logFileWriter.WriteLine("----------------------------------------");
                    logFileWriter.WriteLine($"BFDebug Log File - Started {DateTime.Now}");
                    logFileWriter.WriteLine($"Application: {Application.productName} v{Application.version}");
                    logFileWriter.WriteLine($"Platform: {Application.platform}");
                    logFileWriter.WriteLine($"Unity Version: {Application.unityVersion}");
                    logFileWriter.WriteLine("----------------------------------------");
                }
                else
                {
                    logFileWriter.WriteLine();
                    logFileWriter.WriteLine($"--- Log continued at {DateTime.Now} ---");
                    logFileWriter.WriteLine();
                }

                logFileWriter.Flush();
            }
            catch (Exception ex)
            {
                // Can't use BF logging system here - would cause recursion
                UnityEngine.Debug.LogError($"BFDebug: Failed to open log file: {ex.Message}");

                // Disable file logging
                logDestination &= ~LogDestination.FILE;
            }
        }

        /// <summary>
        /// Rolls log files, maintaining the specified number of backup files.
        /// </summary>
        private static void RollLogFiles()
        {
            try
            {
                // Close current log file
                if (logFileWriter != null)
                {
                    logFileWriter.Flush();
                    logFileWriter.Close();
                    logFileWriter = null;
                }

                string baseFilePath = Path.Combine(logFilePath, logFileName);

                // Delete oldest log file if we have too many
                string oldestLogFile = Path.Combine(logFilePath, $"{logFileName}.{maxLogFiles}");
                if (File.Exists(oldestLogFile))
                    File.Delete(oldestLogFile);

                // Shift existing log files
                for (int i = maxLogFiles - 1; i >= 1; i--)
                {
                    string currentFile = Path.Combine(logFilePath, $"{logFileName}.{i}");
                    string nextFile = Path.Combine(logFilePath, $"{logFileName}.{i + 1}");

                    if (File.Exists(currentFile))
                        File.Move(currentFile, nextFile);
                }

                // Move current log file
                if (File.Exists(baseFilePath))
                    File.Move(baseFilePath, Path.Combine(logFilePath, $"{logFileName}.1"));

                // Open a new log file
                OpenLogFile();
            }
            catch (Exception ex)
            {
                // Can't use BF logging system here - would cause recursion
                UnityEngine.Debug.LogError($"BFDebug: Failed to roll log files: {ex.Message}");
            }
        }

        /// <summary>
        /// Closes log files and releases resources when the application quits.
        /// </summary>
        private static void OnApplicationQuit()
        {
            // Signal log thread to stop
            logThreadRunning = false;

            // Process any remaining log messages
            if (asyncLogQueue.Count > 0 && (logDestination & LogDestination.FILE) != 0)
            {
                lock (fileLock)
                {
                    try
                    {
                        if (logFileWriter == null)
                            OpenLogFile();

                        while (asyncLogQueue.Count > 0)
                        {
                            LogMessage msg = asyncLogQueue.Dequeue();
                            string formattedMessage = FormatLogMessage(msg, false);
                            logFileWriter.WriteLine(formattedMessage);
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore errors during shutdown
                    }
                }
            }

            // Close log file
            lock (fileLock)
            {
                if (logFileWriter != null)
                {
                    try
                    {
                        logFileWriter.WriteLine();
                        logFileWriter.WriteLine($"--- Log closed at {DateTime.Now} ---");
                        logFileWriter.Flush();
                        logFileWriter.Close();
                    }
                    catch (Exception)
                    {
                        // Ignore errors during shutdown
                    }
                    finally
                    {
                        logFileWriter = null;
                    }
                }
            }
        }

        #endregion

        #region Configuration Methods

        /// <summary>
        /// Sets the minimum log level for all categories.
        /// </summary>
        public static void SetMinimumLogLevel(LogLevel level)
        {
            minimumLogLevel = level;
        }

        /// <summary>
        /// Sets the minimum log level for a specific category.
        /// </summary>
        public static void SetCategoryLogLevel(string category, LogLevel level)
        {
            if (string.IsNullOrEmpty(category))
                category = "Default";

            if (categoryLogLevels.ContainsKey(category))
                categoryLogLevels[category] = level;
            else
                categoryLogLevels.Add(category, level);
        }

        /// <summary>
        /// Sets the log destination (console, file, or both).
        /// </summary>
        public static void SetLogDestination(LogDestination destination)
        {
            bool wasFileLogging = (logDestination & LogDestination.FILE) != 0;
            bool willFileLog = (destination & LogDestination.FILE) != 0;

            logDestination = destination;

            // Start file logging if needed
            if (wasFileLogging || !willFileLog) return;
        
            EnsureLogDirectoryExists();
            if (asyncFileLogging)
                StartLogThread();
        }

        /// <summary>
        /// Sets the log file path and name.
        /// </summary>
        public static void SetLogFile(string filePath, string fileName = null, bool appendToFile = true)
        {
            lock (fileLock)
            {
                // Close current log file if open
                if (logFileWriter != null)
                {
                    logFileWriter.Flush();
                    logFileWriter.Close();
                    logFileWriter = null;
                }

                logFilePath = filePath;
                if (!string.IsNullOrEmpty(fileName))
                    logFileName = fileName;

                appendToLogFile = appendToFile;

                // Ensure directory exists
                EnsureLogDirectoryExists();
            }
        }

        /// <summary>
        /// Configures log file rotation settings.
        /// </summary>
        public static void ConfigureLogFileRotation(long maxFileSizeBytes, int maxFiles)
        {
            maxLogFileSize = Math.Max(1024, maxFileSizeBytes); // Minimum 1KB
            maxLogFiles = Math.Max(1, maxFiles); // Minimum 1 file
        }

        /// <summary>
        /// Configures log message formatting options.
        /// </summary>
        public static void ConfigureLogFormat(bool includeTimestamp, bool includeLogLevel,
            bool includeCategory, bool includeThreadId,
            bool stackTraceForErrors, bool useColours)
        {
            BFDebug.includeTimestamp = includeTimestamp;
            BFDebug.includeLogLevel = includeLogLevel;
            BFDebug.includeCategory = includeCategory;
            BFDebug.includeThreadId = includeThreadId;
            BFDebug.stackTraceForErrors = stackTraceForErrors;
            BFDebug.useColours = useColours;
        }

        /// <summary>
        /// Excludes a category from logging.
        /// </summary>
        public static void ExcludeCategory(string category)
        {
            if (!string.IsNullOrEmpty(category) && !excludedCategories.Contains(category))
                excludedCategories.Add(category);
        }

        /// <summary>
        /// Includes a previously excluded category.
        /// </summary>
        public static void IncludeCategory(string category)
        {
            if (!string.IsNullOrEmpty(category) && excludedCategories.Contains(category))
                excludedCategories.Remove(category);
        }

        /// <summary>
        /// Enables or disables logging completely.
        /// </summary>
        public static void EnableLogging(bool enable)
        {
            logEnabled = enable;
        }

        /// <summary>
        /// Enables or disables asynchronous file logging.
        /// </summary>
        public static void EnableAsyncFileLogging(bool enable)
        {
            if (asyncFileLogging == enable)
                return;

            asyncFileLogging = enable;

            if (enable && (logDestination & LogDestination.FILE) != 0)
                StartLogThread();
            else if (!enable)
                logThreadRunning = false;
        }

        /// <summary>
        /// Enables or disables writing to Unity's player log.
        /// </summary>
        public static void EnablePlayerLog(bool enable)
        {
            writeToPlayerLog = enable;
        }

        #endregion

        #region Logging Methods

        /// <summary>
        /// Logs a message at Trace level.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Trace(string message, string category = null)
        {
            Log(LogLevel.TRACE, message, category);
        }

        /// <summary>
        /// Logs a message at Debug level.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Debug(string message, string category = null)
        {
            Log(LogLevel.DEBUG, message, category);
        }

        /// <summary>
        /// Logs a message at Info level.
        /// </summary>
        public static void Info(string message, string category = null)
        {
            Log(LogLevel.INFO, message, category);
        }

        /// <summary>
        /// Logs a message at Warning level.
        /// </summary>
        public static void Warning(string message, string category = null)
        {
            Log(LogLevel.WARNING, message, category);
        }

        /// <summary>
        /// Logs a message at Error level.
        /// </summary>
        public static void Error(string message, string category = null)
        {
            Log(LogLevel.ERROR, message, category);
        }

        /// <summary>
        /// Logs a message at Critical level.
        /// </summary>
        public static void Critical(string message, string category = null)
        {
            Log(LogLevel.CRITICAL, message, category);
        }

        /// <summary>
        /// Logs a formatted message with parameters at the specified level.
        /// </summary>
        public static void LogFormat(LogLevel level, string format, params object[] args)
        {
            if (!logEnabled || level < minimumLogLevel)
                return;

            try
            {
                string message = string.Format(format, args);
                Log(level, message);
            }
            catch (Exception ex)
            {
                Error($"Error formatting log message: {ex.Message}", "BFDebug");
            }
        }

        /// <summary>
        /// Logs an exception with an optional message.
        /// </summary>
        public static void LogException(Exception exception, string message = null, string category = null)
        {
            if (exception == null)
                return;

            string exceptionMessage = message ?? "Exception occurred";
            exceptionMessage += $": {exception.GetType().Name}: {exception.Message}";

            if (exception.StackTrace != null)
                exceptionMessage += $"\n{exception.StackTrace}";

            Error(exceptionMessage, category ?? "Exception");
        }

        public static void Log(string message)
        {
            Log(LogLevel.INFO, message);
        }
        
        public static void Log(string message, string category)
        {
            Log(LogLevel.INFO, message, category);
        }
        
        public static void Log(string message, LogLevel level)
        {
            Log(level, message);
        }
        
        public static void LogWarning(string message)
        {
            Log(LogLevel.WARNING, message);
        }

        public static void LogWarning(string message, string category)
        {
            Log(LogLevel.WARNING, message, category);
        }

        public static void LogError(string message)
        {
            Log(LogLevel.ERROR, message);
        }
        
        public static void LogError(string message, string category)
        {
            Log(LogLevel.ERROR, message, category);
        }
        
        public static void LogCritical(string message)
        {
            Log(LogLevel.CRITICAL, message);
        }

        public static void LogCritical(string message, string category)
        {
            Log(LogLevel.CRITICAL, message, category);
        }

        /// <summary>
        /// Logs a message with the specified level and category.
        /// </summary>
        public static void Log(LogLevel level, string message, string category = null)
        {
            // Skip if logging is disabled or level is below minimum
            if (!logEnabled || level == LogLevel.NONE || level < minimumLogLevel)
                return;

            // Use default category if none provided
            if (string.IsNullOrEmpty(category))
                category = "Default";

            // Skip if category is excluded
            if (excludedCategories.Contains(category))
                return;

            // Check category-specific log level
            if (categoryLogLevels.TryGetValue(category, out LogLevel categoryLevel) && level < categoryLevel)
                return;

            // Create log message
            LogMessage logMessage = new(level, message, category);

            // Console logging
            if ((logDestination & LogDestination.CONSOLE) != 0)
            {
                string formattedMessage = FormatLogMessage(logMessage, true);

                // Write to Unity console
                if (writeToPlayerLog)
                {
                    switch (level)
                    {
                        case LogLevel.WARNING:
                            UnityEngine.Debug.LogWarning(formattedMessage);
                            break;
                        case LogLevel.ERROR:
                        case LogLevel.CRITICAL:
                            UnityEngine.Debug.LogError(formattedMessage);
                            break;
                        default:
                            UnityEngine.Debug.Log(formattedMessage);
                            break;
                    }
                }
            }

            // File logging
            if ((logDestination & LogDestination.FILE) != 0)
            {
                if (asyncFileLogging)
                {
                    // Add to async queue
                    lock (fileLock)
                        asyncLogQueue.Enqueue(logMessage);

                    // Signal log thread
                    logSignal.Set();
                }
                else
                {
                    // Log synchronously
                    lock (fileLock)
                    {
                        try
                        {
                            if (logFileWriter == null)
                                OpenLogFile();

                            string formattedMessage = FormatLogMessage(logMessage, false);
                            logFileWriter.WriteLine(formattedMessage);
                            logFileWriter.Flush();

                            // Check if we need to roll the log file
                            if (logFileWriter.BaseStream.Length > maxLogFileSize)
                                RollLogFiles();
                        }
                        catch (Exception ex)
                        {
                            // Can't use BF logging system here - would cause recursion
                            UnityEngine.Debug.LogError($"BFDebug: Error writing to log file: {ex.Message}");
                        }
                    }
                }
            }

            // Track performance
            TrackPerformance();
        }

        /// <summary>
        /// Formats a log message according to configuration settings.
        /// </summary>
        private static string FormatLogMessage(LogMessage msg, bool forConsole)
        {
            StringBuilder sb = new();

            // Start colour if using them and in console
            if (forConsole && useColours)
                sb.Append(logLevelColours[msg.Level]);

            // Add timestamp
            if (includeTimestamp)
                sb.Append($"[{msg.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] ");

            // Add log level
            if (includeLogLevel)
                sb.Append($"[{msg.Level}] ");

            // Add category
            if (includeCategory && !string.IsNullOrEmpty(msg.Category))
                sb.Append($"[{msg.Category}] ");

            // Add thread ID
            if (includeThreadId)
                sb.Append($"[Thread:{msg.ThreadId}] ");

            // Add message
            sb.Append(msg.Message);

            // Add stack trace for errors if enabled
            if (stackTraceForErrors && msg.Level is LogLevel.ERROR or LogLevel.CRITICAL)
            {
                sb.AppendLine();
                sb.Append("Stack Trace:");
                sb.AppendLine(msg.StackTrace);
            }

            // End colour if using them and in console
            if (forConsole && useColours)
                sb.Append("</colour>");

            return sb.ToString();
        }

        /// <summary>
        /// Tracks performance metrics for logging activity.
        /// </summary>
        private static void TrackPerformance()
        {
            logCountSinceLastPerformance++;

            DateTime now = DateTime.Now;
            if ((now - lastPerformanceLog) > performanceLogInterval && lastPerformanceLog != DateTime.MinValue)
            {
                TimeSpan elapsed = now - lastPerformanceLog;
                double logsPerSecond = logCountSinceLastPerformance / elapsed.TotalSeconds;

                // Log performance stats at trace level
                Trace(
                    $"Logging performance: {logCountSinceLastPerformance} logs in {elapsed.TotalSeconds:F1}s ({logsPerSecond:F1}/s)",
                    "BFDebug.Performance");

                logCountSinceLastPerformance = 0;
                lastPerformanceLog = now;
            }
            else if (lastPerformanceLog == DateTime.MinValue)
                lastPerformanceLog = now;
        }

        #endregion

        #region Conditional Logging Methods

        /// <summary>
        /// Logs a message only if condition is true.
        /// </summary>
        public static void LogIf(bool condition, LogLevel level, string message, string category = null)
        {
            if (condition)
                Log(level, message, category);
        }

        /// <summary>
        /// Logs at Debug level only if condition is true.
        /// </summary>
        public static void DebugIf(bool condition, string message, string category = null)
        {
            if (condition)
                Debug(message, category);
        }

        /// <summary>
        /// Logs at Info level only if condition is true.
        /// </summary>
        public static void InfoIf(bool condition, string message, string category = null)
        {
            if (condition)
                Info(message, category);
        }

        /// <summary>
        /// Logs at Warning level only if condition is true.
        /// </summary>
        public static void WarningIf(bool condition, string message, string category = null)
        {
            if (condition)
                Warning(message, category);
        }

        /// <summary>
        /// Logs at Error level only if condition is true.
        /// </summary>
        public static void ErrorIf(bool condition, string message, string category = null)
        {
            if (condition)
                Error(message, category);
        }

        /// <summary>
        /// Logs at Critical level only if condition is true.
        /// </summary>
        public static void CriticalIf(bool condition, string message, string category = null)
        {
            if (condition)
                Critical(message, category);
        }

        #endregion

        #region Performance Timing Methods

        /// <summary>
        /// Starts a named timer for performance measurement.
        /// </summary>
        public static void StartTimer(string timerName)
        {
            if (string.IsNullOrEmpty(timerName))
                return;

            Stopwatch stopwatch = new();
            stopwatch.Start();

            lock (timerDict)
            {
                if (timerDict.ContainsKey(timerName))
                    timerDict[timerName] = stopwatch;
                else
                    timerDict.Add(timerName, stopwatch);
            }
        }

        /// <summary>
        /// Stops a named timer and logs the elapsed time.
        /// </summary>
        public static void StopTimer(string timerName, string message = null, LogLevel level = LogLevel.DEBUG)
        {
            if (string.IsNullOrEmpty(timerName))
                return;

            Stopwatch stopwatch;

            lock (timerDict)
            {
                if (!timerDict.TryGetValue(timerName, out stopwatch))
                    return;

                timerDict.Remove(timerName);
            }

            stopwatch.Stop();
            TimeSpan elapsed = stopwatch.Elapsed;

            string logMessage;
            logMessage = string.IsNullOrEmpty(message)
                ? $"Timer '{timerName}' elapsed: {FormatTimeSpan(elapsed)}"
                : $"{message}: {FormatTimeSpan(elapsed)}";

            Log(level, logMessage, "Performance");
        }

        /// <summary>
        /// Formats a TimeSpan in a human-readable format.
        /// </summary>
        private static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 1)
                return $"{timeSpan.TotalMilliseconds:F2} ms";
            if (timeSpan.TotalMinutes < 1)
                return $"{timeSpan.TotalSeconds:F2} s";
            if (timeSpan.TotalHours < 1)
                return $"{timeSpan.TotalMinutes:F2} min";

            return $"{timeSpan.TotalHours:F2} h";
        }

        /// <summary>
        /// Measures and logs the execution time of an action.
        /// </summary>
        public static void TimeAction(Action action, string description, LogLevel level = LogLevel.DEBUG)
        {
            if (action == null)
                return;

            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                action();
            }
            finally
            {
                stopwatch.Stop();
                TimeSpan elapsed = stopwatch.Elapsed;

                string logMessage = $"{description}: {FormatTimeSpan(elapsed)}";
                Log(level, logMessage, "Performance");
            }
        }

        #endregion

        #region Counter Methods

        /// <summary>
        /// Increments a named counter.
        /// </summary>
        public static void IncrementCounter(string counterName, long amount = 1)
        {
            if (string.IsNullOrEmpty(counterName))
                return;

            lock (counterDict)
            {
                if (counterDict.ContainsKey(counterName))
                    counterDict[counterName] += amount;
                else
                    counterDict.Add(counterName, amount);
            }
        }

        /// <summary>
        /// Resets a named counter to zero.
        /// </summary>
        public static void ResetCounter(string counterName)
        {
            if (string.IsNullOrEmpty(counterName))
                return;

            lock (counterDict)
            {
                if (counterDict.ContainsKey(counterName))
                    counterDict[counterName] = 0;
            }
        }

        /// <summary>
        /// Gets the current value of a named counter.
        /// </summary>
        public static long GetCounter(string counterName)
        {
            if (string.IsNullOrEmpty(counterName))
                return 0;

            lock (counterDict)
            {
                if (counterDict.TryGetValue(counterName, out long value))
                    return value;

                return 0;
            }
        }

        /// <summary>
        /// Logs the current value of a named counter.
        /// </summary>
        public static void LogCounter(string counterName, string message = null, LogLevel level = LogLevel.DEBUG)
        {
            long value = GetCounter(counterName);

            string logMessage;
            if (string.IsNullOrEmpty(message))
                logMessage = $"Counter '{counterName}': {value}";
            else
                logMessage = $"{message}: {value}";

            Log(level, logMessage, "Counter");
        }

        /// <summary>
        /// Logs all counters.
        /// </summary>
        public static void LogAllCounters(LogLevel level = LogLevel.DEBUG)
        {
            Dictionary<string, long> counters;

            lock (counterDict)
                counters = new(counterDict);

            if (counters.Count == 0)
            {
                Log(level, "No counters to log", "Counter");
                return;
            }

            StringBuilder sb = new();
            sb.AppendLine("Counter values:");

            foreach (KeyValuePair<string, long> counter in counters.OrderBy(c => c.Key))
                sb.AppendLine($"  {counter.Key}: {counter.Value}");

            Log(level, sb.ToString(), "Counter");
        }

        #endregion

        #region Memory Profiling Methods

        /// <summary>
        /// Logs current memory usage statistics.
        /// </summary>
        public static void LogMemoryUsage(LogLevel level = LogLevel.DEBUG)
        {
            StringBuilder sb = new();
            sb.AppendLine("Memory Usage:");

            // Total allocated memory
            sb.AppendLine($"  Total Allocated: {FormatBytes(Profiler.GetTotalAllocatedMemoryLong())}");

            // Reserved memory
            sb.AppendLine($"  Total Reserved: {FormatBytes(Profiler.GetTotalReservedMemoryLong())}");

            // Mono heap
            sb.AppendLine($"  Mono Heap: {FormatBytes(Profiler.GetMonoHeapSizeLong())}");
            sb.AppendLine($"  Mono Used: {FormatBytes(Profiler.GetMonoUsedSizeLong())}");

            // GC collection count
            sb.AppendLine($"  GC Collection Count: {GC.CollectionCount(0)} (Gen 0)");
            sb.AppendLine($"  GC Collection Count: {GC.CollectionCount(1)} (Gen 1)");
            sb.AppendLine($"  GC Collection Count: {GC.CollectionCount(2)} (Gen 2)");

            Log(level, sb.ToString(), "Memory");
        }

        /// <summary>
        /// Formats bytes to a human-readable string.
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < suffixes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:F2} {suffixes[order]}";
        }

        /// <summary>
        /// Triggers garbage collection and logs memory usage before and after.
        /// </summary>
        public static void CollectGarbage(LogLevel level = LogLevel.DEBUG)
        {
            Log(level, "Memory before GC:", "Memory");
            LogMemoryUsage(level);

            // Force full garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Log(level, "Memory after GC:", "Memory");
            LogMemoryUsage(level);
        }

        #endregion

        #region Unity-Specific Logging

        /// <summary>
        /// Logs information about the GameObject.
        /// </summary>
        public static void LogGameObject(GameObject gameObject, bool includeComponents = true,
            bool includeChildren = false, LogLevel level = LogLevel.DEBUG)
        {
            if (gameObject == null)
            {
                Warning("Cannot log null GameObject", "Unity");
                return;
            }

            StringBuilder sb = new();
            sb.AppendLine($"GameObject: {gameObject.name}");
            sb.AppendLine($"  Active: {gameObject.activeSelf}");
            sb.AppendLine($"  Layer: {LayerMask.LayerToName(gameObject.layer)}");
            sb.AppendLine($"  Tag: {gameObject.tag}");
            sb.AppendLine($"  Scene: {gameObject.scene.name}");
            sb.AppendLine($"  Static: {gameObject.isStatic}");

            if (includeComponents)
            {
                Component[] components = gameObject.GetComponents<Component>();
                sb.AppendLine($"  Components ({components.Length}):");
                foreach (Component component in components)
                    sb.AppendLine(component == null ? "    [Null Component]" : $"    {component.GetType().Name}");
            }

            if (includeChildren)
            {
                int childCount = gameObject.transform.childCount;
                sb.AppendLine($"  Children ({childCount}):");
                for (int i = 0; i < childCount; i++)
                {
                    Transform child = gameObject.transform.GetChild(i);
                    sb.AppendLine($"    {child.name}");
                }
            }

            Log(level, sb.ToString(), "Unity");
        }

        /// <summary>
        /// Logs information about the scene.
        /// </summary>
        public static void LogSceneInfo(LogLevel level = LogLevel.DEBUG)
        {
            StringBuilder sb = new();
            sb.AppendLine("Scene Information:");

            // Active scene
            Scene activeScene = SceneManager.GetActiveScene();
            sb.AppendLine($"  Active Scene: {activeScene.name}");
            sb.AppendLine($"  Path: {activeScene.path}");
            sb.AppendLine($"  Build Index: {activeScene.buildIndex}");
            sb.AppendLine($"  Is Loaded: {activeScene.isLoaded}");

            // Root GameObjects in active scene
            GameObject[] rootObjects = activeScene.GetRootGameObjects();
            sb.AppendLine($"  Root Objects: {rootObjects.Length}");

            // Loaded scenes
            int sceneCount = SceneManager.sceneCount;
            sb.AppendLine($"  Loaded Scenes: {sceneCount}");
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                sb.AppendLine($"    {i}: {scene.name} (Path: {scene.path})");
            }

            Log(level, sb.ToString(), "Unity");
        }

        /// <summary>
        /// Logs system information including device, OS, and Unity version.
        /// </summary>
        public static void LogSystemInfo(LogLevel level = LogLevel.INFO)
        {
            StringBuilder sb = new();
            sb.AppendLine("System Information:");

            // Product info
            sb.AppendLine($"  Product: {Application.productName} (v{Application.version})");
            sb.AppendLine($"  Unity Version: {Application.unityVersion}");
            sb.AppendLine($"  Platform: {Application.platform}");

            // Device info
            sb.AppendLine($"  Device: {SystemInfo.deviceModel}");
            sb.AppendLine($"  Device Name: {SystemInfo.deviceName}");
            sb.AppendLine($"  Device Type: {SystemInfo.deviceType}");
            sb.AppendLine($"  OS: {SystemInfo.operatingSystem}");

            // System memory
            sb.AppendLine($"  System Memory: {SystemInfo.systemMemorySize} MB");

            // CPU
            sb.AppendLine($"  CPU: {SystemInfo.processorType}");
            sb.AppendLine($"  CPU Frequency: {SystemInfo.processorFrequency} MHz");
            sb.AppendLine($"  CPU Cores: {SystemInfo.processorCount}");

            // GPU
            sb.AppendLine($"  GPU: {SystemInfo.graphicsDeviceName}");
            sb.AppendLine($"  GPU Vendor: {SystemInfo.graphicsDeviceVendor}");
            sb.AppendLine($"  GPU Version: {SystemInfo.graphicsDeviceVersion}");
            sb.AppendLine($"  GPU Memory: {SystemInfo.graphicsMemorySize} MB");

            // Screen
            sb.AppendLine($"  Screen Size: {Screen.width}x{Screen.height}");
            sb.AppendLine($"  Screen DPI: {Screen.dpi}");
            sb.AppendLine($"  Full Screen: {Screen.fullScreen}");

            // Other
            sb.AppendLine($"  Battery Level: {SystemInfo.batteryLevel}");
            sb.AppendLine($"  Battery Status: {SystemInfo.batteryStatus}");
            sb.AppendLine($"  Internet Reachability: {Application.internetReachability}");

            Log(level, sb.ToString(), "System");
        }

        #endregion
    }
}