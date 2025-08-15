using System;
using System.IO;
using System.Windows;
using FastScreeny.Models;
using FastScreeny.Services;

namespace FastScreeny
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        private readonly Action _onSaved;

        public SettingsWindow(SettingsService settingsService, Action onSaved)
        {
            InitializeComponent();
            _settingsService = settingsService;
            _onSaved = onSaved;

            SavePathBox.Text = _settingsService.Settings.SaveDirectory;
            FileNamePatternBox.Text = _settingsService.Settings.FileNamePattern;
            HotkeyRegionBox.Text = _settingsService.Settings.HotkeyRegion.Original;
            AutoCopyCheck.IsChecked = _settingsService.Settings.AutoCopyToClipboard;
            LaunchOnStartupCheck.IsChecked = _settingsService.Settings.LaunchOnStartup;
            DefaultOpenInEditorCheck.IsChecked = _settingsService.Settings.DefaultOpenInEditor;
            
            // 自动更新设置
            AutoCheckUpdatesCheck.IsChecked = _settingsService.Settings.AutoCheckUpdates;
            UpdateIntervalBox.Text = _settingsService.Settings.UpdateCheckIntervalHours.ToString();
            LastUpdateCheckText.Text = _settingsService.Settings.LastUpdateCheck == DateTime.MinValue 
                ? "Never" 
                : _settingsService.Settings.LastUpdateCheck.ToString("yyyy-MM-dd HH:mm:ss");
            
            InitBorderPresets();
            EnableBorderCheck.IsChecked = _settingsService.Settings.EnableBorder;
            BorderThicknessBox.Text = _settingsService.Settings.BorderThickness.ToString();
            BorderStartColorBox.Text = _settingsService.Settings.BorderGradientStart;
            BorderEndColorBox.Text = _settingsService.Settings.BorderGradientEnd;
            BorderPresetBox.SelectedItem = _settingsService.Settings.BorderPreset;
            BorderPresetBox.SelectionChanged += BorderPresetBox_SelectionChanged;
        }

        private void InitBorderPresets()
        {
            BorderPresetBox.Items.Clear();
            var presets = new[]
            {
                "Custom",
                "Purple Pink Dream",
                "Rainbow Spectrum",
                "Misty White",
                "Warm Orange Gold",
                "Dark Night Blue",
                "Fluorescent Cyan Purple"
            };
            foreach (var p in presets) BorderPresetBox.Items.Add(p);
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = Directory.Exists(SavePathBox.Text) ? SavePathBox.Text : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SavePathBox.Text = dialog.SelectedPath;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _settingsService.Settings.SaveDirectory = string.IsNullOrWhiteSpace(SavePathBox.Text)
                ? StoragePaths.GetDefaultSaveDirectory()
                : SavePathBox.Text.Trim();
            _settingsService.Settings.FileNamePattern = string.IsNullOrWhiteSpace(FileNamePatternBox.Text)
                ? "screenshot_{yyyyMMdd_HHmmss}.png"
                : FileNamePatternBox.Text.Trim();
            _settingsService.Settings.HotkeyRegion = Hotkey.Parse(HotkeyRegionBox.Text);
            _settingsService.Settings.AutoCopyToClipboard = AutoCopyCheck.IsChecked == true;
            _settingsService.Settings.LaunchOnStartup = LaunchOnStartupCheck.IsChecked == true;
            _settingsService.Settings.DefaultOpenInEditor = DefaultOpenInEditorCheck.IsChecked == true;
            
            // 保存自动更新设置
            _settingsService.Settings.AutoCheckUpdates = AutoCheckUpdatesCheck.IsChecked == true;
            if (int.TryParse(UpdateIntervalBox.Text, out var hours) && hours >= 1 && hours <= 168) // 1小时到1周
            {
                _settingsService.Settings.UpdateCheckIntervalHours = hours;
            }
            
            _settingsService.Settings.EnableBorder = EnableBorderCheck.IsChecked == true;
            if (int.TryParse(BorderThicknessBox.Text, out var px) && px >= 0 && px <= 512) {
                _settingsService.Settings.BorderThickness = px;
            }
            _settingsService.Settings.BorderGradientStart = string.IsNullOrWhiteSpace(BorderStartColorBox.Text) ? "#FF5B8DEF" : BorderStartColorBox.Text.Trim();
            _settingsService.Settings.BorderGradientEnd = string.IsNullOrWhiteSpace(BorderEndColorBox.Text) ? "#FF84A8FF" : BorderEndColorBox.Text.Trim();
            _settingsService.Settings.BorderPreset = (BorderPresetBox.SelectedItem as string) ?? "Custom";

            _onSaved();
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    partial class SettingsWindow
    {
        private void BorderPresetBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var preset = (BorderPresetBox.SelectedItem as string) ?? "Custom";
            switch (preset)
            {
                case "Purple Pink Dream":
                    BorderThicknessBox.Text = "40";
                    BorderStartColorBox.Text = "#FF8B5CF6";
                    BorderEndColorBox.Text = "#FFEC4899";
                    break;
                case "Rainbow Spectrum":
                    BorderThicknessBox.Text = "45";
                    BorderStartColorBox.Text = "#FF8B5CF6";
                    BorderEndColorBox.Text = "#FFFBBF24";
                    break;
                case "Misty White":
                    BorderThicknessBox.Text = "36";
                    BorderStartColorBox.Text = "#FFFFFFFF";
                    BorderEndColorBox.Text = "#FFEFEFEF";
                    break;
                case "Warm Orange Gold":
                    BorderThicknessBox.Text = "32";
                    BorderStartColorBox.Text = "#FFFF6B35";
                    BorderEndColorBox.Text = "#FFFBBF24";
                    break;
                case "Dark Night Blue":
                    BorderThicknessBox.Text = "30";
                    BorderStartColorBox.Text = "#FF0F172A";
                    BorderEndColorBox.Text = "#FF1E293B";
                    break;
                case "Fluorescent Cyan Purple":
                    BorderThicknessBox.Text = "35";
                    BorderStartColorBox.Text = "#FF06B6D4";
                    BorderEndColorBox.Text = "#FF8B5CF6";
                    break;
                default:
                    break;
            }
        }
    }
}


