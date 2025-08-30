# Update 1.0.3

## 🎨 New Features
- **Auto-Update System**: Comprehensive automatic update functionality with GitHub integration
- **ZIP Update Support**: Optimized update process using ZIP files for faster downloads
- **Multi-Format Updates**: Support for both ZIP archives and EXE installers
- **Smart Update Detection**: Intelligent version comparison and update availability checking

## 🔧 Improvements
- Enhanced update UI with progress tracking and detailed version information
- Automatic and manual update check options with configurable intervals
- Streamlined update process with batch file automation for seamless replacement
- Full English localization for all update-related UI and notifications
- Improved error handling and user feedback during update operations

## 🐛 Bug Fixes
- Resolved namespace conflicts in MessageBox references
- Fixed update file format detection and handling
- Improved application restart reliability after updates
- Fix the admin requirement for setups

---

# Update 1.0.2

## 🎨 New Features
- **File Structure Refactor**: Reorganized project file structure for better categorization
- **Transparent Overlay Optimization**: Improved visual effects of region selection overlay

## 🔧 Improvements
- Removed semi-transparent background of overlay for full screen visibility
- Enhanced border display effect of selection area
- No admin requirement anymore for the setup exe

## 🐛 Bug Fixes
- Fixed mouse event capture issue during region selection
- Resolved overlay display anomalies on certain resolutions

---

# Update 1.0.1

## 🎨 New Features
- **Enhanced Editor Interface**: Added visual button press feedback with smooth animations
- **Drawing Mode**: New toggle button to temporarily disable border preview for drawing
- **Improved Border Preview**: Real-time gradient border preview with instant updates
- **Better Tool Management**: Simplified tool switching and state management
- **Canvas Size Optimization**: Auto-adjusting canvas dimensions for better drawing experience

## 🔧 Improvements
- Removed crop mode functionality for simplified user experience
- Enhanced button interactions with 1px press offset animation
- Improved visual feedback with blue border highlights on hover/press
- Better error handling in border preview generation
- Optimized mouse event handling for drawing tools

## 🐛 Bug Fixes
- Fixed drawing shapes disappearing after release in border preview mode
- Resolved conflicts between border preview and drawing functionality
- Fixed canvas sizing issues that prevented proper shape drawing
- Improved tool state synchronization

---

# Update 1.0.0

## 🚀 Initial Features
- Initial release version
- Region screenshot support (multi-monitor compatible)
- Built-in annotation tools (arrows/rectangles/text)
- Auto-save as PNG format

## ⚙️ Technical Upgrades
- Migrated to .NET 8 runtime
- Refactored image processing core (System.Drawing alternatives)

## 🐛 Known Issues
- Editor interface scaling issues on high DPI screens
- Some hotkeys conflict with system shortcuts

## 📅 2025-01-14
### Future Plans
- Cloud storage integration (OneDrive/Google Drive)
- GIF screen recording support
