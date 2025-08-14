using System;
using System.Windows;
using WinForms = System.Windows.Forms;
using FastScreeny.Services;

namespace FastScreeny
{
    public partial class App : System.Windows.Application
    {
        private WinForms.NotifyIcon? _notifyIcon;
        private SettingsWindow? _settingsWindow;
        private HotkeyManager? _hotkeyManager;
        private SettingsService? _settingsService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _settingsService = new SettingsService();
            _settingsService.Load();

            _hotkeyManager = new HotkeyManager(Current);

            bool runInBackground = e.Args != null && Array.Exists(e.Args, a => string.Equals(a, "--background", StringComparison.OrdinalIgnoreCase));
            if (!runInBackground)
            {
                ShowSettings();
            }

            InitializeTray();
            RegisterHotkeys();

            if (_settingsService.Settings.LaunchOnStartup)
            {
                AutoStartManager.Enable();
            }
            else
            {
                AutoStartManager.Disable();
            }
        }

        private void InitializeTray()
        {
            _notifyIcon = new WinForms.NotifyIcon
            {
                Text = "FastScreeny",
                Icon = System.Drawing.SystemIcons.Application,
                Visible = true
            };

            var contextMenu = new WinForms.ContextMenuStrip();
            contextMenu.Items.Add("Settings", null, (s, e) => ShowSettings());
            contextMenu.Items.Add("Region Screenshot", null, async (s, e) => await ScreenCaptureService.CaptureRegionToDefaultAsync(_settingsService!));
            contextMenu.Items.Add("Region Screenshot (Save Directly)", null, async (s, e) => await ScreenCaptureService.CaptureRegionAndSaveAsync(_settingsService!));
            contextMenu.Items.Add("Region Screenshot (Edit Mode)", null, async (s, e) => await ScreenCaptureService.CaptureRegionAndEditAsync(_settingsService!));
            contextMenu.Items.Add(new WinForms.ToolStripSeparator());
            contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());
            _notifyIcon.ContextMenuStrip = contextMenu;

            _notifyIcon.DoubleClick += (s, e) => ShowSettings();
        }

        private void RegisterHotkeys()
        {
            if (_hotkeyManager == null || _settingsService == null) return;

            _hotkeyManager.UnregisterAll();

            var ok = _hotkeyManager.RegisterHotkey(_settingsService.Settings.HotkeyRegion, async () =>
            {
                await ScreenCaptureService.CaptureRegionToDefaultAsync(_settingsService);
            });
            if (!ok)
            {
                _notifyIcon?.ShowBalloonTip(3000, "Hotkey Registration Failed", $"{_settingsService.Settings.HotkeyRegion} may be occupied by other programs or requires administrator privileges.", WinForms.ToolTipIcon.Warning);
            }
        }

        private void ShowSettings()
        {
            if (_settingsWindow == null)
            {
                _settingsWindow = new SettingsWindow(_settingsService!, ApplySettingsChanges);
                _settingsWindow.Closed += (s, e) => _settingsWindow = null;
            }
            _settingsWindow.Show();
            _settingsWindow.Activate();
        }

        private void ApplySettingsChanges()
        {
            _settingsService!.Save();
            RegisterHotkeys();

            if (_settingsService.Settings.LaunchOnStartup)
                AutoStartManager.Enable();
            else
                AutoStartManager.Disable();
        }

        private void ExitApplication()
        {
            _notifyIcon!.Visible = false;
            _hotkeyManager?.Dispose();
            Shutdown();
        }
    }
}


