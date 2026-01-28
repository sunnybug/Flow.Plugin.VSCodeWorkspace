A plugin for Flow Launcher that allows user to quickly open the recent workspace of VSCode.

![image](https://github.com/taooceros/Flow.Plugin.VSCodeWorkspace/assets/45326534/15609d5d-869f-4df4-b62a-0d0d9b3fc31a)

The default actionkeyword is `{`.

Remote Workspace (WSL/SSH) are also supported.

![image](https://github.com/taooceros/Flow.Plugin.VSCodeWorkspace/assets/45326534/277df331-e124-448b-8411-d20bf6418b76)

Hope you enjoy it!

The original source code is from [Microsoft Powertoys](https://github.com/microsoft/PowerToys/tree/main/src/modules/launcher/Plugins/Community.PowerToys.Run.Plugin.VSCodeWorkspaces).

## 更新日志

### v2.0.1 (2025-01-26)
- 升级到 .NET 8.0 框架
- 添加详细的调试日志，帮助诊断工作区扫描问题
- 改进日志输出，包括 VSCode 实例扫描、AppData 路径、数据库查询结果等信息
- **VSCode 实例扫描改进**
  - 添加文件版本验证，确保只加载真正的 VSCode 可执行文件
  - 支持更多 VSCode 安装位置（便携版、标准安装、自定义路径）
  - 改进版本检测逻辑，支持 Stable/Insiders/Exploration/VSCodium
- **SSH 配置文件处理增强**
  - 避免重复读取同一个 SSH 配置文件
  - 当未配置自定义 SSH 文件时，自动回退到默认 `~/.ssh/config`
  - 添加详细的 SSH 配置解析日志
- **远程机器显示改进**
  - SSH 主机标题添加 "SSH:" 前缀以便区分
- **诊断工具**
  - 新增 `check-duplicate-plugins.ps1` - 检查重复插件安装
  - 新增 `check-vscode.bat` - 检查 VSCode 安装状态
  - 新增 `VSCodeDiscovery` 项目 - VSCode 实例检测工具
- **开发工具改进**
  - 新增 `build-install.ps1` - 自动化编译和安装脚本
  - 清理旧插件目录，避免重复插件问题
  - 自动关闭/重启 Flow Launcher
- **SSH 配置解析增强**
  - 改进正则匹配逻辑，跳过无效匹配
  - 验证 Host 属性存在性，避免空主机条目
- **项目配置优化**
  - 添加 `CLAUDE.md` 项目指南文档
  - 添加 Claude Code 命令配置文件
