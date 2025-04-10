using System;
using System.IO;
using UnityEngine;

namespace ByteForge.Runtime
{
    /// <summary>
    /// Defines severity levels for log messages in the ByteForge logging system.
    /// </summary>
    /// <remarks>
    /// These log levels follow standard severity conventions with INFO being the least
    /// severe and FATAL being the most severe. The level determines how messages are
    /// displayed in the console and how they're formatted in log files.
    /// </remarks>
    public enum BFLogLevel
    {
        /// <summary>
        /// Informational messages that track the normal flow of the application.
        /// </summary>
        INFO,
        
        /// <summary>
        /// Potentially harmful situations that may lead to problems if not addressed.
        /// </summary>
        WARNING,
        
        /// <summary>
        /// Error events that might still allow the application to continue running.
        /// </summary>
        ERROR,
        
        /// <summary>
        /// Very severe error events that will likely lead to application crashes.
        /// </summary>
        FATAL
    }

    /// <summary>
    /// Centralized logging system for the ByteForge framework.
    /// </summary>
    /// <remarks>
    /// BFDebug provides a unified interface for logging messages to both the Unity console
    /// and persistent log files. It automatically archives old logs and creates new log files
    /// for each session. The system adds timestamps, thread IDs, log levels, and type information
    /// to logged messages for comprehensive debugging.
    /// </remarks>
    public static class BFDebug
    {
        /// <summary>
        /// Path to the folder where log files are stored.
        /// </summary>
        private static string logFolderPath = Path.Combine(Application.persistentDataPath, "Logs");
        
        /// <summary>
        /// Path to the folder where archived log files are stored.
        /// </summary>
        private static string logArchiveFolderPath = Path.Combine(Application.persistentDataPath, "Logs", "Archive");
        
        /// <summary>
        /// Path to the current log file.
        /// </summary>
        private static string currentLogFilePath = Path.Combine(logFolderPath, "LatestLog.txt");
        
        /// <summary>
        /// Whether the logging system has been initialized.
        /// </summary>
        private static bool isInitialized = false;
        
        /// <summary>
        /// Initializes the logging system, creating necessary directories and setting up the log file.
        /// </summary>
        /// <remarks>
        /// This method creates the log directories if they don't exist, archives any existing log file,
        /// creates a new log file, and writes an initialization message. It's called automatically
        /// on the first log message if the system hasn't been initialized yet.
        /// </remarks>
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            logFolderPath = Path.Combine(Application.persistentDataPath, "Logs");
            logArchiveFolderPath = Path.Combine(Application.persistentDataPath, "Logs", "Archive");
            currentLogFilePath = Path.Combine(logFolderPath, "LatestLog.txt");
            
            Directory.CreateDirectory(logFolderPath);
            Directory.CreateDirectory(logArchiveFolderPath);
            
            // Archive the old log file
            if (File.Exists(currentLogFilePath))
            {
                string archivedLogFilePath = Path.Combine(logArchiveFolderPath, $"Log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
                File.Move(currentLogFilePath, archivedLogFilePath);
            }
            
            // Create a new log file
            File.Create(currentLogFilePath).Dispose();
            
            // Log system initialization
            string initMessage = $"Log system initialized at {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            File.AppendAllText(currentLogFilePath, FormatFileLogMessage(initMessage, BFLogLevel.INFO));
            
            isInitialized = true;
        }
        
        /// <summary>
        /// Logs a warning message to both the file and console.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <remarks>
        /// This is a convenience method that logs a message with WARNING level.
        /// In the Unity Editor, this appears with yellow warning coloring.
        /// </remarks>
        // ReSharper disable Unity.PerformanceAnalysis
        public static void LogWarning(object message)
        {
            Log(message, BFLogLevel.WARNING);
        }

        /// <summary>
        /// Logs an error message to both the file and console.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <remarks>
        /// This is a convenience method that logs a message with ERROR level.
        /// In the Unity Editor, this appears with red error coloring.
        /// </remarks>
        // ReSharper disable Unity.PerformanceAnalysis
        public static void LogError(object message)
        {
            Log(message, BFLogLevel.ERROR);
        }

        /// <summary>
        /// Logs a fatal error message to both the file and console, and may throw an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <remarks>
        /// This is a convenience method that logs a message with FATAL level.
        /// In the Unity Editor, this appears with red error coloring and also throws
        /// an exception to immediately alert developers to the critical issue.
        /// </remarks>
        // ReSharper disable Unity.PerformanceAnalysis
        public static void LogFatal(object message)
        {
            Log(message, BFLogLevel.FATAL);
        }

        /// <summary>
        /// Logs a message with the specified severity level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The severity level of the message. Defaults to INFO.</param>
        /// <remarks>
        /// This is the main logging method that handles initialization if needed,
        /// logs the message to the file with detailed information (timestamp, thread ID, etc.),
        /// and logs to the Unity console with appropriate formatting based on severity.
        /// </remarks>
        // ReSharper disable Unity.PerformanceAnalysis
        public static void Log(object message, BFLogLevel level = BFLogLevel.INFO)
        {
            if (!isInitialized) 
                Initialize();
                
            message ??= "Null";
            
            // Log to file (verbose format)
            LogToFile(message, level);
            
            // Log to console (clean format)
            LogToConsole(message, level);
        }
        
        /// <summary>
        /// Writes a log message to the current log file.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The severity level of the message.</param>
        /// <remarks>
        /// This method formats the message with detailed information and appends it to the current log file.
        /// </remarks>
        private static void LogToFile(object message, BFLogLevel level)
        {
            string formattedMessage = FormatFileLogMessage(message, level);
            File.AppendAllText(currentLogFilePath, formattedMessage);
        }
        
        /// <summary>
        /// Formats a message for file logging with detailed information.
        /// </summary>
        /// <param name="message">The message to format.</param>
        /// <param name="level">The severity level of the message.</param>
        /// <returns>A formatted string with timestamp, thread ID, log level, and type information.</returns>
        /// <remarks>
        /// The formatted message follows the pattern:
        /// [Timestamp] [ThreadID] [LogLevel] [MessageType]: Message content
        /// </remarks>
        private static string FormatFileLogMessage(object message, BFLogLevel level)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string threadId = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString();
            string messageType = message.GetType().ToString();
            string logLevel = level.ToString();
            
            return $"[{timestamp}] [{threadId}] [{logLevel}] [{messageType}]: {message}\n";
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Logs a message to the Unity console with appropriate formatting based on severity.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The severity level of the message.</param>
        /// <remarks>
        /// In the Unity Editor, this method uses Debug.Log, Debug.LogWarning, or Debug.LogError
        /// based on the severity level. For FATAL errors, it also throws an exception.
        /// In builds, this method does nothing as Unity's debug logging is typically disabled.
        /// </remarks>
        private static void LogToConsole(object message, BFLogLevel level)
        {
            #if UNITY_EDITOR
                switch (level)
                {
                    case BFLogLevel.INFO:
                        Debug.Log(message.ToString());
                        break;
                    case BFLogLevel.WARNING:
                        Debug.LogWarning(message.ToString());
                        break;
                    case BFLogLevel.ERROR:
                        Debug.LogError(message.ToString());
                        break;
                    case BFLogLevel.FATAL:
                        Debug.LogError($"FATAL: {message}");
                        throw new("Fatal Error: " + message);
                }
            #endif
        }
    }
}