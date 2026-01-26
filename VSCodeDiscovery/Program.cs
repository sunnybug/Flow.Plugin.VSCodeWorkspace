using System;
using System.Diagnostics;
using System.IO;
using Flow.Plugin.VSCodeWorkspaces.VSCodeHelper;

namespace VSCodeDiscovery;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
        Console.WriteLine("║  VSCode 实例检测工具                                  ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // 输出当前 PATH 环境变量
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        Console.WriteLine($"【步骤 1】检查 PATH 环境变量");
        Console.WriteLine($"PATH 长度: {pathEnv.Length} 字符");
        Console.WriteLine();

        var paths = pathEnv.Split(";").Where(x =>
            x.Contains("VS Code", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("codium", StringComparison.OrdinalIgnoreCase) ||
            x.Contains("vscode", StringComparison.OrdinalIgnoreCase)).ToList();

        Console.WriteLine($"【步骤 2】在 PATH 中找到 {paths.Count} 个 VSCode 相关路径:");
        foreach (var path in paths)
        {
            Console.WriteLine($"\n  路径: {path}");
            if (Directory.Exists(path))
            {
                Console.WriteLine($"  ✓ 目录存在");

                var newPath = path;
                if (!Path.GetFileName(path).Equals("bin", StringComparison.OrdinalIgnoreCase))
                    newPath = Path.Combine(path, "bin");

                Console.WriteLine($"  → 检查 bin 目录: {newPath}");

                if (Directory.Exists(newPath))
                {
                    var files = Directory.EnumerateFiles(newPath).ToList();
                    Console.WriteLine($"  → bin 目录共有 {files.Count} 个文件");

                    var codeFiles = files.Where(x =>
                        (x.Contains("code", StringComparison.OrdinalIgnoreCase) ||
                         x.Contains("codium", StringComparison.OrdinalIgnoreCase))
                        && !x.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase)).ToList();

                    Console.WriteLine($"  → 匹配的 VSCode 可执行文件 ({codeFiles.Count}):");
                    foreach (var file in codeFiles)
                    {
                        var fileName = Path.GetFileName(file);
                        var fileInfo = File.Exists(file) ? new FileInfo(file) : null;
                        var size = fileInfo?.Length ?? 0;
                        Console.WriteLine($"      • {fileName} ({size:N0} bytes)");
                    }
                }
                else
                {
                    Console.WriteLine($"  ✗ bin 目录不存在");
                }
            }
            else
            {
                Console.WriteLine($"  ✗ 目录不存在");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"【步骤 3】调用 VSCodeInstances.LoadVSCodeInstances()");
        Console.WriteLine("─────────────────────────────────────────────────────");

        try
        {
            VSCodeInstances.LoadVSCodeInstances();

            Console.WriteLine($"✓ 检测完成，找到 {VSCodeInstances.Instances.Count} 个 VSCode 实例");
            Console.WriteLine();

            if (VSCodeInstances.Instances.Count > 0)
            {
                for (int i = 0; i < VSCodeInstances.Instances.Count; i++)
                {
                    var instance = VSCodeInstances.Instances[i];
                    Console.WriteLine($"【实例 {i + 1}】");
                    Console.WriteLine($"  版本类型: {instance.VSCodeVersion}");
                    Console.WriteLine($"  可执行文件: {instance.ExecutablePath}");
                    Console.WriteLine($"  AppData: {instance.AppData}");

                    // 验证文件并获取版本信息
                    if (File.Exists(instance.ExecutablePath))
                    {
                        try
                        {
                            var fileInfo = FileVersionInfo.GetVersionInfo(instance.ExecutablePath);
                            Console.WriteLine($"  文件版本: {fileInfo.FileVersion ?? "未知"}");
                            Console.WriteLine($"  产品版本: {fileInfo.ProductVersion ?? "未知"}");
                            Console.WriteLine($"  文件大小: {new FileInfo(instance.ExecutablePath).Length:N0} bytes");
                            Console.WriteLine($"  修改时间: {File.GetLastWriteTime(instance.ExecutablePath):yyyy-MM-dd HH:mm:ss}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"  ⚠ 无法获取版本信息: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"  ✗ 警告: 可执行文件不存在!");
                    }

                    Console.WriteLine($"  工作区图标: {(instance.WorkspaceIconBitMap != null ? "已生成" : "未生成")}");
                    Console.WriteLine($"  远程图标: {(instance.RemoteIconBitMap != null ? "已生成" : "未生成")}");
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
                Console.WriteLine("║  ⚠  警告: 未找到任何 VSCode 实例!                  ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
                Console.WriteLine();
                Console.WriteLine("可能的原因:");
                Console.WriteLine("  1. PATH 环境变量中不包含 VSCode 路径");
                Console.WriteLine("  2. VSCode 的可执行文件名不符合预期 (code.exe, code-insiders.exe 等)");
                Console.WriteLine("  3. 目录结构不是标准的 bin 子目录结构");
                Console.WriteLine("  4. VSCode 未正确安装或安装位置不在标准路径");
                Console.WriteLine();
                Console.WriteLine("建议检查:");
                Console.WriteLine("  • 运行 'where code' 命令查看 VSCode 安装位置");
                Console.WriteLine("  • 确认 VSCode 已正确添加到 PATH 环境变量");
                Console.WriteLine("  • 尝试重新安装 VSCode");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 发生异常: {ex.Message}");
            Console.WriteLine($"  {ex.StackTrace}");
        }

        Console.WriteLine();
        Console.WriteLine("─────────────────────────────────────────────────────");
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
}
