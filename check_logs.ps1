# UTF-8 BOM
# Check Flow Launcher logs for SSH machine discovery

$logPath = "C:\Users\hotgame\AppData\Roaming\FlowLauncher\Logs\2.0.3"

if (-not (Test-Path $logPath)) {
    Write-Host "Log directory not found: $logPath"
    exit 1
}

$latestLog = Get-ChildItem -Path $logPath -Filter '*.txt' | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($latestLog) {
    Write-Host "Latest log file: $($latestLog.FullName)"
    Write-Host "Modified time: $($latestLog.LastWriteTime)"
    Write-Host ""
    Write-Host "=== VSCodeWorkspaces and SSH Related Logs ==="
    Write-Host ""

    $content = Get-Content $latestLog.FullName -Tail 500

    # Find all VSCodeWorkspaces and SSH related logs
    $relevantLogs = $content | Where-Object {
        $_ -match 'VSCodeWorkspaces|VSCodeRemoteMachines|SSH|远程机器'
    }

    if ($relevantLogs) {
        $relevantLogs | ForEach-Object { Write-Host $_ }
    } else {
        Write-Host "No relevant logs found"
        Write-Host ""
        Write-Host "=== All Log Content (Last 100 Lines) ==="
        Get-Content $latestLog.FullName -Tail 100 | ForEach-Object { Write-Host $_ }
    }
} else {
    Write-Host "No log files found"
}
