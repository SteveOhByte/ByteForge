using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ByteForge.Runtime
{
    public class OS
    {
        public static readonly bool IS_WINDOWS = Application.platform == RuntimePlatform.WindowsEditor ||
                                                 Application.platform == RuntimePlatform.WindowsPlayer;

        public static readonly bool IS_MAC = Application.platform == RuntimePlatform.OSXEditor ||
                                             Application.platform == RuntimePlatform.OSXPlayer;

        public static readonly bool IS_LINUX = Application.platform == RuntimePlatform.LinuxEditor ||
                                               Application.platform == RuntimePlatform.LinuxPlayer;

        // Windows API
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);

        public enum MessageBoxType
        {
            INFORMATION = 0x00000000,
            WARNING = 0x00000030,
            ERROR = 0x00000010,
            QUESTION = 0x00000020
        }

        /// <summary>
        /// Shows a native message box on the current operating system
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="title">The title of the message box</param>
        /// <param name="type">The type of message box to display</param>
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

        private static void ShowWindowsMessage(string message, string title, MessageBoxType type)
        {
            MessageBox(IntPtr.Zero, message, title, (uint)type);
        }

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