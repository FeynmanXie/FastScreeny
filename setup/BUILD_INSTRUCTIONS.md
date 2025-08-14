# FastScreeny 安装程序构建说明

## 📦 构建环境要求

### 必需组件
1. **.NET 8 SDK**
   - 下载：https://dotnet.microsoft.com/download/dotnet/8.0
   - 用于编译 C# 应用程序

2. **Inno Setup 6**（构建安装程序需要）
   - 下载：https://jrsoftware.org/isdl.php
   - 免费的 Windows 安装程序制作工具

### 可选组件
- **PowerShell 5.0+**（创建图标需要，Windows 10+ 默认已安装）

## 🚀 快速开始

### 方法1：一键构建（推荐）
```bash
# 构建应用程序 + 安装程序（如果有 Inno Setup）
quick_build.bat

# 或者完整构建流程
build_installer.bat
```

### 方法2：分步构建
```bash
# 1. 只构建应用程序
build_release.bat

# 2. 构建安装程序（需要先完成步骤1）
build_installer.bat
```

## 📁 输出文件

构建完成后，文件结构如下：
```
FastScreeny/
├── bin/Release/net8.0-windows/          # 应用程序输出
│   ├── FastScreeny.exe                   # 主程序
│   ├── FastScreeny.dll
│   └── ...
├── dist/installer/                       # 安装程序输出
│   └── FastScreeny_Setup_v1.0.0.exe    # 安装程序
└── setup/                               # 安装程序资源
    ├── app.ico                          # 应用图标
    ├── LICENSE.txt                      # 许可证
    └── README_INSTALL.txt               # 安装后说明
```

## ⚙️ 安装程序功能

### 安装选项
- ✅ **桌面快捷方式**：创建桌面图标
- ✅ **开始菜单**：添加到开始菜单
- ✅ **开机自启**：Windows 启动时自动运行
- ✅ **右键菜单**：在文件管理器右键菜单添加快捷截图

### 系统集成
- 自动检测 .NET 8 Runtime
- 智能停止正在运行的程序
- 注册表清理（卸载时）
- 创建用户文档目录

### 卸载功能
- 完全移除程序文件
- 清理注册表项
- 移除开机自启动
- 保留用户截图文件

## 🔧 自定义安装程序

### 修改版本信息
编辑 `setup/FastScreeny_Setup.iss`：
```ini
AppVersion=1.0.0                    # 版本号
OutputBaseFilename=FastScreeny_Setup_v1.0.0  # 输出文件名
```

### 修改安装选项
在 `[Tasks]` 部分调整默认选项：
```ini
Name: "autostart"; Description: "开机自动启动(&A)"; GroupDescription: "系统集成:"; Flags: checkablealone
```
- 添加 `checked` 标志：默认选中
- 添加 `unchecked` 标志：默认不选中

### 添加自定义文件
在 `[Files]` 部分添加：
```ini
Source: "path\to\your\file"; DestDir: "{app}"; Flags: ignoreversion
```

## 🐛 故障排除

### 常见问题

**1. "未找到 .NET SDK"**
- 解决：安装 .NET 8 SDK 并重启命令提示符

**2. "未找到 Inno Setup"**
- 解决：下载并安装 Inno Setup 6，确保安装到默认路径

**3. "编译失败"**
- 检查项目文件是否完整
- 确保没有语法错误
- 尝试手动运行 `dotnet build -c Release`

**4. "图标创建失败"**
- PowerShell 执行策略限制：运行 `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser`
- 或手动放置 `app.ico` 文件到 `setup/` 目录

### 日志调试

如果安装程序构建失败，可以查看详细日志：
```bash
# 启用详细输出
ISCC.exe /O+ "setup\FastScreeny_Setup.iss"
```

## 📋 发布检查清单

发布前请确认：
- [ ] 应用程序正常启动和运行
- [ ] 所有功能测试通过
- [ ] 版本号已更新
- [ ] 安装程序可以正常安装和卸载
- [ ] 开机自启动功能正常
- [ ] 右键菜单功能正常
- [ ] 文档和许可证文件正确

## 🔗 相关链接

- [Inno Setup 官方文档](https://jrsoftware.org/ishelp/)
- [.NET 8 下载](https://dotnet.microsoft.com/download/dotnet/8.0)
- [FastScreeny 项目主页](https://github.com/fastscreeny/fastscreeny)
