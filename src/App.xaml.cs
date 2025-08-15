using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using WinForms = System.Windows.Forms;
using FastScreeny.Services;
using FastScreeny.Views;

namespace FastScreeny
{
    public partial class App : System.Windows.Application
    {
        private WinForms.NotifyIcon? _notifyIcon;
        private SettingsWindow? _settingsWindow;
        private HotkeyManager? _hotkeyManager;
        private SettingsService? _settingsService;
        private AutoUpdateService? _autoUpdateService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _settingsService = new SettingsService();
            _settingsService.Load();

            _hotkeyManager = new HotkeyManager(Current);
            
            // 初始化自动更新服务
            _autoUpdateService = new AutoUpdateService("FeynmanXie", "FastScreeny");
            _autoUpdateService.UpdateAvailable += OnUpdateAvailable;

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

            // 检查更新
            _ = CheckForUpdatesAsync();
        }

        private void InitializeTray()
        {
            _notifyIcon = new WinForms.NotifyIcon
            {
                Text = "FastScreeny",
                Icon = LoadIconFromResource(),
                Visible = true
            };

            var contextMenu = new WinForms.ContextMenuStrip();
            contextMenu.Items.Add("Settings", null, (s, e) => ShowSettings());
            contextMenu.Items.Add("Region Screenshot", null, async (s, e) => await ScreenCaptureService.CaptureRegionToDefaultAsync(_settingsService!));
            contextMenu.Items.Add("Region Screenshot (Save Directly)", null, async (s, e) => await ScreenCaptureService.CaptureRegionAndSaveAsync(_settingsService!));
            contextMenu.Items.Add("Region Screenshot (Edit Mode)", null, async (s, e) => await ScreenCaptureService.CaptureRegionAndEditAsync(_settingsService!));
            contextMenu.Items.Add(new WinForms.ToolStripSeparator());
            contextMenu.Items.Add("Check for Updates", null, async (s, e) => await ManualCheckForUpdatesAsync());
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
            _autoUpdateService?.Dispose();
            Shutdown();
        }

        private Icon LoadIconFromResource()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("FastScreeny.public.favicon.ico");
                if (stream != null)
                {
                    return new Icon(stream);
                }
            }
            catch
            {
                // Fallback to system icon if loading fails
            }
            return SystemIcons.Application;
        }

        private async Task CheckForUpdatesAsync()
        {
            if (_settingsService == null || _autoUpdateService == null)
                return;

            // 检查是否启用自动检查更新
            if (!_settingsService.Settings.AutoCheckUpdates)
                return;

            // 检查距离上次检查是否超过设定间隔
            var timeSinceLastCheck = DateTime.Now - _settingsService.Settings.LastUpdateCheck;
            if (timeSinceLastCheck.TotalHours < _settingsService.Settings.UpdateCheckIntervalHours)
                return;

            try
            {
                await _autoUpdateService.CheckForUpdatesAsync();
                
                // 更新最后检查时间
                _settingsService.Settings.LastUpdateCheck = DateTime.Now;
                _settingsService.Save();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"自动检查更新失败: {ex.Message}");
            }
        }

        private async Task ManualCheckForUpdatesAsync()
        {
            if (_autoUpdateService == null)
                return;

            try
            {
                _notifyIcon?.ShowBalloonTip(3000, "Check for Updates", "Checking for latest version...", WinForms.ToolTipIcon.Info);
                
                var updateInfo = await _autoUpdateService.CheckForUpdatesAsync();
                
                if (updateInfo != null)
                {
                    if (updateInfo.IsUpdateAvailable)
                    {
                        ShowUpdateWindow(updateInfo);
                    }
                    else
                    {
                        _notifyIcon?.ShowBalloonTip(3000, "Check for Updates", "You are running the latest version!", WinForms.ToolTipIcon.Info);
                    }
                }
                else
                {
                    _notifyIcon?.ShowBalloonTip(3000, "Check for Updates", "Update check failed. Please check your network connection.", WinForms.ToolTipIcon.Warning);
                }

                // 更新最后检查时间
                if (_settingsService != null)
                {
                    _settingsService.Settings.LastUpdateCheck = DateTime.Now;
                    _settingsService.Save();
                }
            }
            catch (Exception ex)
            {
                _notifyIcon?.ShowBalloonTip(3000, "Check for Updates", $"Error checking for updates: {ex.Message}", WinForms.ToolTipIcon.Error);
            }
        }

        private void OnUpdateAvailable(object? sender, Models.UpdateInfo updateInfo)
        {
            Dispatcher.Invoke(() =>
            {
                _notifyIcon?.ShowBalloonTip(5000, "New Version Available", 
                    $"FastScreeny {updateInfo.LatestVersion} is now available! Click to view details.", 
                    WinForms.ToolTipIcon.Info);
                
                ShowUpdateWindow(updateInfo);
            });
        }

        private void ShowUpdateWindow(Models.UpdateInfo updateInfo)
        {
            if (_autoUpdateService == null) return;

            var updateWindow = new UpdateWindow(_autoUpdateService, updateInfo);
            updateWindow.Show();
        }
    }
}


