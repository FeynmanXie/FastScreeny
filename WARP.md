# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

FastScreeny is a Windows screenshot application built with .NET 8 WPF, focused on fast capture, editing, and output management. It supports multi-monitor setups, hotkey-driven workflows, and automatic updates.

## Common Development Commands

### Core Development
- `dotnet restore` - Restore NuGet packages
- `dotnet build -c Debug` - Build debug version
- `dotnet build -c Release` - Build release version  
- `dotnet run --project FastScreeny.csproj` - Run application
- `dotnet format` - Apply standard formatting before commits

### Build Scripts
- `build\build_release.bat` - Publish Release build to `bin/Release/net8.0-windows/`
- `build\build_release.bat run` - Build and run the application automatically
- `build\build_installer.bat` - Create Windows installer (requires Inno Setup on PATH)

### Testing
- No unit tests currently exist. When adding tests, create `tests/FastScreeny.Tests` with xUnit or MSTest
- Manual testing: Run app with `--background` flag to test tray functionality

## Architecture Overview

### Application Entry Point
The application uses a **tray-first** architecture where the main UI is the system tray icon, not a traditional main window:

- **App.xaml.cs** - Main application entry point, manages tray icon, global hotkeys, and auto-updates
- **System Tray Integration** - Primary interface through NotifyIcon with context menu
- **Background Mode** - App can run headless with `--background` argument

### Core Services Layer
- `ScreenCaptureService` - Multi-monitor screen capture with DPI-aware coordinate transformation
- `HotkeyManager` - Global hotkey registration and management with conflict detection
- `AutoUpdateService` - GitHub-based automatic updates supporting ZIP/EXE formats
- `SettingsService` - JSON-based configuration persistence with hot-reload
- `AutoStartManager` - Windows startup integration via registry

### Views/Windows Architecture
- `MainWindow` - Minimal placeholder (app is tray-based)
- `SettingsWindow` - Configuration interface with hotkey setup and validation
- `OverlaySelectionWindow` - **Full-screen overlay** across all monitors for region selection
- `EditorWindow` - Screenshot editor with drawing tools and border effects
- `UpdateWindow` - Update management UI with download progress

### Data Models
- `AppSettings` - Configuration model with hotkey, file naming, and update preferences
- `Hotkey` - Hotkey representation with parsing and validation
- `UpdateInfo` - Update metadata from GitHub API

## Key Technical Patterns

### Multi-Monitor DPI Handling
The app handles complex multi-monitor setups with different DPI scales:
- Uses WPF's `PresentationSource.CompositionTarget` for accurate DPI transformations
- Fallback coordinate systems for edge cases
- Cross-screen overlay positioning with boundary validation

### Hotkey System
- Global hotkey registration using Windows API
- Conflict detection and user notification
- Hot-reload of hotkey settings without restart

### Update System
- GitHub API integration for version checking
- Automatic download and installation workflow
- Support for both ZIP archives and standalone executables

## Development Environment

### Requirements
- .NET 8 SDK
- Windows 10+ (target platform)
- Inno Setup 6 (for installer creation)

### Project Structure
```
src/
├── App.xaml + App.xaml.cs          # Application entry point
├── Views/                          # WPF windows
├── Services/                       # Business logic layer
└── Models/                         # Data models
build/                              # Build automation scripts
setup/                              # Installer configuration
```

### Version Management
- Version numbers defined in `FastScreeny.csproj` (AssemblyVersion, FileVersion, Version)
- Update `docs/update_info.md` when making releases
- Auto-update system compares versions with GitHub releases

### Code Style
- C#/.NET 8 with nullable reference types enabled
- 4-space indentation, PascalCase for types/methods/properties
- Private fields use `_camelCase` convention
- UI logic in Views, business logic in Services

## Configuration & Settings

Settings are managed through `SettingsService` with JSON persistence to `%APPDATA%\FastScreeny\`:
- Real-time hotkey conflict validation
- Auto-start registry integration
- Configurable update check intervals
- File naming templates with DateTime variables

## Testing & Debugging

### Manual Testing Scenarios
- Multi-monitor region selection across different DPI scales
- Hotkey conflicts with system and other applications  
- Update process with mock GitHub releases
- Tray icon persistence after Windows updates/restarts

### Common Issues
- **DPI Scaling**: Test on mixed DPI monitor setups
- **Hotkey Conflicts**: Validate against common system shortcuts
- **File Locks**: Ensure proper disposal of bitmap resources
- **Update Failures**: Handle network issues and corrupted downloads

## Installer & Distribution

The installer uses Inno Setup (`setup/FastScreeny_Setup.iss`):
- .NET 8 Runtime detection and installation
- Auto-start option and Windows context menu integration
- Clean uninstall with registry cleanup
- Desktop shortcut and start menu entry creation