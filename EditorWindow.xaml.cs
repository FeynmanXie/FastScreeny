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
        private enum Tool { None, Circle, Rect, Arrow }
        private Tool _currentTool = Tool.None;
        private bool _drawingMode = false; // 绘画模式状态

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
            new BrushPreset { Name = "Red Accent", Stroke = System.Windows.Media.Brushes.Red, Thickness = 4 },
            new BrushPreset { Name = "Fluorescent Cyan", Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00,0xE5,0xFF)), Thickness = 4 },
            new BrushPreset { Name = "Bright Purple", Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x7B,0x61,0xFF)), Thickness = 4 },
            new BrushPreset { Name = "Black Thick Line", Stroke = System.Windows.Media.Brushes.Black, Thickness = 6 }
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
            BorderPresetBox.ItemsSource = new[] { "Custom", "Purple Pink Dream", "Rainbow Spectrum", "Misty White", "Warm Orange Gold", "Dark Night Blue", "Fluorescent Cyan Purple" };
            BorderPresetBox.SelectedItem = _settingsService.Settings.BorderPreset;
            BorderStartColorBox.Text = _settingsService.Settings.BorderGradientStart;
            BorderEndColorBox.Text = _settingsService.Settings.BorderGradientEnd;
            BorderPresetBox.SelectionChanged += BorderPresetBox_SelectionChanged;
            
            // 添加边框预览事件
            EnableBorderCheck.Checked += BorderSettings_Changed;
            EnableBorderCheck.Unchecked += BorderSettings_Changed;
            BorderThicknessBox.TextChanged += BorderSettings_Changed;
            BorderStartColorBox.TextChanged += BorderSettings_Changed;
            BorderEndColorBox.TextChanged += BorderSettings_Changed;
            
            // 初始预览
            UpdateBorderPreview();
            
            // 确保Canvas尺寸正确
            this.Loaded += EditorWindow_Loaded;
        }

        private void EditorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 确保OverlayCanvas的尺寸与图像匹配
            UpdateCanvasSizes();
        }

        private void UpdateCanvasSizes()
        {
            // 设置OverlayCanvas的尺寸与BaseImage匹配
            var imageWidth = BaseImage.ActualWidth > 0 ? BaseImage.ActualWidth : _workingBitmap.Width;
            var imageHeight = BaseImage.ActualHeight > 0 ? BaseImage.ActualHeight : _workingBitmap.Height;
            
            OverlayCanvas.Width = imageWidth;
            OverlayCanvas.Height = imageHeight;
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

        private void DrawingModeBtn_Click(object sender, RoutedEventArgs e)
        {
            _drawingMode = !_drawingMode;
            UpdateDrawingModeUI();
            UpdateBorderPreview();
        }

        private void UpdateDrawingModeUI()
        {
            if (_drawingMode)
            {
                DrawingModeBtn.Content = "🖼️ Preview Mode";
                DrawingModeBtn.Style = (Style)FindResource("DangerButton");
            }
            else
            {
                DrawingModeBtn.Content = "🎨 Drawing Mode";
                DrawingModeBtn.Style = (Style)FindResource("PrimaryButton");
            }
        }

        private void ToolCircleBtn_Click(object sender, RoutedEventArgs e) 
        {
            _currentTool = Tool.Circle;
        }
        private void ToolRectBtn_Click(object sender, RoutedEventArgs e) 
        {
            _currentTool = Tool.Rect;
        }
        private void ToolArrowBtn_Click(object sender, RoutedEventArgs e) 
        {
            _currentTool = Tool.Arrow;
        }

        private void OverlayCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 在边框预览模式下禁用绘图（除了绘画模式）
            if (PreviewImage.Visibility == Visibility.Visible && !_drawingMode)
            {
                return; // 不允许在边框预览上绘图
            }
            
            var canvas = sender as Canvas;
            _dragStart = e.GetPosition(canvas);
            _dragging = true;
            StartPreviewShape();
        }

        private void OverlayCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_dragging || _previewShape == null) return;
            var canvas = sender as Canvas;
            var pos = e.GetPosition(canvas);
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
            // 确保Canvas尺寸正确
            UpdateCanvasSizes();
            
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
                    OverlayCanvas.Children.Add(_previewShape);
                    break;
                case Tool.Rect:
                    _previewShape = new System.Windows.Shapes.Rectangle
                    {
                        Stroke = brush.Stroke,
                        StrokeThickness = brush.Thickness,
                        Fill = System.Windows.Media.Brushes.Transparent
                    };
                    OverlayCanvas.Children.Add(_previewShape);
                    break;
                case Tool.Arrow:
                    _previewShape = new System.Windows.Shapes.Line
                    {
                        Stroke = brush.Stroke,
                        StrokeThickness = brush.Thickness,
                        StrokeStartLineCap = System.Windows.Media.PenLineCap.Round,
                        StrokeEndLineCap = System.Windows.Media.PenLineCap.Triangle
                    };
                    OverlayCanvas.Children.Add(_previewShape);
                    break;
                default:
                    return;
            }
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
            
            // 绘制完成后更新边框预览
            UpdateBorderPreview();
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
            var name = BorderPresetBox.SelectedItem as string ?? "Custom";
            switch (name)
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
            }
            // 预设更改后也要更新预览
            UpdateBorderPreview();
        }

        private void BorderSettings_Changed(object sender, RoutedEventArgs e)
        {
            UpdateBorderPreview();
        }

        private void UpdateBorderPreview()
        {
            try
            {
                // 在绘画模式下不显示边框预览
                if (_drawingMode)
                {
                    PreviewImage.Visibility = Visibility.Collapsed;
                    BaseImage.Visibility = Visibility.Visible;
                    UpdateDrawingToolsState(true); // 启用绘图工具
                    return;
                }

                if (EnableBorderCheck.IsChecked == true)
                {
                    // 创建预览设置
                    var previewSettings = new AppSettings
                    {
                        EnableBorder = true,
                        BorderThickness = int.TryParse(BorderThicknessBox.Text, out var thickness) ? thickness : 30,
                        BorderGradientStart = string.IsNullOrWhiteSpace(BorderStartColorBox.Text) ? "#FF8B5CF6" : BorderStartColorBox.Text.Trim(),
                        BorderGradientEnd = string.IsNullOrWhiteSpace(BorderEndColorBox.Text) ? "#FFEC4899" : BorderEndColorBox.Text.Trim()
                    };

                    // 生成带边框的预览图
                    using var previewBitmap = ScreenCaptureService.ApplyOptionalBorder(_workingBitmap, previewSettings);
                    PreviewImage.Source = BitmapToImageSource(previewBitmap);
                    PreviewImage.Visibility = Visibility.Visible;
                    BaseImage.Visibility = Visibility.Collapsed;
                    UpdateDrawingToolsState(false); // 禁用绘图工具
                }
                else
                {
                    // 禁用边框时显示原图
                    PreviewImage.Visibility = Visibility.Collapsed;
                    BaseImage.Visibility = Visibility.Visible;
                    UpdateDrawingToolsState(true); // 启用绘图工具
                }
            }
            catch
            {
                // 如果预览出错，回退到原图
                PreviewImage.Visibility = Visibility.Collapsed;
                BaseImage.Visibility = Visibility.Visible;
                UpdateDrawingToolsState(true); // 启用绘图工具
            }
        }

        private void UpdateDrawingToolsState(bool enabled)
        {
            // 更新绘图工具按钮的启用状态
            ToolCircleBtn.IsEnabled = enabled;
            ToolRectBtn.IsEnabled = enabled;
            ToolArrowBtn.IsEnabled = enabled;
        }
    }
}


