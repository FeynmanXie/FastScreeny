using System;

namespace FastScreeny.Models
{
    public class UpdateInfo
    {
        public string CurrentVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public bool IsUpdateAvailable { get; set; }
        public long FileSize { get; set; }
        public string FileName { get; set; } = string.Empty;
    }

    public class ReleaseAsset
    {
        public string Name { get; set; } = string.Empty;
        public string Browser_Download_Url { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Content_Type { get; set; } = string.Empty;
    }

    public class GitHubRelease
    {
        public string Tag_Name { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime Published_At { get; set; }
        public bool Prerelease { get; set; }
        public ReleaseAsset[] Assets { get; set; } = Array.Empty<ReleaseAsset>();
    }
}
