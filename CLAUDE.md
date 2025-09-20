# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

FastScreeny is a Windows screenshot application built with .NET 8 WPF, focused on fast capture, editing, and output management. It supports multi-monitor setups, hotkey-driven workflows, and automatic updates.

## Build & Development Commands

### Core Development
- `dotnet restore` - Restore NuGet packages
- `dotnet build -c Debug` - Build debug version
- `dotnet build -c Release` - Build release version  
- `dotnet run --project FastScreeny.csproj` - Run application
- `dotnet format` - Apply standard formatting before commits

### Build Scripts
- `build\build_release.bat` - Publish Release build to `bin/publish`
- `build\build_installer.bat` - Create Windows installer (requires Inno Setup on PATH)

### Testing
- No tests currently exist. When adding tests, create `tests/FastScreeny.Tests` with xUnit or MSTest

## Architecture

### Core Components
- **App.xaml.cs** - Main application entry point, manages tray icon, hotkeys, updates
- **Services/** - Business logic layer (ScreenCapture, HotkeyManager, AutoUpdate, Settings)
- **Views/** - WPF windows with XAML UI and code-behind
- **Models/** - Data contracts (AppSettings, Hotkey, UpdateInfo)

### Key Services
- `ScreenCaptureService` - Handles region capture, multi-monitor support, editing workflows
- `HotkeyManager` - Global hotkey registration and management
- `AutoUpdateService` - GitHub-based automatic updates with ZIP/EXE support
- `SettingsService` - Configuration management with JSON persistence

### Windows
- `MainWindow` - Minimal main window (primarily for app initialization)
- `SettingsWindow` - Configuration interface with hotkey setup
- `OverlaySelectionWindow` - Region selection overlay with cross-monitor support
- `EditorWindow` - Screenshot editor with drawing tools and border effects
- `UpdateWindow` - Update management UI with progress tracking

## Project Structure

```
src/
├── App.xaml + App.xaml.cs          # Application entry point
├── Views/                          # WPF windows
│   ├── MainWindow.xaml + .xaml.cs
│   ├── SettingsWindow.xaml + .xaml.cs
│   ├── OverlaySelectionWindow.xaml + .xaml.cs
│   ├── EditorWindow.xaml + .xaml.cs
│   └── UpdateWindow.xaml + .xaml.cs
├── Services/                       # Business logic
│   ├── ScreenCaptureService.cs
│   ├── HotkeyManager.cs
│   ├── AutoUpdateService.cs
│   ├── SettingsService.cs
│   ├── AutoStartManager.cs
│   └── StoragePaths.cs
└── Models/                        # Data models
    ├── AppSettings.cs
    ├── Hotkey.cs
    └── UpdateInfo.cs
```

## Key Features

### Screen Capture
- Multi-monitor region selection
- Three capture modes: fullscreen, window, freeform
- Optional gradient borders with customizable styling
- Auto-save to clipboard or local files

### Editor Capabilities
- Drawing tools: rectangles, arrows, brush presets
- Real-time border preview with toggle mode
- Canvas optimization for better drawing experience
- Output to PNG, clipboard, or printer

### Auto-Update System
- GitHub-based update checking
- Support for ZIP and EXE update formats
- Automatic and manual update checks
- Progress tracking and user notifications

## Configuration & Settings

Settings are managed through `SettingsService` with JSON persistence:
- `AppSettings` class contains all user preferences
- Hotkey management with conflict detection
- Auto-start and system tray integration
- Update check intervals and automatic updates

## Development Notes

### Version Management
- Version numbers are defined in `FastScreeny.csproj` (AssemblyVersion, FileVersion, Version)
- Keep in sync with `docs/update_info.md` when making releases
- Auto-update system compares versions with GitHub releases

### Build Requirements
- .NET 8 SDK
- Inno Setup 6 (for installer creation)
- Windows 10+ target platform

### Code Style
- C#/.NET 8 with nullable reference types enabled
- 4-space indentation
- PascalCase for types/methods/properties, camelCase for locals/parameters
- Private fields use `_camelCase` convention
- Keep UI logic in Views, business logic in Services

## Installer & Distribution

The installer is built using Inno Setup with configuration in `setup/FastScreeny_Setup.iss`:
- Creates desktop shortcut, start menu entry
- Auto-start option and context menu integration
- .NET 8 Runtime detection
- Clean uninstall with registry cleanup

## Git Workflow

Follow conventional commits:
- `feat:` - New features
- `fix:` - Bug fixes  
- `docs:` - Documentation
- `refactor:` - Code refactoring
- `build:` - Build system changes

Include version updates in `docs/update_info.md` when making releases.