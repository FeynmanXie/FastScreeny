using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using FastScreeny.Models;

namespace FastScreeny.Services
{
    public class AutoUpdateService
    {
        private readonly HttpClient _httpClient;
        private readonly string _gitHubOwner;
        private readonly string _gitHubRepo;
        private readonly string _currentVersion;

        public event EventHandler<UpdateInfo>? UpdateAvailable;
        public event EventHandler<string>? UpdateDownloadProgress;
        public event EventHandler<bool>? UpdateCheckCompleted;

        public AutoUpdateService(string gitHubOwner = "FeynmanXie", string gitHubRepo = "FastScreeny")
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "FastScreeny-AutoUpdater");
            _gitHubOwner = gitHubOwner;
            _gitHubRepo = gitHubRepo;
            _currentVersion = GetCurrentVersion();
        }

        private string GetCurrentVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "1.0.0.0";
            }
            catch
            {
                return "1.0.0.0";
            }
        }

        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                var apiUrl = $"https://api.github.com/repos/{_gitHubOwner}/{_gitHubRepo}/releases/latest";
                var response = await _httpClient.GetStringAsync(apiUrl);

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                var release = JsonSerializer.Deserialize<GitHubRelease>(response, options);
                if (release == null)
                {
                    UpdateCheckCompleted?.Invoke(this, false);
                    return null;
                }

                var latestVersion = release.Tag_Name.TrimStart('v');
                var isUpdateAvailable = IsNewerVersion(latestVersion, _currentVersion);

                var updateInfo = new UpdateInfo
                {
                    CurrentVersion = _currentVersion,
                    LatestVersion = latestVersion,
                    IsUpdateAvailable = isUpdateAvailable,
                    ReleaseNotes = release.Body,
                    PublishedAt = release.Published_At
                };

                // 查找适合的更新文件（优先ZIP，其次EXE）
                foreach (var asset in release.Assets)
                {
                    // 优先选择ZIP文件
                    if (asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        updateInfo.DownloadUrl = asset.Browser_Download_Url;
                        updateInfo.FileSize = asset.Size;
                        updateInfo.FileName = asset.Name;
                        break;
                    }
                    // 如果没有ZIP文件，则选择包含Setup的EXE文件
                    else if (asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                        asset.Name.Contains("Setup", StringComparison.OrdinalIgnoreCase))
                    {
                        updateInfo.DownloadUrl = asset.Browser_Download_Url;
                        updateInfo.FileSize = asset.Size;
                        updateInfo.FileName = asset.Name;
                        // 不要break，继续查找ZIP文件
                    }
                }

                if (isUpdateAvailable)
                {
                    UpdateAvailable?.Invoke(this, updateInfo);
                }

                UpdateCheckCompleted?.Invoke(this, true);
                return updateInfo;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查更新时发生错误: {ex.Message}");
                UpdateCheckCompleted?.Invoke(this, false);
                return null;
            }
        }

        public async Task<string?> DownloadUpdateAsync(UpdateInfo updateInfo, IProgress<double>? progress = null)
        {
            try
            {
                if (string.IsNullOrEmpty(updateInfo.DownloadUrl))
                {
                    return null;
                }

                var tempPath = Path.GetTempPath();
                var fileName = updateInfo.FileName;
                var downloadPath = Path.Combine(tempPath, fileName);

                // 如果文件已存在，删除它
                if (File.Exists(downloadPath))
                {
                    File.Delete(downloadPath);
                }

                using var response = await _httpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var canReportProgress = totalBytes != -1 && progress != null;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                var totalBytesRead = 0L;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;

                    if (canReportProgress)
                    {
                        var progressPercentage = (double)totalBytesRead / totalBytes * 100;
                        progress!.Report(progressPercentage);
                        UpdateDownloadProgress?.Invoke(this, $"已下载: {progressPercentage:F1}%");
                    }
                }

                return downloadPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"下载更新时发生错误: {ex.Message}");
                return null;
            }
        }

        public void InstallUpdate(string downloadPath)
        {
            try
            {
                if (!File.Exists(downloadPath))
                {
                    System.Windows.MessageBox.Show("Update file not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (downloadPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    InstallZipUpdate(downloadPath);
                }
                else if (downloadPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    InstallExeUpdate(downloadPath);
                }
                else
                {
                    System.Windows.MessageBox.Show("Unsupported update file format!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error installing update: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InstallZipUpdate(string zipPath)
        {
            try
            {
                var extractPath = Path.Combine(Path.GetTempPath(), "FastScreeny_Update");

                // 如果解压目录已存在，先删除
                if (Directory.Exists(extractPath))
                {
                    Directory.Delete(extractPath, true);
                }

                // 解压ZIP文件
                ZipFile.ExtractToDirectory(zipPath, extractPath);

                // 获取当前应用程序目录
                var appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(appDirectory))
                {
                    System.Windows.MessageBox.Show("Cannot determine application directory!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 询问用户是否要自动更新
                var result = System.Windows.MessageBox.Show(
                    $"Update files extracted to:\n{extractPath}\n\n" +
                    "Choose update method:\n" +
                    "• Yes: Auto-replace files and restart (Recommended)\n" +
                    "• No: Manual update (Open folders)\n" +
                    "• Cancel: Update later",
                    "Choose Update Method",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // 自动更新：创建批处理文件来替换文件
                    CreateUpdateBatch(extractPath, appDirectory);
                }
                else if (result == MessageBoxResult.No)
                {
                    // 手动更新：打开解压目录和应用程序目录
                    Process.Start("explorer.exe", extractPath);
                    Process.Start("explorer.exe", appDirectory);

                    System.Windows.MessageBox.Show(
                        "Please manually copy the extracted files to the application directory, then restart the application.",
                        "Manual Update",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error extracting update files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateUpdateBatch(string sourcePath, string targetPath)
        {
            try
            {
                var batchPath = Path.Combine(Path.GetTempPath(), "FastScreeny_Update.bat");
                var currentExe = Assembly.GetExecutingAssembly().Location;

                var batchContent = $@"@echo off
echo Updating FastScreeny...
timeout /t 2 /nobreak > nul

REM Copy new files
xcopy ""{sourcePath}\*"" ""{targetPath}"" /E /Y /Q

REM Clean temporary files
rmdir /s /q ""{sourcePath}""
del ""{batchPath}""

REM Restart application
start """" ""{currentExe}""

echo Update completed!
exit
";

                File.WriteAllText(batchPath, batchContent, System.Text.Encoding.Default);

                // 启动批处理文件
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = batchPath,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(processStartInfo);

                // 关闭当前应用程序
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error creating update script: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InstallExeUpdate(string exePath)
        {
            // 原有的EXE安装逻辑
            var processStartInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas" // 请求管理员权限
            };

            Process.Start(processStartInfo);

            // 关闭当前应用程序
            System.Windows.Application.Current.Shutdown();
        }

        private bool IsNewerVersion(string latestVersion, string currentVersion)
        {
            try
            {
                var latest = new Version(latestVersion);
                var current = new Version(currentVersion);
                return latest > current;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
