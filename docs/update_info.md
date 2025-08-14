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