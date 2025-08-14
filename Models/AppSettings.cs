using System;

namespace FastScreeny.Models
{
    public class AppSettings
    {
        public string SaveDirectory { get; set; } = Services.StoragePaths.GetDefaultSaveDirectory();
        public string FileNamePattern { get; set; } = "screenshot_{yyyyMMdd_HHmmss}.png";
        public Hotkey HotkeyRegion { get; set; } = Hotkey.Parse("Ctrl+Alt+A");
        public bool AutoCopyToClipboard { get; set; } = true;
        public bool LaunchOnStartup { get; set; } = true;
        // Border/decoration around the captured image without covering the content
        public bool EnableBorder { get; set; } = false;
        public int BorderThickness { get; set; } = 24; // pixels
        public string BorderGradientStart { get; set; } = "#FF5B8DEF";
        public string BorderGradientEnd { get; set; } = "#FF84A8FF";
        public string BorderPreset { get; set; } = "自定义";
        // Default capture mode: direct save or open in editor
        public bool DefaultOpenInEditor { get; set; } = false;
    }
}


