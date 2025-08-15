using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using FastScreeny.Models;
using System.Drawing.Drawing2D;
using FastScreeny;

namespace FastScreeny.Services
{
    public static class ScreenCaptureService
    {
        public static async Task CaptureRegionAndEditAsync(SettingsService settings)
        {
            var overlay = CreateOverlayAcrossScreens();
            overlay.Show();
            var region = await overlay.SelectRegionAsync();
            if (region == null)
            {
                return;
            }

            var r = region.Value;
            using var bmp = new Bitmap((int)r.Width, (int)r.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen((int)r.X, (int)r.Y, 0, 0, new System.Drawing.Size((int)r.Width, (int)r.Height));
            }

            // 打开编辑器
            var editor = new EditorWindow((Bitmap)bmp.Clone(), settings);
            editor.ShowDialog();
        }
        public static async Task CaptureRegionToDefaultAsync(SettingsService settings)
        {
            if (settings.Settings.DefaultOpenInEditor)
            {
                await CaptureRegionAndEditAsync(settings);
            }
            else
            {
                await CaptureRegionAndSaveAsync(settings);
            }
        }

        public static async Task CaptureRegionAndSaveAsync(SettingsService settings)
        {
            var overlay = CreateOverlayAcrossScreens();
            overlay.Show();
            var region = await overlay.SelectRegionAsync();
            if (region == null)
            {
                return;
            }

            var r = region.Value;
            using var bmp = new Bitmap((int)r.Width, (int)r.Height);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen((int)r.X, (int)r.Y, 0, 0, new System.Drawing.Size((int)r.Width, (int)r.Height));

            using var finalBmp = ApplyOptionalBorder(bmp, settings.Settings);
            SaveAndCopy(settings, finalBmp);
        }

        private static OverlaySelectionWindow CreateOverlayAcrossScreens()
        {
            var allBounds = Screen.AllScreens.Select(s => s.Bounds).Aggregate(Rectangle.Union);
            var w = new OverlaySelectionWindow
            {
                Left = allBounds.Left,
                Top = allBounds.Top,
                Width = allBounds.Width,
                Height = allBounds.Height
            };
            return w;
        }

        private static void SaveAndCopy(SettingsService settings, Bitmap bmp)
        {
            var dir = StoragePaths.EnsureDirectory(settings.Settings.SaveDirectory);
            var fileName = ResolveFileName(settings.Settings.FileNamePattern);
            var path = Path.Combine(dir, fileName);
            bmp.Save(path, ImageFormat.Png);

            if (settings.Settings.AutoCopyToClipboard)
            {
                try
                {
                    System.Windows.Forms.Clipboard.SetImage(bmp);
                }
                catch
                {
                    // ignore clipboard exceptions due to STA timing
                }
            }
        }

        // 供外部（编辑器）调用的保存封装
        public static void SaveBitmapWithSettings(SettingsService settings, Bitmap bmp)
        {
            using var finalBmp = ApplyOptionalBorder(bmp, settings.Settings);
            SaveAndCopy(settings, finalBmp);
        }

        public static Bitmap ApplyOptionalBorder(Bitmap source, AppSettings settings)
        {
            if (settings == null || !settings.EnableBorder || settings.BorderThickness <= 0)
            {
                return (Bitmap)source.Clone();
            }

            int t = settings.BorderThickness;
            int newW = source.Width + t * 2;
            int newH = source.Height + t * 2;
            var result = new Bitmap(newW, newH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(result))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                var startColor = ParseColor(settings.BorderGradientStart, System.Drawing.Color.FromArgb(0xFF, 0x8B, 0x5C, 0xF6));
                var endColor = ParseColor(settings.BorderGradientEnd, System.Drawing.Color.FromArgb(0xFF, 0xEC, 0x48, 0x99));

                // 整张"渐变纸"作为背景（线性渐变：从左上角到右下角）
                var gradientRect = new RectangleF(0, 0, newW, newH);
                using (var brush = new LinearGradientBrush(gradientRect, startColor, endColor, LinearGradientMode.ForwardDiagonal))
                {
                    // 添加多点渐变以获得更丰富的色彩过渡
                    var blend = new ColorBlend(3);
                    blend.Colors = new[] { startColor, BlendColors(startColor, endColor, 0.5f), endColor };
                    blend.Positions = new[] { 0.0f, 0.5f, 1.0f };
                    brush.InterpolationColors = blend;
                    
                    g.FillRectangle(brush, 0, 0, newW, newH);
                }

                // 将原图贴在中央，留出四周厚度 t 的可见“底纸”区域
                g.DrawImage(source, t, t, source.Width, source.Height);
            }

            return result;
        }

        private static System.Drawing.Color ParseColor(string? text, System.Drawing.Color fallback)
        {
            if (string.IsNullOrWhiteSpace(text)) return fallback;
            text = text.Trim();
            try
            {
                // Support #RRGGBB and #AARRGGBB
                if (text.StartsWith("#") && (text.Length == 7 || text.Length == 9))
                {
                    if (text.Length == 7)
                    {
                        return System.Drawing.ColorTranslator.FromHtml(text);
                    }
                    // #AARRGGBB
                    var a = Convert.ToByte(text.Substring(1, 2), 16);
                    var r = Convert.ToByte(text.Substring(3, 2), 16);
                    var g = Convert.ToByte(text.Substring(5, 2), 16);
                    var b = Convert.ToByte(text.Substring(7, 2), 16);
                    return System.Drawing.Color.FromArgb(a, r, g, b);
                }

                // Try named colors or other html forms
                return System.Drawing.ColorTranslator.FromHtml(text);
            }
            catch
            {
                return fallback;
            }
        }

        private static System.Drawing.Color BlendColors(System.Drawing.Color color1, System.Drawing.Color color2, float ratio)
        {
            var r = (byte)(color1.R + (color2.R - color1.R) * ratio);
            var g = (byte)(color1.G + (color2.G - color1.G) * ratio);
            var b = (byte)(color1.B + (color2.B - color1.B) * ratio);
            var a = (byte)(color1.A + (color2.A - color1.A) * ratio);
            return System.Drawing.Color.FromArgb(a, r, g, b);
        }

        private static string ResolveFileName(string pattern)
        {
            string ReplaceToken(string token, string format)
            {
                return DateTime.Now.ToString(format);
            }

            var name = pattern
                .Replace("{yyyyMMdd_HHmmss}", ReplaceToken("ts", "yyyyMMdd_HHmmss"));
            if (!name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                name += ".png";
            }
            return name;
        }
    }
}


