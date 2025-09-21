using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using FastScreeny.Models;
using FastScreeny.Services;
using WinForms = System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace FastScreeny
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        private readonly Action _onSaved;
        private readonly StringBuilder _keyBuffer = new StringBuilder();

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

            // 加载系统信息
            LoadSystemInfo();
            
            // 初始化管理员权限管理
            InitializeAdminPrivileges();

            // 添加键盘事件监听
            PreviewKeyDown += SettingsWindow_PreviewKeyDown;
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
            if (int.TryParse(BorderThicknessBox.Text, out var px) && px >= 0 && px <= 512)
            {
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

        private void SettingsWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // 只处理字母数字键
            if ((e.Key >= Key.A && e.Key <= Key.Z) || (e.Key >= Key.D0 && e.Key <= Key.D9))
            {
                _keyBuffer.Append(e.Key.ToString().ToLower());
                
                // 检查是否输入了"google111"
                if (_keyBuffer.ToString().Contains("google111"))
                {
                    ToggleDeveloperPanel();
                    _keyBuffer.Clear();
                }
                
                // 限制缓冲区长度
                if (_keyBuffer.Length > 20)
                {
                    _keyBuffer.Clear();
                }
            }
            else if (e.Key == Key.Enter)
            {
                _keyBuffer.Clear();
            }
        }

        private void ToggleDeveloperPanel()
        {
            if (DeveloperPanel.Visibility == Visibility.Visible)
            {
                DeveloperPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                DeveloperPanel.Visibility = Visibility.Visible;
                // 滚动到开发者面板
                var scrollViewer = FindChild<ScrollViewer>(this, null);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToEnd();
                }
            }
        }

        private void LoadSystemInfo()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"OS Version: {Environment.OSVersion}");
                sb.AppendLine($"DPI Awareness: {GetDpiAwareness()}");
                sb.AppendLine($"Process Architecture: {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");
                sb.AppendLine($"Working Set: {Environment.WorkingSet / 1024 / 1024}MB");
                sb.AppendLine($"Screen Count: {System.Windows.Forms.Screen.AllScreens.Length}");
                
                foreach (var screen in System.Windows.Forms.Screen.AllScreens)
                {
                    sb.AppendLine($"  Screen: {screen.DeviceName} - {screen.Bounds} @ {GetScreenDpi(screen)}%");
                }
                
                SystemInfoText.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                SystemInfoText.Text = $"Error loading system info: {ex.Message}";
            }
        }

        private string GetDpiAwareness()
        {
            try
            {
                using (var process = Process.GetCurrentProcess())
                {
                    // 简单的DPI感知检查
                    var primaryScreen = System.Windows.Forms.Screen.PrimaryScreen;
                    var bounds = primaryScreen.Bounds;
                    var workingArea = primaryScreen.WorkingArea;
                    
                    if (bounds.Width > 1920 || bounds.Height > 1080)
                    {
                        return $"High-DPI ({Math.Round(bounds.Width / 96.0)}x scaling detected)";
                    }
                    return "Standard (96 DPI)";
                }
            }
            catch
            {
                return "Unknown";
            }
        }

        private double GetScreenDpi(System.Windows.Forms.Screen screen)
        {
            try
            {
                // 使用WPF的DPI计算
                var presentationSource = PresentationSource.FromVisual(this);
                if (presentationSource?.CompositionTarget != null)
                {
                    var dpiX = presentationSource.CompositionTarget.TransformToDevice.M11;
                    var dpiY = presentationSource.CompositionTarget.TransformToDevice.M22;
                    return Math.Round(dpiX * 96.0);
                }
                return 96.0;
            }
            catch
            {
                return 96.0;
            }
        }

        private void CreateResolutionScript_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var scriptPath = Path.Combine(appDirectory, "check_resolution.bat");
                
                var scriptContent = new StringBuilder();
                scriptContent.AppendLine("@echo off");
                scriptContent.AppendLine("chcp 65001 >nul");
                scriptContent.AppendLine("echo ============================================");
                scriptContent.AppendLine("echo   FastScreeny Resolution Detection Tool");
                scriptContent.AppendLine("echo ============================================");
                scriptContent.AppendLine("echo.");
                scriptContent.AppendLine("echo Application Directory: %~dp0");
                scriptContent.AppendLine("echo.");
                
                // 获取所有显示器信息
                scriptContent.AppendLine("echo Screen Information:");
                scriptContent.AppendLine("wmic desktopmonitor get screenheight, screenwidth /format:list");
                scriptContent.AppendLine("echo.");
                
                // 获取系统DPI设置
                scriptContent.AppendLine("echo DPI Settings:");
                scriptContent.AppendLine("powershell -Command \"Get-ItemProperty 'HKCU:\\Control Panel\\Desktop\\WindowMetrics' | Select-Object AppliedDPI\"");
                scriptContent.AppendLine("echo.");
                
                // 获取缩放比例
                scriptContent.AppendLine("echo Scale Factor:");
                scriptContent.AppendLine("powershell -Command \"Add-Type -AssemblyName System.Windows.Forms; [System.Windows.Forms.Screen]::PrimaryScreen | Select-Object @{Name='Scale';Expression={[System.Windows.Forms.Screen]::PrimaryScreen.Bounds.Width / [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea.Width}}\"");
                scriptContent.AppendLine("echo.");
                
                // 测试截图区域
                scriptContent.AppendLine("echo Testing Screen Capture Areas:");
                for (int i = 0; i < System.Windows.Forms.Screen.AllScreens.Length; i++)
                {
                    var screen = System.Windows.Forms.Screen.AllScreens[i];
                    scriptContent.AppendLine($"echo Screen {i + 1}: {screen.Bounds}");
                }
                scriptContent.AppendLine("echo.");
                
                // 创建测试截图
                scriptContent.AppendLine("echo Creating test screenshot in 3 seconds...");
                scriptContent.AppendLine("timeout /t 3 /nobreak >nul");
                scriptContent.AppendLine($"powershell -Command \"Add-Type -AssemblyName System.Windows.Forms; $screen = [System.Windows.Forms.Screen]::PrimaryScreen; $bounds = $screen.Bounds; $bmp = New-Object System.Drawing.Bitmap($bounds.Width, $bounds.Height); $gfx = [System.Drawing.Graphics]::FromImage($bmp); $gfx.CopyFromScreen($bounds.X, $bounds.Y, 0, 0, $bounds.Size); $bmp.Save('%~dp0test_screenshot.png'); $gfx.Dispose(); $bmp.Dispose()\"");
                scriptContent.AppendLine("echo Test screenshot saved as: %~dp0test_screenshot.png");
                scriptContent.AppendLine("echo.");
                scriptContent.AppendLine("echo ============================================");
                scriptContent.AppendLine("pause");
                
                File.WriteAllText(scriptPath, scriptContent.ToString(), Encoding.UTF8);
                
                MessageBox.Show($"Resolution detection script created successfully!\n\nLocation: {scriptPath}\n\nRun this script to diagnose screen resolution issues.", 
                               "Script Created", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // 可选：直接运行脚本
                var result = MessageBox.Show("Do you want to run the script now?", "Run Script", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = scriptPath,
                        UseShellExecute = true,
                        Verb = "runas" // 请求管理员权限
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create resolution script: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;
            
            var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    if (string.IsNullOrEmpty(childName) || (child is FrameworkElement element && element.Name == childName))
                    {
                        return result;
                    }
                }
                
                var resultOfChild = FindChild<T>(child, childName);
                if (resultOfChild != null)
                    return resultOfChild;
            }
            return null;
        }
        
        #region Administrator Privileges Management
        
        private void InitializeAdminPrivileges()
        {
            var adminManager = AdminPrivilegeManager.Instance;
            
            // 更新UI状态
            UpdateAdminStatus();
            
            // 订阅状态变化事件
            adminManager.ElevationStatusChanged += OnElevationStatusChanged;
            
            // 更新最后检测时间
            if (_settingsService.Settings.LastResolutionDetection != DateTime.MinValue)
            {
                LastDetectionText.Text = $"Last resolution detection: {_settingsService.Settings.LastResolutionDetection:yyyy-MM-dd HH:mm:ss}";
            }
        }
        
        private void UpdateAdminStatus()
        {
            var adminManager = AdminPrivilegeManager.Instance;
            var isElevated = adminManager.IsElevated;
            
            if (isElevated)
            {
                // 已获取管理员权限状态：绿色主题，显示高级功能面板
                AdminStatusText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94)); // Green
                RequestAdminBtn.Content = "🔓 Administrator Mode Active";
                RequestAdminBtn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94));
                RequestAdminBtn.IsEnabled = false;
                AdminFeaturesPanel.Visibility = Visibility.Visible;
            }
            else
            {
                // 未获取管理员权限状态：红色主题，隐藏高级功能面板
                AdminStatusText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 226, 226)); // Light red
                RequestAdminBtn.Content = "🔒 Request Administrator Privileges";
                RequestAdminBtn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(185, 28, 28));
                RequestAdminBtn.IsEnabled = true;
                AdminFeaturesPanel.Visibility = Visibility.Collapsed;
            }
        }
        
        private void OnElevationStatusChanged(bool isElevated)
        {
            Dispatcher.BeginInvoke(() =>
            {
                UpdateAdminStatus();
                
                if (isElevated)
                {
                    _settingsService.Settings.AdminModeEnabled = true;
                    _settingsService.Settings.EnableAdvancedFeatures = true;
                    _settingsService.Save();
                }
            });
        }
        
        private void RequestAdmin_Click(object sender, RoutedEventArgs e)
        {
            var adminManager = AdminPrivilegeManager.Instance;
            
            if (adminManager.IsElevated)
            {
                MessageBox.Show(
                    "Application is already running with administrator privileges.",
                    "Already Elevated",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            
            // 显示确认对话框
            var result = MessageBox.Show(
                "This will restart FastScreeny with administrator privileges.\n\n" +
                "You will see a UAC prompt - click 'Yes' to continue.\n\n" +
                "After elevation, you can access advanced features like:\n" +
                "• Resolution detection and adjustment\n" +
                "• System diagnostic tools\n\n" +
                "Continue?",
                "Request Administrator Privileges",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                // 请求提升权限（这会触发UAC并重启应用）
                adminManager.RequestElevation();
            }
        }
        
        private void DetectResolution_Click(object sender, RoutedEventArgs e)
        {
            var adminManager = AdminPrivilegeManager.Instance;
            
            if (!adminManager.IsElevated)
            {
                MessageBox.Show(
                    "Resolution detection requires administrator privileges.\n\n" +
                    "Please click 'Request Administrator Privileges' first.",
                    "Administrator Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                // 执行分辨率检测
                adminManager.DetectAndAdjustResolution();
                
                // 更新设置中的最后检测时间
                _settingsService.Settings.LastResolutionDetection = DateTime.Now;
                _settingsService.Save();
                
                // 更新UI显示
                LastDetectionText.Text = $"Last resolution detection: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Resolution detection failed:\n{ex.Message}",
                    "Detection Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        
        #endregion
    }
}


