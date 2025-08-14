# FastScreeny - Quick Screenshot Tool

## 🖥️ Overview
FastScreeny is a lightweight Windows screenshot tool designed for **fast capture, efficient editing, and smart output**. Core philosophy: **hotkey-driven, distraction-free, productivity-first**.

## 🌟 Core Features
```mermaid
pie
    title Feature Distribution
    "Region Capture" : 35
    "Drawing Tools" : 30
    "Border Effects" : 20
    "System Integration" : 15
```

### 1. Smart Capture
- **Multi-Monitor Support**: Seamless cross-screen selection
- **Three Capture Modes**:
  - Fullscreen (auto-detect primary display)
  - Window (smart app window detection)
  - Freeform (pixel-level adjustment)

### 2. Instant Editor
```mermaid
graph LR
    A[Drawing Tools] --> B[Circle/Rectangle]
    A --> C[Arrow Annotations]
    A --> D[Brush Presets]
    E[Border Effects] --> F[Gradient Borders]
    E --> G[Real-time Preview]
    H[Drawing Mode] --> I[Preview Toggle]
```

### 3. Advanced Editor Features
- **Drawing Mode Toggle**: Switch between preview and drawing modes instantly
- **Interactive Button Feedback**: Visual press animations with 1px offset
- **Real-time Border Preview**: Live gradient border effects with customizable colors
- **Smart Tool Management**: Context-aware tool enabling/disabling
- **Brush Presets**: Multiple color and thickness combinations

### 4. Output Management
- **Saving Strategies**:
  - Auto-sort by date
  - Custom filename templates (time variables supported)
- **Multi-Destination Output**:
  - Local PNG (lossless quality)
  - Clipboard (instant paste)
  - Printer (one-click printing)

## ⚡ Tech Stack
```mermaid
classDiagram
    class ScreenCaptureService{
        +CaptureRegion()
        +ApplyOptionalBorder()
        +SaveBitmapWithSettings()
    }
    class EditorWindow{
        +DrawingTools
        +BorderPreview
        +DrawingModeToggle
    }
    class SettingsService{
        +HotkeyManager
        +AppSettings
        +StoragePaths
    }
    ScreenCaptureService <-- EditorWindow
    ScreenCaptureService <-- SettingsService
```

## 📦 System Requirements
| Component | Specification |
|-----------|---------------|
| OS | Windows 10+ |
| Runtime | .NET 8 Desktop |
| RAM | Min 500MB free |
| GPU | DirectX 10+ support |

> Note: This documentation corresponds to v1.0.1 with enhanced editor features. Updates and changelog are maintained in `docs/update_info.md`