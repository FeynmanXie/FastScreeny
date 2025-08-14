using System;
using System.Linq;
using System.Windows.Input;

namespace FastScreeny.Models
{
    public class Hotkey
    {
        public ModifierKeys Modifiers { get; set; }
        public Key Key { get; set; }
        public string Original { get; set; } = string.Empty;

        public static Hotkey Parse(string? text)
        {
            var hk = new Hotkey();
            hk.Original = text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                hk.Modifiers = ModifierKeys.Control | ModifierKeys.Alt;
                hk.Key = Key.F12;
                return hk;
            }

            var parts = text!.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var raw in parts)
            {
                var part = raw.Trim();
                var token = part.Replace(" ", string.Empty).ToLowerInvariant();

                // Modifiers aliases
                switch (token)
                {
                    case "ctrl":
                    case "ctl":
                    case "control":
                        hk.Modifiers |= ModifierKeys.Control;
                        continue;
                    case "alt":
                        hk.Modifiers |= ModifierKeys.Alt;
                        continue;
                    case "shift":
                        hk.Modifiers |= ModifierKeys.Shift;
                        continue;
                    case "win":
                    case "windows":
                    case "lwin":
                    case "rwin":
                    case "meta":
                    case "cmd":
                        hk.Modifiers |= ModifierKeys.Windows;
                        continue;
                }

                // Try known key names
                if (Enum.TryParse<Key>(part, true, out var parsedKey))
                {
                    hk.Key = parsedKey;
                    continue;
                }

                // Fallback: single character like "A" or digit
                if (part.Length == 1)
                {
                    char c = part[0];
                    if (char.IsLetter(c))
                    {
                        hk.Key = (Key)Enum.Parse(typeof(Key), char.ToUpperInvariant(c).ToString());
                    }
                    else if (char.IsDigit(c))
                    {
                        // D0..D9
                        hk.Key = (Key)Enum.Parse(typeof(Key), $"D{c}");
                    }
                }
            }

            if (hk.Key == Key.None)
            {
                hk.Key = Key.F12;
            }
            return hk;
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Original)
                ? $"{Modifiers}+{Key}"
                : Original;
        }
    }
}


