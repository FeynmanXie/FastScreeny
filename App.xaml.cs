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
            contextMenu.Items.Add("设置", null, (s, e) => ShowSettings());
            contextMenu.Items.Add("区域截图", null, async (s, e) => await ScreenCaptureService.CaptureRegionToDefaultAsync(_settingsService!));
            contextMenu.Items.Add("区域截图（直接保存）", null, async (s, e) => await ScreenCaptureService.CaptureRegionAndSaveAsync(_settingsService!));
            contextMenu.Items.Add("区域截图（编辑模式）", null, async (s, e) => await ScreenCaptureService.CaptureRegionAndEditAsync(_settingsService!));
            contextMenu.Items.Add(new WinForms.ToolStripSeparator());
            contextMenu.Items.Add("退出", null, (s, e) => ExitApplication());
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
                _notifyIcon?.ShowBalloonTip(3000, "快捷键注册失败", $"{_settingsService.Settings.HotkeyRegion} 可能被其他程序占用，或需要管理员权限。", WinForms.ToolTipIcon.Warning);
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


