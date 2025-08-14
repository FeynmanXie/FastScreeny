# 创建简单的 FastScreeny 图标
# 这个脚本会创建一个基本的 ICO 文件

param(
    [string]$OutputPath = "setup\app.ico"
)

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

# 创建 32x32 的位图
$bitmap = New-Object System.Drawing.Bitmap(32, 32)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# 设置高质量渲染
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias

# 创建渐变背景
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Point(0, 0)),
    (New-Object System.Drawing.Point(32, 32)),
    [System.Drawing.Color]::FromArgb(139, 92, 246),  # 紫色
    [System.Drawing.Color]::FromArgb(236, 72, 153)   # 粉色
)

# 绘制圆形背景
$graphics.FillEllipse($brush, 2, 2, 28, 28)

# 绘制边框
$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, 2)
$graphics.DrawEllipse($pen, 2, 2, 28, 28)

# 绘制截图符号 (简化的相机图标)
$whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$graphics.FillRectangle($whiteBrush, 8, 12, 16, 10)
$graphics.FillRectangle($whiteBrush, 12, 8, 8, 4)

# 绘制镜头
$graphics.DrawEllipse($pen, 12, 14, 8, 6)

# 清理资源
$graphics.Dispose()
$brush.Dispose()
$pen.Dispose()
$whiteBrush.Dispose()

# 保存为 ICO 格式
try {
    # 创建输出目录
    $outputDir = Split-Path $OutputPath -Parent
    if (-not (Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }
    
    # 保存图标
    $bitmap.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Icon)
    Write-Host "图标已创建: $OutputPath" -ForegroundColor Green
    
} catch {
    Write-Host "创建图标失败: $_" -ForegroundColor Red
} finally {
    $bitmap.Dispose()
}
