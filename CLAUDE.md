# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

这是一个 Flow Launcher 插件,用于快速打开 VSCode 中最近使用的工作区。支持:
- 本地工作区 (文件夹和工作区文件)
- 远程工作区 (WSL/SSH/Codespaces)
- 自定义工作区

## 构建命令

```bash
# 还原依赖
dotnet restore

# Debug 构建
dotnet build -c Debug

# Release 构建
dotnet build -c Release

# 发布插件 (打包成 zip)
dotnet publish Flow.Plugin.VSCodeWorkspaces.csproj -r win-x64 -c Release -o "Flow.Plugin.VSCodeWorkspaces-<version>"
```

构建输出位置:
- Debug: `Output\Debug\VSCodeWorkspaces\`
- Release: `Output\Release\VSCodeWorkspaces\`

项目要求:
- .NET 7.0 Windows 目标框架 (`net7.0-windows`)
- x64 平台
- 警告被视为错误 (`TreatWarningsAsErrors=true`)

## 代码架构

### 核心入口点
- **[Main.cs](Main.cs)** - 实现 Flow Launcher 的插件接口 (`IPlugin`, `IPluginI18n`, `ISettingProvider`, `IContextMenu`)
  - `Query()` - 根据用户输入搜索工作区
  - `Init()` - 初始化插件,加载 VSCode 实例
  - `LoadContextMenus()` - 为本地工作区提供右键菜单 (打开文件夹)

### 主要组件

#### [VSCodeHelper/VSCodeInstances.cs](VSCodeHelper/VSCodeInstances.cs)
- 扫描系统 PATH 环境变量查找所有 VSCode 安装 (Stable/Insiders/Exploration/VSCodium)
- 从 VSCode 可执行文件提取图标并与 folder/monitor 图标合成
- 识别便携版安装 (检查 data/user-data 目录)
- 每个实例包含: `ExecutablePath`, `AppData`, `WorkspaceIcon`, `RemoteIcon`

#### [WorkspacesHelper/VSCodeWorkspacesApi.cs](WorkspacesHelper/VSCodeWorkspacesApi.cs)
- 从三个来源读取最近打开的工作区:
  1. **storage.json** (VSCode < 1.64) - JSON 文件包含 `Workspaces3` 或 `Entries` 数组
  2. **state.vscdb** (VSCode >= 1.64) - SQLite 数据库,读取 `history.recentlyOpenedPathsList`
  3. **用户自定义工作区** (来自设置)
- 解析 VSCode URI 格式: `file:///`, `vscode-remote://`, `vscode-local://`
- 支持文件夹 (.code-workspace) 和工作区文件

#### [RemoteMachinesHelper/VSCodeRemoteMachinesApi.cs](RemoteMachinesHelper/VSCodeRemoteMachinesApi.cs)
- 读取 VSCode 的 `settings.json` 获取 SSH 配置文件路径 (`remote.SSH.configFile`)
- 使用 [SshConfigParser](SshConfigParser/) 解析 SSH 主机配置
- 为每个 SSH 主机创建远程工作区结果

#### [SshConfigParser/SshConfig.cs](SshConfigParser/SshConfig.cs)
- 解析标准 SSH 配置文件格式 (Host, HostName, User 等指令)

### 数据模型

- **[VSCodeWorkspace.cs](WorkspacesHelper/VSCodeWorkspace.cs)** - 工作区信息 (路径、类型、位置、关联的 VSCode 实例)
- **[VSCodeInstance.cs](VSCodeHelper/VSCodeInstance.cs)** - VSCode 安装信息
- **[VSCodeRemoteMachine.cs](RemoteMachinesHelper/VSCodeRemoteMachine.cs)** - 远程机器信息

### URI 解析逻辑

[ParseVSCodeUri.cs](WorkspacesHelper/ParseVSCodeUri.cs) 处理各种 VSCode URI 格式:
- 本地: `file:///c:/path/to/folder`
- WSL: `vscode-local://wsl%2BUbuntu/path`
- SSH: `vscode-remote://ssh-remote%2Bhost/path`
- Codespaces: `vscode-remote://codespaces%2Bname/path`

### 设置系统

- **[Settings.cs](Settings.cs)** - 用户配置:
  - `DiscoverWorkspaces` - 自动发现工作区
  - `DiscoverMachines` - 自动发现远程机器
  - `CustomWorkspaces` - 用户自定义工作区 URI 列表
- **[SettingsView.xaml](SettingsView.xaml)** + [SettingsView.xaml.cs](SettingsView.xaml.cs) - WPF 设置界面

## 开发注意事项

1. **版本同步**: 修改功能后需要同步更新 [plugin.json](plugin.json) 中的 `Version` 字段和 [README.md](README.md)

2. **错误处理**: VSCode 配置文件解析失败时会通过 `Main.Context.API.LogException()` 记录异常,不会抛出异常中断插件

3. **图标资源**: 图标放在 [Images/](Images/) 目录,构建时复制到输出目录

4. **国际化**: 字符串资源在 [Properties/Resources.resx](Properties/Resources.resx),使用 `IPluginI18n` 接口支持语言切换

5. **去重逻辑**: 工作区结果使用 `Distinct()` 去重,基于 `VSCodeWorkspace` 的相等性比较

6. **Ctrl+点击**: 对于本地工作区,按住 Ctrl 点击会打开文件夹而不是 VSCode

7. **警告即错误**: 项目配置 `TreatWarningsAsErrors=true`,代码必须无警告才能编译

## 源代码来源

代码源自 [Microsoft PowerToys](https://github.com/microsoft/PowerToys/tree/main/src/modules/launcher/Plugins/Community.PowerToys.Run.Plugin.VSCodeWorkspaces),已适配 Flow Launcher API。
