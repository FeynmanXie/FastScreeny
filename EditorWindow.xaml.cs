using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using FastScreeny.Services;
using FastScreeny.Models;

namespace FastScreeny
{
    public partial class EditorWindow : Window
    {
        private readonly SettingsService _settingsService;
        private Bitmap _workingBitmap;
        private enum Tool { None, Circle, Rect, Arrow, Crop }
        private Tool _currentTool = Tool.None;

        private System.Windows.Point _dragStart;
        private bool _dragging;
        private System.Windows.Shapes.Shape? _previewShape;

        private class BrushPreset
        {
            public string Name { get; set; } = string.Empty;
            public System.Windows.Media.Brush Stroke { get; set; } = System.Windows.Media.Brushes.Red;
            public double Thickness { get; set; } = 4;
        }

        private readonly List<BrushPreset> _brushPresets = new()
        {
            new BrushPreset { Name = "红色强调", Stroke = System.Windows.Media.Brushes.Red, Thickness = 4 },
            new BrushPreset { Name = "荧光青", Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00,0xE5,0xFF)), Thickness = 4 },
            new BrushPreset { Name = "亮紫", Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x7B,0x61,0xFF)), Thickness = 4 },
            new BrushPreset { Name = "黑色粗线", Stroke = System.Windows.Media.Brushes.Black, Thickness = 6 }
        };

        public EditorWindow(Bitmap bitmap, SettingsService settingsService)
        {
            InitializeComponent();
            _workingBitmap = (Bitmap)bitmap.Clone();
            _settingsService = settingsService;

            BaseImage.Source = BitmapToImageSource(_workingBitmap);

            // 初始化画笔预设
            BrushPresetBox.ItemsSource = _brushPresets.Select(p => p.Name).ToList();
            BrushPresetBox.SelectedIndex = 0;

            // 初始化边框控件
            EnableBorderCheck.IsChecked = _settingsService.Settings.EnableBorder;
            BorderThicknessBox.Text = _settingsService.Settings.BorderThickness.ToString();
            BorderPresetBox.ItemsSource = new[] { "自定义", "梦幻紫粉", "炫彩彩虹", "薄雾白边", "暖阳橙金", "极夜黑蓝", "荧光青紫" };
            BorderPresetBox.SelectedItem = _settingsService.Settings.BorderPreset;
            BorderStartColorBox.Text = _settingsService.Settings.BorderGradientStart;
            BorderEndColorBox.Text = _settingsService.Settings.BorderGradientEnd;
            BorderPresetBox.SelectionChanged += BorderPresetBox_SelectionChanged;
        }

        private static BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using var memory = new MemoryStream();
            bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
            memory.Position = 0;
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = memory;
            bitmapImage.EndInit();
            return bitmapImage;
        }

        private BrushPreset CurrentBrush()
        {
            var name = BrushPresetBox.SelectedItem as string;
            var preset = _brushPresets.FirstOrDefault(p => p.Name == name) ?? _brushPresets[0];
            return preset;
        }

        private void ToolCircleBtn_Click(object sender, RoutedEventArgs e) => _currentTool = Tool.Circle;
        private void ToolRectBtn_Click(object sender, RoutedEventArgs e) => _currentTool = Tool.Rect;
        private void ToolArrowBtn_Click(object sender, RoutedEventArgs e) => _currentTool = Tool.Arrow;
        private void CropModeBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentTool = Tool.Crop;
            CropCanvas.Visibility = Visibility.Visible;
        }
        private void ApplyCropBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_previewShape == null) return;
            var rect = new System.Drawing.Rectangle((int)System.Windows.Controls.Canvas.GetLeft(_previewShape), (int)System.Windows.Controls.Canvas.GetTop(_previewShape), (int)_previewShape.Width, (int)_previewShape.Height);
            rect.Intersect(new System.Drawing.Rectangle(0, 0, _workingBitmap.Width, _workingBitmap.Height));
            if (rect.Width <= 0 || rect.Height <= 0) return;
            var cropped = new Bitmap(rect.Width, rect.Height);
            using (var g = Graphics.FromImage(cropped))
            {
                g.DrawImage(_workingBitmap, new System.Drawing.Rectangle(0, 0, rect.Width, rect.Height), rect, System.Drawing.GraphicsUnit.Pixel);
            }
            _workingBitmap.Dispose();
            _workingBitmap = cropped;
            BaseImage.Source = BitmapToImageSource(_workingBitmap);
            OverlayCanvas.Children.Clear();
            CropCanvas.Visibility = Visibility.Collapsed;
            _currentTool = Tool.None;
        }
        private void CancelCropBtn_Click(object sender, RoutedEventArgs e)
        {
            CropCanvas.Visibility = Visibility.Collapsed;
            _currentTool = Tool.None;
            if (_previewShape != null) { OverlayCanvas.Children.Remove(_previewShape); _previewShape = null; }
        }

        private void OverlayCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(OverlayCanvas);
            _dragging = true;
            StartPreviewShape();
        }

        private void OverlayCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_dragging || _previewShape == null) return;
            var pos = e.GetPosition(OverlayCanvas);
            UpdatePreviewShape(_dragStart, pos);
        }

        private void OverlayCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_dragging) return;
            _dragging = false;
            CommitPreviewShape();
        }

        private void StartPreviewShape()
        {
            var brush = CurrentBrush();
            switch (_currentTool)
            {
                case Tool.Circle:
                    _previewShape = new System.Windows.Shapes.Ellipse
                    {
                        Stroke = brush.Stroke,
                        StrokeThickness = brush.Thickness,
                        Fill = System.Windows.Media.Brushes.Transparent
                    };
                    break;
                case Tool.Rect:
                    _previewShape = new System.Windows.Shapes.Rectangle
                    {
                        Stroke = brush.Stroke,
                        StrokeThickness = brush.Thickness,
                        Fill = System.Windows.Media.Brushes.Transparent
                    };
                    break;
                case Tool.Arrow:
                    _previewShape = new System.Windows.Shapes.Line
                    {
                        Stroke = brush.Stroke,
                        StrokeThickness = brush.Thickness,
                        StrokeStartLineCap = System.Windows.Media.PenLineCap.Round,
                        StrokeEndLineCap = System.Windows.Media.PenLineCap.Triangle
                    };
                    break;
                case Tool.Crop:
                    _previewShape = new System.Windows.Shapes.Rectangle
                    {
                        Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 0x5B, 0x8D, 0xEF)),
                        StrokeThickness = 2,
                        Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 0x5B, 0x8D, 0xEF))
                    };
                    break;
                default:
                    return;
            }
            OverlayCanvas.Children.Add(_previewShape);
        }

        private void UpdatePreviewShape(System.Windows.Point a, System.Windows.Point b)
        {
            if (_previewShape is System.Windows.Shapes.Line line)
            {
                line.X1 = a.X; line.Y1 = a.Y; line.X2 = b.X; line.Y2 = b.Y;
                return;
            }
            var left = Math.Min(a.X, b.X);
            var top = Math.Min(a.Y, b.Y);
            var width = Math.Abs(b.X - a.X);
            var height = Math.Abs(b.Y - a.Y);
            if (_previewShape is System.Windows.Shapes.Ellipse)
            {
                System.Windows.Controls.Canvas.SetLeft(_previewShape, left);
                System.Windows.Controls.Canvas.SetTop(_previewShape, top);
                _previewShape.Width = width; _previewShape.Height = height;
            }
            else if (_previewShape is System.Windows.Shapes.Rectangle)
            {
                System.Windows.Controls.Canvas.SetLeft(_previewShape, left);
                System.Windows.Controls.Canvas.SetTop(_previewShape, top);
                _previewShape.Width = width; _previewShape.Height = height;
            }
        }

        private void CommitPreviewShape()
        {
            if (_previewShape == null) return;
            var rt = new System.Windows.Rect(System.Windows.Controls.Canvas.GetLeft(_previewShape), System.Windows.Controls.Canvas.GetTop(_previewShape), _previewShape.Width, _previewShape.Height);

            using (var g = Graphics.FromImage(_workingBitmap))
            {
                var preset = CurrentBrush();
                using var pen = new Pen(ToGdiColor(((System.Windows.Media.SolidColorBrush)preset.Stroke).Color), (float)preset.Thickness)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                };

                switch (_currentTool)
                {
                    case Tool.Circle:
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.DrawEllipse(pen, (float)rt.X, (float)rt.Y, (float)rt.Width, (float)rt.Height);
                        break;
                    case Tool.Rect:
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.DrawRectangle(pen, (float)rt.X, (float)rt.Y, (float)rt.Width, (float)rt.Height);
                        break;
                    case Tool.Arrow:
                        if (_previewShape is System.Windows.Shapes.Line line)
                        {
                            var adjustable = CreateArrowAdjustableGeometry(new System.Drawing.PointF((float)line.X1, (float)line.Y1), new System.Drawing.PointF((float)line.X2, (float)line.Y2), (float)preset.Thickness);
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.FillPath(new SolidBrush(ToGdiColor(((System.Windows.Media.SolidColorBrush)preset.Stroke).Color)), adjustable);
                        }
                        break;
                }
            }

            BaseImage.Source = BitmapToImageSource(_workingBitmap);
            OverlayCanvas.Children.Remove(_previewShape);
            _previewShape = null;
        }

        private static System.Drawing.Color ToGdiColor(System.Windows.Media.Color color) => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);

        private static GraphicsPath CreateArrowAdjustableGeometry(System.Drawing.PointF start, System.Drawing.PointF end, float thickness)
        {
            var path = new GraphicsPath();
            // 箭头始终为固定箭头造型（线身 + 三角头），长度随拖拽调整
            var dx = end.X - start.X; var dy = end.Y - start.Y;
            var len = Math.Max(1f, (float)Math.Sqrt(dx * dx + dy * dy));
            var ux = dx / len; var uy = dy / len; // 单位向量
            var shaftWidth = thickness; // 线身宽度
            var headLength = Math.Max(10f, thickness * 4f);
            var headWidth = Math.Max(shaftWidth * 2f, thickness * 3f);

            var shaftEnd = new System.Drawing.PointF(end.X - ux * headLength, end.Y - uy * headLength);
            // 线身矩形的法向量
            var nx = -uy; var ny = ux;
            var p1 = new System.Drawing.PointF(start.X + nx * shaftWidth / 2f, start.Y + ny * shaftWidth / 2f);
            var p2 = new System.Drawing.PointF(start.X - nx * shaftWidth / 2f, start.Y - ny * shaftWidth / 2f);
            var p3 = new System.Drawing.PointF(shaftEnd.X - nx * shaftWidth / 2f, shaftEnd.Y - ny * shaftWidth / 2f);
            var p4 = new System.Drawing.PointF(shaftEnd.X + nx * shaftWidth / 2f, shaftEnd.Y + ny * shaftWidth / 2f);

            // 三角箭头
            var h1 = new System.Drawing.PointF(end.X, end.Y);
            var h2 = new System.Drawing.PointF(shaftEnd.X - nx * headWidth / 2f, shaftEnd.Y - ny * headWidth / 2f);
            var h3 = new System.Drawing.PointF(shaftEnd.X + nx * headWidth / 2f, shaftEnd.Y + ny * headWidth / 2f);

            path.AddPolygon(new[] { p1, p2, p3, p4 });
            path.CloseFigure();
            path.AddPolygon(new[] { h1, h2, h3 });
            path.CloseFigure();
            return path;
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            OverlayCanvas.Children.Clear();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            // 同步编辑器中的边框设置到 Settings
            _settingsService.Settings.EnableBorder = EnableBorderCheck.IsChecked == true;
            if (int.TryParse(BorderThicknessBox.Text, out var px) && px >= 0 && px <= 512)
            {
                _settingsService.Settings.BorderThickness = px;
            }
            _settingsService.Settings.BorderGradientStart = string.IsNullOrWhiteSpace(BorderStartColorBox.Text) ? _settingsService.Settings.BorderGradientStart : BorderStartColorBox.Text.Trim();
            _settingsService.Settings.BorderGradientEnd = string.IsNullOrWhiteSpace(BorderEndColorBox.Text) ? _settingsService.Settings.BorderGradientEnd : BorderEndColorBox.Text.Trim();
            _settingsService.Settings.BorderPreset = (BorderPresetBox.SelectedItem as string) ?? _settingsService.Settings.BorderPreset;
            // 保存
            ScreenCaptureService.SaveBitmapWithSettings(_settingsService, _workingBitmap);
            Close();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

        private void BorderPresetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var name = BorderPresetBox.SelectedItem as string ?? "自定义";
            switch (name)
            {
                case "梦幻紫粉":
                    BorderThicknessBox.Text = "40";
                    BorderStartColorBox.Text = "#FF8B5CF6";
                    BorderEndColorBox.Text = "#FFEC4899";
                    break;
                case "炫彩彩虹":
                    BorderThicknessBox.Text = "45";
                    BorderStartColorBox.Text = "#FF8B5CF6";
                    BorderEndColorBox.Text = "#FFFBBF24";
                    break;
                case "薄雾白边":
                    BorderThicknessBox.Text = "36";
                    BorderStartColorBox.Text = "#FFFFFFFF";
                    BorderEndColorBox.Text = "#FFEFEFEF";
                    break;
                case "暖阳橙金":
                    BorderThicknessBox.Text = "32";
                    BorderStartColorBox.Text = "#FFFF6B35";
                    BorderEndColorBox.Text = "#FFFBBF24";
                    break;
                case "极夜黑蓝":
                    BorderThicknessBox.Text = "30";
                    BorderStartColorBox.Text = "#FF0F172A";
                    BorderEndColorBox.Text = "#FF1E293B";
                    break;
                case "荧光青紫":
                    BorderThicknessBox.Text = "35";
                    BorderStartColorBox.Text = "#FF06B6D4";
                    BorderEndColorBox.Text = "#FF8B5CF6";
                    break;
            }
        }
    }
}


