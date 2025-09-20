using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FastScreeny.Services
{
    public class AdminPrivilegeManager
    {
        private static AdminPrivilegeManager? _instance;
        private bool _isElevated = false;
        private bool _elevationRequested = false;

        public static AdminPrivilegeManager Instance => _instance ??= new AdminPrivilegeManager();

        public bool IsElevated => _isElevated;
        public bool ElevationRequested => _elevationRequested;

        public event Action<bool>? ElevationStatusChanged;

        private AdminPrivilegeManager()
        {
            CheckCurrentElevationStatus();
        }

        /// <summary>
        /// 检查当前进程是否具有管理员权限
        /// </summary>
        public bool CheckCurrentElevationStatus()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                _isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
                
                System.Diagnostics.Debug.WriteLine($"Current elevation status: {_isElevated}");
                return _isElevated;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking elevation status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 请求管理员权限提升 - 弹出UAC对话框
        /// </summary>
        public bool RequestElevation()
        {
            if (_isElevated)
            {
                System.Diagnostics.Debug.WriteLine("Already elevated");
                return true;
            }

            if (_elevationRequested)
            {
                System.Diagnostics.Debug.WriteLine("Elevation already requested");
                return true;
            }

            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var startInfo = new ProcessStartInfo
                {
                    FileName = currentProcess.MainModule?.FileName ?? System.Reflection.Assembly.GetExecutingAssembly().Location,
                    UseShellExecute = true,
                    Verb = "runas", // 这会触发UAC对话框
                    Arguments = "--elevated --background" // 标记为提升权限启动
                };

                var result = System.Windows.MessageBox.Show(
                    "FastScreeny needs administrator privileges for advanced features.\n\n" +
                    "Click 'Yes' to continue with elevated privileges, or 'No' to cancel.",
                    "Administrator Privileges Required",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return false;
                }

                // 启动新的管理员实例
                var elevatedProcess = Process.Start(startInfo);
                if (elevatedProcess != null)
                {
                    _elevationRequested = true;
                    ElevationStatusChanged?.Invoke(true);
                    
                    // 关闭当前非管理员实例
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        System.Windows.Application.Current.Shutdown();
                    });
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to request elevation: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Failed to request administrator privileges:\n{ex.Message}",
                    "Elevation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            return false;
        }

        /// <summary>
        /// 自动检测并调整分辨率设置
        /// </summary>
        public void DetectAndAdjustResolution()
        {
            if (!_isElevated)
            {
                System.Windows.MessageBox.Show(
                    "Resolution detection requires administrator privileges.\nPlease click 'Request Admin Privileges' first.",
                    "Admin Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                var detectionResults = PerformResolutionDetection();
                ShowResolutionResults(detectionResults);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Resolution detection failed: {ex.Message}",
                    "Detection Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private string PerformResolutionDetection()
        {
            var results = new System.Text.StringBuilder();
            results.AppendLine("=== FastScreeny Resolution Detection Report ===");
            results.AppendLine($"Detection Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            results.AppendLine($"Privilege Level: {(_isElevated ? "Administrator" : "User")}");
            results.AppendLine();

            // 获取所有显示器信息
            results.AppendLine("Display Configuration:");
            var screens = Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                var screen = screens[i];
                results.AppendLine($"  Monitor {i + 1}: {screen.Bounds.Width}x{screen.Bounds.Height} at ({screen.Bounds.X}, {screen.Bounds.Y})");
                results.AppendLine($"    Primary: {screen.Primary}");
                results.AppendLine($"    Working Area: {screen.WorkingArea}");
                results.AppendLine();
            }

            // 获取DPI信息
            try
            {
                using (var graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                {
                    results.AppendLine($"System DPI: {graphics.DpiX}x{graphics.DpiY}");
                    results.AppendLine($"DPI Scale Factor: {graphics.DpiX / 96.0:F2}x");
                }
            }
            catch (Exception ex)
            {
                results.AppendLine($"DPI Detection Error: {ex.Message}");
            }

            results.AppendLine();
            results.AppendLine("Recommendations:");

            // 基于检测结果提供建议
            if (screens.Length > 1)
            {
                results.AppendLine("- Multi-monitor setup detected. Ensure screenshot overlays span all screens correctly.");
            }

            var totalWidth = 0;
            var totalHeight = 0;
            foreach (var screen in screens)
            {
                totalWidth = Math.Max(totalWidth, screen.Bounds.Right);
                totalHeight = Math.Max(totalHeight, screen.Bounds.Bottom);
            }

            if (totalWidth > 3840 || totalHeight > 2160)
            {
                results.AppendLine("- High resolution display detected. Performance optimizations recommended.");
            }

            results.AppendLine("- Resolution detection completed successfully.");

            return results.ToString();
        }

        private void ShowResolutionResults(string results)
        {
            var resultWindow = new Window
            {
                Title = "Resolution Detection Results",
                Width = 600,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.CanResize
            };

            var textBox = new System.Windows.Controls.TextBox
            {
                Text = results,
                IsReadOnly = true,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12,
                Padding = new Thickness(10),
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                Background = System.Windows.Media.Brushes.Black,
                Foreground = System.Windows.Media.Brushes.LimeGreen
            };

            resultWindow.Content = textBox;
            resultWindow.ShowDialog();
        }

        /// <summary>
        /// 验证暗号并打开管理员PowerShell
        /// </summary>
        public void RequestAdminPowerShellWithPassword()
        {
            if (!_isElevated)
            {
                System.Windows.MessageBox.Show(
                    "Admin PowerShell requires administrator privileges.\nPlease click 'Request Admin Privileges' first.",
                    "Admin Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // 创建密码输入对话框
            var passwordDialog = CreatePasswordDialog();
            if (passwordDialog.ShowDialog() == true)
            {
                var enteredPassword = passwordDialog.Tag as string;
                if (ValidatePassword(enteredPassword))
                {
                    OpenAdminPowerShell();
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "Incorrect password. Access denied.",
                        "Authentication Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private Window CreatePasswordDialog()
        {
            var dialog = new Window
            {
                Title = "Admin PowerShell Authentication",
                Width = 350,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow
            };

            var grid = new System.Windows.Controls.Grid();
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

            var label = new System.Windows.Controls.Label
            {
                Content = "Enter password to open Admin PowerShell:",
                Margin = new Thickness(10),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            System.Windows.Controls.Grid.SetRow(label, 0);

            var passwordBox = new System.Windows.Controls.PasswordBox
            {
                Margin = new Thickness(10),
                FontSize = 14,
                Padding = new Thickness(5)
            };
            System.Windows.Controls.Grid.SetRow(passwordBox, 1);

            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };

            var okButton = new System.Windows.Controls.Button
            {
                Content = "OK",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                IsDefault = true
            };

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5),
                IsCancel = true
            };

            okButton.Click += (s, e) =>
            {
                dialog.Tag = passwordBox.Password;
                dialog.DialogResult = true;
            };

            cancelButton.Click += (s, e) =>
            {
                dialog.DialogResult = false;
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            System.Windows.Controls.Grid.SetRow(buttonPanel, 2);

            grid.Children.Add(label);
            grid.Children.Add(passwordBox);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;

            // 焦点设置到密码框
            dialog.Loaded += (s, e) => passwordBox.Focus();

            return dialog;
        }

        private bool ValidatePassword(string? password)
        {
            return password == "google123"; // 您指定的暗号
        }

        private void OpenAdminPowerShell()
        {
            try
            {
                var appDirectory = AppDomain.CurrentDomain.BaseDirectory;

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    WorkingDirectory = appDirectory,
                    UseShellExecute = true,
                    Arguments = $"-NoExit -Command \"" +
                               $"cd '{appDirectory}'; " +
                               $"Write-Host 'FastScreeny Admin PowerShell - Authenticated Access' -ForegroundColor Green; " +
                               $"Write-Host 'Current Directory: $PWD' -ForegroundColor Yellow; " +
                               $"Write-Host 'Administrator Mode: Enabled' -ForegroundColor Red; " +
                               $"Write-Host 'Available Admin Commands:' -ForegroundColor Cyan; " +
                               $"Write-Host '  Get-Process FastScreeny' -ForegroundColor White; " +
                               $"Write-Host '  Get-EventLog Application -Source FastScreeny' -ForegroundColor White; " +
                               $"Write-Host '  Test-NetConnection github.com' -ForegroundColor White; " +
                               $"Write-Host 'Resolution Detection Available' -ForegroundColor Magenta;" +
                               $"\""
                };

                Process.Start(startInfo);

                System.Windows.MessageBox.Show(
                    "Admin PowerShell opened successfully!\nAuthenticated access granted.",
                    "PowerShell Opened",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to open Admin PowerShell: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 检查命令行参数，判断是否是以管理员权限启动的
        /// </summary>
        public bool HandleElevatedStartup(string[] args)
        {
            if (args != null && Array.Exists(args, arg => arg == "--elevated"))
            {
                _elevationRequested = true;
                _isElevated = CheckCurrentElevationStatus();
                
                if (_isElevated)
                {
                    System.Diagnostics.Debug.WriteLine("Application started with elevated privileges");
                    ElevationStatusChanged?.Invoke(true);
                    return true;
                }
            }

            return false;
        }
    }
}