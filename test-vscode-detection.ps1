# VSCode 实例检测测试脚本
# 编码: UTF-8 with BOM

Write-Host "╔═══════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  VSCode 实例检测工具                                  ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# 输出当前 PATH 环境变量
$pathEnv = [Environment]::GetEnvironmentVariable("PATH")
Write-Host "【步骤 1】检查 PATH 环境变量" -ForegroundColor Yellow
Write-Host "PATH 长度: $($pathEnv.Length) 字符"
Write-Host ""

# 分割 PATH 并查找 VSCode 相关路径
$paths = $pathEnv -split ';' | Where-Object {
    $_ -match 'VS Code' -or $_ -match 'codium' -or $_ -match 'vscode'
}

Write-Host "【步骤 2】在 PATH 中找到 $($paths.Count) 个 VSCode 相关路径:" -ForegroundColor Yellow
foreach ($path in $paths) {
    Write-Host ""
    Write-Host "  路径: $path" -ForegroundColor White

    if (Test-Path $path) {
        Write-Host "  ✓ 目录存在" -ForegroundColor Green

        # 检查是否是 bin 目录
        $binPath = $path
        if ((Split-Path $path -Leaf) -ne 'bin') {
            $binPath = Join-Path $path 'bin'
        }

        Write-Host "  → 检查 bin 目录: $binPath"

        if (Test-Path $binPath) {
            $files = Get-ChildItem $binPath -File
            Write-Host "  → bin 目录共有 $($files.Count) 个文件"

            # 查找 VSCode 可执行文件
            $codeFiles = $files | Where-Object {
                ($_.Name -match 'code' -or $_.Name -match 'codium') -and $_.Extension -ne '.cmd'
            }

            Write-Host "  → 匹配的 VSCode 可执行文件 ($($codeFiles.Count)):" -ForegroundColor Cyan
            foreach ($file in $codeFiles) {
                $size = $file.Length
                Write-Host "      • $($file.Name) ($('{0:N0}' -f $size) bytes)" -ForegroundColor White

                # 获取文件版本信息
                try {
                    $fileVersionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($file.FullName)
                    if ($fileVersionInfo.FileVersion) {
                        Write-Host "        版本: $($fileVersionInfo.FileVersion)" -ForegroundColor Gray
                    }
                    if ($fileVersionInfo.ProductVersion) {
                        Write-Host "        产品版本: $($fileVersionInfo.ProductVersion)" -ForegroundColor Gray
                    }
                } catch {
                    Write-Host "        ⚠ 无法获取版本信息" -ForegroundColor DarkYellow
                }
            }
        } else {
            Write-Host "  ✗ bin 目录不存在" -ForegroundColor Red
        }
    } else {
        Write-Host "  ✗ 目录不存在" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "【步骤 3】运行 'where code' 命令检查" -ForegroundColor Yellow
Write-Host "─────────────────────────────────────────────────────" -ForegroundColor DarkGray

try {
    $whereResult = where.exe code 2>$null
    if ($whereResult) {
        Write-Host "✓ 找到 VSCode 可执行文件:" -ForegroundColor Green
        $whereResult | ForEach-Object {
            Write-Host "  $_" -ForegroundColor White
        }
    } else {
        Write-Host "✗ 未找到 'code' 命令" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ 运行 'where code' 失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "【步骤 4】检查常见 VSCode 安装路径" -ForegroundColor Yellow
Write-Host "─────────────────────────────────────────────────────" -ForegroundColor DarkGray

$commonPaths = @(
    "$env:LOCALAPPDATA\Programs\Microsoft VS Code",
    "$env:LOCALAPPDATA\Programs\Microsoft VS Code Insiders",
    "${env:ProgramFiles}\Microsoft VS Code",
    "${env:ProgramFiles(x86)}\Microsoft VS Code"
)

foreach ($installPath in $commonPaths) {
    Write-Host ""
    Write-Host "  检查: $installPath" -ForegroundColor White

    if (Test-Path $installPath) {
        Write-Host "  ✓ 目录存在" -ForegroundColor Green

        # 检查 Code.exe
        $codeExe = Join-Path $installPath "Code.exe"
        if (Test-Path $codeExe) {
            Write-Host "    • Code.exe 存在" -ForegroundColor Green
            try {
                $fileVersionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($codeExe)
                Write-Host "      版本: $($fileVersionInfo.FileVersion)" -ForegroundColor Cyan
            } catch {
                Write-Host "      ⚠ 无法获取版本" -ForegroundColor DarkYellow
            }
        } else {
            Write-Host "    ✗ Code.exe 不存在" -ForegroundColor Red
        }

        # 检查 bin 目录
        $binPath = Join-Path $installPath "bin"
        if (Test-Path $binPath) {
            Write-Host "    • bin 目录存在" -ForegroundColor Green
            $binFiles = Get-ChildItem $binPath -File | Where-Object { $_.Name -match 'code' }
            foreach ($file in $binFiles) {
                Write-Host "      - $($file.Name)" -ForegroundColor Cyan
            }
        } else {
            Write-Host "    ✗ bin 目录不存在" -ForegroundColor Red
        }
    } else {
        Write-Host "  ✗ 目录不存在" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "─────────────────────────────────────────────────────" -ForegroundColor DarkGray
Write-Host "检测完成！按任意键退出..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
