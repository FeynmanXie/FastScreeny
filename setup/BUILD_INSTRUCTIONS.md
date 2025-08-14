# FastScreeny Installer Build Instructions

## 📦 Build Environment Requirements

### Required Components
1. **.NET 8 SDK**
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - Used to compile C# applications

2. **Inno Setup 6** (required for building installer)
   - Download: https://jrsoftware.org/isdl.php
   - Free Windows installer creation tool

### Optional Components
- **PowerShell 5.0+** (needed for icon creation, pre-installed on Windows 10+)

## 🚀 Quick Start

### Method 1: One-Click Build (Recommended)
```bash
# Build application + installer (if Inno Setup is available)
quick_build.bat

# Or full build process
build_installer.bat
```

### Method 2: Step-by-Step Build
```bash
# 1. Build application only
build_release.bat

# 2. Build installer (requires step 1 to be completed first)
build_installer.bat
```

## 📁 Output Files

After building, the file structure is as follows:
```
FastScreeny/
├── bin/Release/net8.0-windows/          # Application output
│   ├── FastScreeny.exe                   # Main program
│   ├── FastScreeny.dll
│   └── ...
├── dist/installer/                       # Installer output
│   └── FastScreeny_Setup_v1.0.0.exe    # Installer
└── setup/                               # Installer resources
    ├── app.ico                          # Application icon
    ├── LICENSE.txt                      # License
    └── README_INSTALL.txt               # Post-installation instructions
```

## ⚙️ Installer Features

### Installation Options
- ✅ **Desktop Shortcut**: Create desktop icon
- ✅ **Start Menu**: Add to start menu
- ✅ **Auto-start**: Automatically run when Windows starts
- ✅ **Context Menu**: Add quick screenshot to file explorer right-click menu

### System Integration
- Automatic .NET 8 Runtime detection
- Intelligent stopping of running programs
- Registry cleanup (during uninstall)
- Create user documents directory

### Uninstall Features
- Complete removal of program files
- Clean registry entries
- Remove auto-start
- Preserve user screenshot files

## 🔧 Customize Installer

### Modify Version Information
Edit `setup/FastScreeny_Setup.iss`:
```ini
AppVersion=1.0.0                    # Version number
OutputBaseFilename=FastScreeny_Setup_v1.0.0  # Output filename
```

### Modify Installation Options
Adjust default options in `[Tasks]` section:
```ini
Name: "autostart"; Description: "&Auto-start on boot"; GroupDescription: "System Integration:"; Flags: checkablealone
```
- Add `checked` flag: Default selected
- Add `unchecked` flag: Default unselected

### Add Custom Files
Add to `[Files]` section:
```ini
Source: "path\to\your\file"; DestDir: "{app}"; Flags: ignoreversion
```

## 🐛 Troubleshooting

### Common Issues

**1. ".NET SDK not found"**
- Solution: Install .NET 8 SDK and restart command prompt

**2. "Inno Setup not found"**
- Solution: Download and install Inno Setup 6, ensure installation to default path

**3. "Build failed"**
- Check if project files are complete
- Ensure no syntax errors
- Try manually running `dotnet build -c Release`

**4. "Icon creation failed"**
- PowerShell execution policy restriction: Run `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser`
- Or manually place `app.ico` file in `setup/` directory

### Debug Logging

If installer build fails, you can view detailed logs:
```bash
# Enable verbose output
ISCC.exe /O+ "setup\FastScreeny_Setup.iss"
```

## 📋 Release Checklist

Before release, please confirm:
- [ ] Application starts and runs normally
- [ ] All functionality tests pass
- [ ] Version number updated
- [ ] Installer can install and uninstall normally
- [ ] Auto-start functionality works
- [ ] Right-click menu functionality works
- [ ] Documentation and license files are correct

## 🔗 Related Links

- [Inno Setup Official Documentation](https://jrsoftware.org/ishelp/)
- [.NET 8 Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- [FastScreeny Project Homepage](https://github.com/fastscreeny/fastscreeny)