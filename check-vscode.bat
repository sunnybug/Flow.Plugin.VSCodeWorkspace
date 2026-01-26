@echo off
chcp 65001 >nul
echo VSCode 检测工具
echo.

echo 【步骤 1】运行 where code 命令
where code
echo.

echo 【步骤 2】检查 PATH 中的 VSCode 路径
echo %PATH% | findstr /i "VS Code"
echo.

echo 【步骤 3】检查常见安装路径
if exist "%LOCALAPPDATA%\Programs\Microsoft VS Code\Code.exe" (
    echo   找到: %LOCALAPPDATA%\Programs\Microsoft VS Code\Code.exe
    "%LOCALAPPDATA%\Programs\Microsoft VS Code\Code.exe" --version
) else (
    echo   未找到: %%LOCALAPPDATA%%\Programs\Microsoft VS Code\Code.exe
)
echo.

echo 【步骤 4】检查 bin 目录
if exist "%LOCALAPPDATA%\Programs\Microsoft VS Code\bin\" (
    echo   bin 目录存在:
    dir /b "%LOCALAPPDATA%\Programs\Microsoft VS Code\bin\"
) else (
    echo   bin 目录不存在
)
echo.

pause
