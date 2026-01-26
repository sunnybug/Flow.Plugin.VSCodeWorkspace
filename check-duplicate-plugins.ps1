# UTF-8 BOM
# 检查 Flow Launcher 中是否有重复的 VS Code Workspaces 插件

Write-Host "=== VS Code Workspaces 插件诊断工具 ===" -ForegroundColor Cyan
Write-Host ""

# 1. 检查 Flow Launcher 插件目录
Write-Host "1. Flow Launcher 插件目录:" -ForegroundColor Yellow
$pluginsPath = Join-Path $env:APPDATA "FlowLauncher\Plugins"
if (Test-Path $pluginsPath) {
    $vscodePlugins = Get-ChildItem $pluginsPath -Directory | Where-Object { $_.Name -match 'VS.*Code.*Workspace' }
    Write-Host "   找到 $($vscodePlugins.Count) 个 VS Code Workspaces 插件`n" -ForegroundColor White

    foreach ($plugin in $vscodePlugins) {
        $pluginJsonPath = Join-Path $plugin.FullName "plugin.json"
        if (Test-Path $pluginJsonPath) {
            try {
                $pluginJson = Get-Content $pluginJsonPath -Raw | ConvertFrom-Json
                Write-Host "   目录: $($plugin.Name)" -ForegroundColor Green
                Write-Host "   版本: $($pluginJson.Version)" -ForegroundColor Cyan
                Write-Host "   ID: $($pluginJson.ID)" -ForegroundColor Gray
                Write-Host "   路径: $($plugin.FullName)" -ForegroundColor Gray
                Write-Host ""
            } catch {
                Write-Host "   无法读取 plugin.json: $pluginJsonPath" -ForegroundColor Red
            }
        }
    }
} else {
    Write-Host "   未找到插件目录" -ForegroundColor Red
}
Write-Host ""

# 2. 检查开发输出目录
Write-Host "2. 开发环境输出目录:" -ForegroundColor Yellow
$devPath = "d:\xsw\code\Flow.Plugin.VSCodeWorkspace\Output"
if (Test-Path $devPath) {
    $pluginJsonFiles = Get-ChildItem $devPath -Recurse -Filter "plugin.json"
    Write-Host "   找到 $($pluginJsonFiles.Count) 个 plugin.json 文件`n" -ForegroundColor White

    foreach ($jsonFile in $pluginJsonFiles) {
        try {
            $json = Get-Content $jsonFile.FullName -Raw | ConvertFrom-Json
            Write-Host "   路径: $($jsonFile.FullName)" -ForegroundColor Gray
            Write-Host "   版本: $($json.Version)" -ForegroundColor Cyan
            Write-Host "   ID: $($json.ID)" -ForegroundColor Gray
            Write-Host ""
        } catch {
            Write-Host "   无法读取: $($jsonFile.FullName)" -ForegroundColor Red
        }
    }
} else {
    Write-Host "   未找到开发输出目录" -ForegroundColor Red
}
Write-Host ""

# 3. 检查最新的日志文件
Write-Host "3. 最新日志中的插件信息:" -ForegroundColor Yellow
$logPath = Join-Path $env:APPDATA "FlowLauncher\Logs"
if (Test-Path $logPath) {
    # 获取最新的版本目录
    $latestVersionDir = Get-ChildItem $logPath -Directory | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($latestVersionDir) {
        # 获取最新的日志文件
        $latestLog = Get-ChildItem $latestVersionDir.FullName -Recurse -File -Filter "*.txt" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($latestLog) {
            Write-Host "   日志文件: $($latestLog.Name)" -ForegroundColor Gray
            Write-Host "   修改时间: $($latestLog.LastWriteTime)" -ForegroundColor Gray
            Write-Host ""
            Write-Host "   插件相关日志 (最近 20 条):" -ForegroundColor White

            $logs = Get-Content $latestLog.FullName | Select-String -Pattern "VS Code.*Workspace|Duplicate.*plugin|525995402BEF4A8CA860D92F6D108092" | Select-Object -Last 20
            if ($logs) {
                foreach ($log in $logs) {
                    $line = $log.ToString()
                    if ($line -match "WARN|ERROR") {
                        Write-Host "   $line" -ForegroundColor Red
                    } else {
                        Write-Host "   $line" -ForegroundColor Gray
                    }
                }
            } else {
                Write-Host "   未找到相关日志" -ForegroundColor Gray
            }
        } else {
            Write-Host "   未找到日志文件" -ForegroundColor Red
        }
    } else {
        Write-Host "   未找到日志目录" -ForegroundColor Red
    }
} else {
    Write-Host "   未找到日志目录" -ForegroundColor Red
}
Write-Host ""

# 4. 总结
Write-Host "=== 诊断建议 ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "如果看到重复插件警告，可能的原因:" -ForegroundColor Yellow
Write-Host "1. Flow Launcher 启动时扫描了开发环境输出目录" -ForegroundColor White
Write-Host "2. 之前安装的旧版本没有完全删除" -ForegroundColor White
Write-Host "3. 插件目录中有多个版本" -ForegroundColor White
Write-Host ""
Write-Host "解决方法:" -ForegroundColor Yellow
Write-Host "1. 确保只保留一个版本的插件在 Flow Launcher 插件目录中" -ForegroundColor White
Write-Host "2. 重启 Flow Launcher 以清除缓存" -ForegroundColor White
Write-Host "3. 检查开发环境输出目录是否被 Flow Launcher 扫描" -ForegroundColor White
Write-Host ""

Write-Host "按任意键退出..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
