using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ByteForge.Runtime
{
    /// <summary>
    /// Provides cross-platform operating system utilities and native functionality.
    /// </summary>
    /// <remarks>
    /// The OS class offers platform detection and native OS-specific functionality
    /// that's not readily available through Unity's standard APIs. It provides a
    /// consistent interface across Windows, macOS, and Linux platforms, with appropriate
    /// fallbacks when platform-specific functionality is not available.
    /// 
    /// Primary features include:
    /// - Platform detection constants
    /// - Native message box display
    /// 
    /// This class helps bridge the gap between Unity's cross-platform abstraction
    /// and native OS capabilities when needed.
    /// </remarks>
    public class OS
    {
        /// <summary>
        /// Indicates whether the application is running on Windows.
        /// </summary>
        /// <remarks>
        /// This is true for both the Windows Player and Windows Editor.
        /// </remarks>
        public static readonly bool IS_WINDOWS = Application.platform == RuntimePlatform.WindowsEditor ||
                                                 Application.platform == RuntimePlatform.WindowsPlayer;

        /// <summary>
        /// Indicates whether the application is running on macOS.
        /// </summary>
        /// <remarks>
        /// This is true for both the macOS Player and macOS Editor.
        /// </remarks>
        public static readonly bool IS_MAC = Application.platform == RuntimePlatform.OSXEditor ||
                                             Application.platform == RuntimePlatform.OSXPlayer;

        /// <summary>
        /// Indicates whether the application is running on Linux.
        /// </summary>
        /// <remarks>
        /// This is true for both the Linux Player and Linux Editor.
        /// </remarks>
        public static readonly bool IS_LINUX = Application.platform == RuntimePlatform.LinuxEditor ||
                                               Application.platform == RuntimePlatform.LinuxPlayer;

        /// <summary>
        /// Windows API for displaying native message boxes.
        /// </summary>
        /// <param name="hWnd">Handle to the owner window of the message box.</param>
        /// <param name="text">The message to be displayed.</param>
        /// <param name="caption">The title of the message box.</param>
        /// <param name="type">The type and behavior of the message box.</param>
        /// <returns>The value of the selected button.</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);

        /// <summary>
        /// Defines the types of message boxes available for display.
        /// </summary>
        public enum MessageBoxType
        {
            /// <summary>
            /// Information message box with an "i" icon.
            /// </summary>
            INFORMATION = 0x00000000,
            
            /// <summary>
            /// Warning message box with an exclamation mark icon.
            /// </summary>
            WARNING = 0x00000030,
            
            /// <summary>
            /// Error message box with an "X" icon.
            /// </summary>
            ERROR = 0x00000010,
            
            /// <summary>
            /// Question message box with a question mark icon.
            /// </summary>
            QUESTION = 0x00000020
        }

        /// <summary>
        /// Shows a native message box on the current operating system.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="title">The title of the message box. Defaults to "Message".</param>
        /// <param name="type">The type of message box to display. Defaults to MessageBoxType.INFORMATION.</param>
        /// <remarks>
        /// This method provides a consistent way to display native message boxes across
        /// Windows, macOS, and Linux. On Windows, it uses the Windows API. On macOS,
        /// it uses AppleScript. On Linux, it attempts to use either zenity (GNOME) or
        /// kdialog (KDE) depending on what's available.
        /// 
        /// If none of the native methods are available, it falls back to logging the message
        /// using BFDebug.
        /// 
        /// Example usage:
        /// <code>
        /// OS.ShowMessage("Save completed successfully!", "Save Status", OS.MessageBoxType.INFORMATION);
        /// OS.ShowMessage("File not found!", "Error", OS.MessageBoxType.ERROR);
        /// </code>
        /// </remarks>
        public static void ShowMessage(string message, string title = "Message",
            MessageBoxType type = MessageBoxType.INFORMATION)
        {
            if (IS_WINDOWS)
                ShowWindowsMessage(message, title, type);
            else if (IS_MAC)
                ShowMacMessage(message, title, type);
            else if (IS_LINUX)
                ShowLinuxMessage(message, title, type);
            else
                BFDebug.Log($"Message: {title} - {message}");
        }

        /// <summary>
        /// Shows a message box using the Windows API.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="title">The title of the message box.</param>
        /// <param name="type">The type of message box to display.</param>
        /// <remarks>
        /// This method uses the Windows MessageBox API to display a native Windows message box.
        /// It handles the platform-specific implementation details for Windows.
        /// </remarks>
        private static void ShowWindowsMessage(string message, string title, MessageBoxType type)
        {
            MessageBox(IntPtr.Zero, message, title, (uint)type);
        }

        /// <summary>
        /// Shows a message box on macOS using AppleScript.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="title">The title of the message box.</param>
        /// <param name="type">The type of message box to display.</param>
        /// <remarks>
        /// This method creates a temporary AppleScript file and executes it to display
        /// a native macOS message box. It cleans up the temporary script file afterward.
        /// </remarks>
        private static void ShowMacMessage(string message, string title, MessageBoxType type)
        {
            string typeParam = "";

            switch (type)
            {
                case MessageBoxType.INFORMATION:
                    typeParam = "note";
                    break;
                case MessageBoxType.WARNING:
                    typeParam = "caution";
                    break;
                case MessageBoxType.ERROR:
                    typeParam = "stop";
                    break;
                case MessageBoxType.QUESTION:
                    typeParam = "note";
                    break;
            }

            // Escape quotes in message and title for AppleScript
            message = message.Replace("\"", "\\\"");
            title = title.Replace("\"", "\\\"");

            string appleScriptCommand =
                $"display dialog \"{message}\" with title \"{title}\" buttons {{\"OK\"}} with icon {typeParam}";

            string scriptPath = Path.Combine(Path.GetTempPath(), "unity_msgbox.scpt");
            File.WriteAllText(scriptPath, appleScriptCommand);

            ProcessStartInfo psi = new()
            {
                FileName = "osascript",
                Arguments = scriptPath,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = new();
            process.StartInfo = psi;
            process.Start();
            process.WaitForExit();

            try
            {
                File.Delete(scriptPath);
            }
            catch
            {
                // Ignore file deletion errors
            }
        }

        /// <summary>
        /// Shows a message box on Linux using zenity or kdialog.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="title">The title of the message box.</param>
        /// <param name="type">The type of message box to display.</param>
        /// <remarks>
        /// This method attempts to use zenity (for GNOME) or kdialog (for KDE) to display
        /// a native Linux message box. It falls back to logging the message if neither is available.
        /// 
        /// The method first tries zenity, and if that fails, it tries kdialog.
        /// This provides support for the two most common Linux desktop environments.
        /// </remarks>
        private static void ShowLinuxMessage(string message, string title, MessageBoxType type)
        {
            string iconType = "";

            switch (type)
            {
                case MessageBoxType.INFORMATION:
                    iconType = "--info";
                    break;
                case MessageBoxType.WARNING:
                    iconType = "--warning";
                    break;
                case MessageBoxType.ERROR:
                    iconType = "--error";
                    break;
                case MessageBoxType.QUESTION:
                    iconType = "--question";
                    break;
            }

            // Escape quotes for shell
            message = message.Replace("\"", "\\\"");
            title = title.Replace("\"", "\\\"");

            // Try using zenity first (GNOME)
            ProcessStartInfo psi = new()
            {
                FileName = "zenity",
                Arguments = $"{iconType} --title=\"{title}\" --text=\"{message}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = new();

            try
            {
                process.StartInfo = psi;
                process.Start();
                process.WaitForExit();
            }
            catch
            {
                // If zenity fails, try using kdialog (KDE)
                try
                {
                    psi = new()
                    {
                        FileName = "kdialog"
                    };

                    string kdeIconType = "";
                    switch (type)
                    {
                        case MessageBoxType.INFORMATION:
                            kdeIconType = "--msgbox";
                            break;
                        case MessageBoxType.WARNING:
                            kdeIconType = "--sorry";
                            break;
                        case MessageBoxType.ERROR:
                            kdeIconType = "--error";
                            break;
                        case MessageBoxType.QUESTION:
                            kdeIconType = "--questionyesno";
                            break;
                    }

                    psi.Arguments = $"{kdeIconType} \"{message}\" --title \"{title}\"";
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;

                    process = new();
                    process.StartInfo = psi;
                    process.Start();
                    process.WaitForExit();
                }
                catch
                {
                    // If all else fails, log to console
                    BFDebug.Log($"Message: {title} - {message}");
                }
            }
        }
    }
}