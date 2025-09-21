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

            var deviceRect = DipRectToDeviceScreenRect(overlay, region.Value);
            using var bmp = new Bitmap(deviceRect.Width, deviceRect.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(deviceRect.X, deviceRect.Y, 0, 0, new System.Drawing.Size(deviceRect.Width, deviceRect.Height));
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

            var deviceRect = DipRectToDeviceScreenRect(overlay, region.Value);
            using var bmp = new Bitmap(deviceRect.Width, deviceRect.Height);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(deviceRect.X, deviceRect.Y, 0, 0, new System.Drawing.Size(deviceRect.Width, deviceRect.Height));

            using var finalBmp = ApplyOptionalBorder(bmp, settings.Settings);
            SaveAndCopy(settings, finalBmp);
        }

        private static OverlaySelectionWindow CreateOverlayAcrossScreens()
        {
            var allBounds = Screen.AllScreens.Select(s => s.Bounds).Aggregate(Rectangle.Union);
            var w = new OverlaySelectionWindow();
            
            // 调试信息：打印所有屏幕的边界
            System.Diagnostics.Debug.WriteLine($"All screens combined bounds: {allBounds}");
            foreach (var screen in Screen.AllScreens)
            {
                System.Diagnostics.Debug.WriteLine($"Screen: {screen.Bounds}, Primary: {screen.Primary}");
            }
            
            // 设置窗口状态以确保正确的覆盖层行为
            w.WindowState = WindowState.Maximized;  // 使用最大化确保覆盖整个屏幕
            w.WindowStyle = WindowStyle.None;
            w.ResizeMode = ResizeMode.NoResize;
            w.Topmost = true;
            w.AllowsTransparency = true;
            w.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(1, 0, 0, 0));
            
            // 手动设置窗口位置和尺寸
            w.Left = allBounds.Left;
            w.Top = allBounds.Top;
            w.Width = allBounds.Width;
            w.Height = allBounds.Height;
            
            System.Diagnostics.Debug.WriteLine($"Manual overlay position: Left={w.Left}, Top={w.Top}, Width={w.Width}, Height={w.Height}");
            
            return w;
        }

        private static double GetDpiScale()
        {
            try
            {
                // 获取系统DPI缩放比例
                using (var graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                {
                    var dpi = graphics.DpiX;
                    return dpi / 96.0; // 96 DPI是标准DPI
                }
            }
            catch
            {
                return 1.0; // 默认不缩放
            }
        }

        private static System.Drawing.Rectangle DipRectToDeviceScreenRect(System.Windows.Window w, System.Windows.Rect dipRect)
        {
            // 简化的坐标转换：直接使用屏幕坐标
            int x = (int)Math.Round(dipRect.X + w.Left);
            int y = (int)Math.Round(dipRect.Y + w.Top);
            int width = (int)Math.Round(dipRect.Width);
            int height = (int)Math.Round(dipRect.Height);
            
            System.Diagnostics.Debug.WriteLine($"Converting DIP rect: dipRect={dipRect}, window.Left={w.Left}, window.Top={w.Top}");
            System.Diagnostics.Debug.WriteLine($"Resulting screen rect: x={x}, y={y}, width={width}, height={height}");
            
            // 确保尺寸为正数
            if (width < 0) 
            {
                x += width;
                width = -width;
            }
            if (height < 0)
            {
                y += height;
                height = -height;
            }
            
            return new System.Drawing.Rectangle(x, y, width, height);
        }

        private static System.Drawing.Rectangle GetCorrectedScreenRect(System.Windows.Rect dipRect, System.Windows.Window w)
        {
            // 获取窗口的屏幕位置和DPI信息
            var windowLeft = (int)Math.Round(w.Left);
            var windowTop = (int)Math.Round(w.Top);
            
            // 使用Windows Forms获取准确的屏幕信息
            var allScreens = System.Windows.Forms.Screen.AllScreens;
            System.Drawing.Rectangle targetScreen = System.Drawing.Rectangle.Empty;
            
            // 找到包含窗口的屏幕
            foreach (var screen in allScreens)
            {
                if (screen.Bounds.Contains(windowLeft, windowTop))
                {
                    targetScreen = screen.Bounds;
                    break;
                }
            }
            
            // 如果找不到包含窗口的屏幕，使用主屏幕
            if (targetScreen.IsEmpty)
            {
                targetScreen = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
            }
            
            // 计算相对于屏幕的坐标
            int x = windowLeft + (int)Math.Round(dipRect.X);
            int y = windowTop + (int)Math.Round(dipRect.Y);
            int width = (int)Math.Round(dipRect.Width);
            int height = (int)Math.Round(dipRect.Height);
            
            // 确保坐标在屏幕范围内
            x = Math.Max(targetScreen.X, Math.Min(x, targetScreen.Right - width));
            y = Math.Max(targetScreen.Y, Math.Min(y, targetScreen.Bottom - height));
            
            return new System.Drawing.Rectangle(x, y, width, height);
        }

        private static System.Drawing.Rectangle ValidateAndCorrectScreenCoordinates(int x, int y, int width, int height)
        {
            // 验证坐标是否在任何屏幕范围内
            var allScreens = System.Windows.Forms.Screen.AllScreens;
            var rect = new System.Drawing.Rectangle(x, y, width, height);
            
            // 检查矩形是否与任何屏幕相交
            foreach (var screen in allScreens)
            {
                if (screen.Bounds.IntersectsWith(rect))
                {
                    // 确保矩形完全在屏幕范围内
                    var correctedRect = System.Drawing.Rectangle.Intersect(screen.Bounds, rect);
                    if (!correctedRect.IsEmpty)
                    {
                        return correctedRect;
                    }
                }
            }
            
            // 如果没有找到合适的屏幕，尝试找到最近的屏幕
            var primaryScreen = System.Windows.Forms.Screen.PrimaryScreen!;
            if (x < primaryScreen.Bounds.X || x >= primaryScreen.Bounds.Right || 
                y < primaryScreen.Bounds.Y || y >= primaryScreen.Bounds.Bottom)
            {
                // 将坐标调整到主屏幕范围内
                x = Math.Max(primaryScreen.Bounds.X, Math.Min(x, primaryScreen.Bounds.Right - width));
                y = Math.Max(primaryScreen.Bounds.Y, Math.Min(y, primaryScreen.Bounds.Bottom - height));
            }
            
            return new System.Drawing.Rectangle(x, y, width, height);
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


