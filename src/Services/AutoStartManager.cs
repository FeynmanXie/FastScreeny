using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;

namespace FastScreeny.Services
{
    public static class AutoStartManager
    {
        private const string RunKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string AppName = "FastScreeny";

        public static void Enable()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true) ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);
            var exePath = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
            key!.SetValue(AppName, "\"" + exePath + "\" --background");
        }

        public static void Disable()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            key?.DeleteValue(AppName, throwOnMissingValue: false);
        }

        public static bool IsEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            var value = key?.GetValue(AppName) as string;
            return !string.IsNullOrEmpty(value);
        }
    }
}


