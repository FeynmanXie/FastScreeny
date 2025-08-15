using System;
using System.Threading.Tasks;
using System.Windows;
using FastScreeny.Models;
using FastScreeny.Services;

namespace FastScreeny.Views
{
    public partial class UpdateWindow : Window
    {
        private readonly AutoUpdateService _updateService;
        private readonly UpdateInfo _updateInfo;
        private bool _isDownloading = false;

        public UpdateWindow(AutoUpdateService updateService, UpdateInfo updateInfo)
        {
            InitializeComponent();
            _updateService = updateService;
            _updateInfo = updateInfo;
            
            InitializeWindow();
            SubscribeToEvents();
        }

        private void InitializeWindow()
        {
            CurrentVersionText.Text = _updateInfo.CurrentVersion;
            LatestVersionText.Text = _updateInfo.LatestVersion;
            PublishDateText.Text = _updateInfo.PublishedAt.ToString("yyyy-MM-dd");
            FileSizeText.Text = FormatFileSize(_updateInfo.FileSize);
            ReleaseNotesText.Text = string.IsNullOrEmpty(_updateInfo.ReleaseNotes) 
                ? "No release notes available" 
                : _updateInfo.ReleaseNotes;
        }

        private void SubscribeToEvents()
        {
            _updateService.UpdateDownloadProgress += OnDownloadProgress;
        }

        private void OnDownloadProgress(object? sender, string progressMessage)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressText.Text = progressMessage;
            });
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDownloading) return;

            _isDownloading = true;
            DownloadButton.IsEnabled = false;
            LaterButton.IsEnabled = false;
            SkipButton.IsEnabled = false;
            
            ProgressPanel.Visibility = Visibility.Visible;
            StatusText.Text = "Downloading update file...";

            try
            {
                var progress = new Progress<double>(value =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        DownloadProgressBar.Value = value;
                        ProgressText.Text = $"Download progress: {value:F1}%";
                    });
                });

                var downloadPath = await _updateService.DownloadUpdateAsync(_updateInfo, progress);
                
                if (!string.IsNullOrEmpty(downloadPath))
                {
                    var isZipFile = downloadPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
                    var message = isZipFile 
                        ? "Download complete! Update the application now?\n\nNote: The application will close during the update process."
                        : "Download complete! Install the update now?\n\nNote: The application will close during the installation.";
                    
                    var result = System.Windows.MessageBox.Show(
                        message, 
                        "Download Complete", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _updateService.InstallUpdate(downloadPath);
                    }
                    else
                    {
                        var buttonText = isZipFile ? "Update Now" : "Install Now";
                        StatusText.Text = $"Download complete, update file saved to: {downloadPath}";
                        DownloadButton.Content = buttonText;
                        DownloadButton.IsEnabled = true;
                        DownloadButton.Click -= DownloadButton_Click;
                        DownloadButton.Click += (s, e) => _updateService.InstallUpdate(downloadPath);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Download failed. Please check your network connection or try again later.", "Download Failed", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    ResetDownloadState();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error during download: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                ResetDownloadState();
            }
        }

        private void ResetDownloadState()
        {
            _isDownloading = false;
            DownloadButton.IsEnabled = true;
            LaterButton.IsEnabled = true;
            SkipButton.IsEnabled = true;
            ProgressPanel.Visibility = Visibility.Collapsed;
            StatusText.Text = "Click Update Now to download the new version";
        }

        private void LaterButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to skip this version? The application will no longer remind you to update to this version.", 
                "Skip Version", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // 这里可以保存跳过的版本信息到设置中
                // 暂时直接关闭窗口
                DialogResult = false;
                Close();
            }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "Unknown size";

            string[] suffixes = { "B", "KB", "MB", "GB" };
            double size = bytes;
            int suffixIndex = 0;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:F1} {suffixes[suffixIndex]}";
        }

        protected override void OnClosed(EventArgs e)
        {
            _updateService.UpdateDownloadProgress -= OnDownloadProgress;
            base.OnClosed(e);
        }
    }
}
