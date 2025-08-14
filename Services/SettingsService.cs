using System;
using System.IO;
using System.Text.Json;
using FastScreeny.Models;

namespace FastScreeny.Services
{
    public class SettingsService
    {
        private readonly string _settingsPath;
        public AppSettings Settings { get; private set; } = new AppSettings();

        public SettingsService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "FastScreeny");
            Directory.CreateDirectory(dir);
            _settingsPath = Path.Combine(dir, "settings.json");
        }

        public void Load()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var data = JsonSerializer.Deserialize<AppSettings>(json);
                    if (data != null)
                    {
                        Settings = data;
                        // Re-parse hotkey from original text to fix legacy parsing of aliases like Ctrl/Win
                        if (!string.IsNullOrWhiteSpace(Settings.HotkeyRegion?.Original))
                        {
                            Settings.HotkeyRegion = Hotkey.Parse(Settings.HotkeyRegion.Original);
                        }
                    }
                }
            }
            catch
            {
                // ignore invalid settings; fallback to defaults
                Settings = new AppSettings();
            }
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_settingsPath, json);
        }
    }
}


