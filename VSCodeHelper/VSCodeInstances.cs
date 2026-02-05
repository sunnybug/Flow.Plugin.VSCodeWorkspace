// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Windows.Media.Imaging;

namespace Flow.Plugin.VSCodeWorkspaces.VSCodeHelper
{
    public static class VSCodeInstances
    {
        private static string _systemPath = string.Empty;

        private static readonly string _userAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        public static List<VSCodeInstance> Instances { get; set; } = new();

        [SupportedOSPlatform("windows")]
        private static BitmapImage Bitmap2BitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        [SupportedOSPlatform("windows")]
        private static Bitmap BitmapOverlayToCenter(Bitmap bitmap1, Bitmap overlayBitmap)
        {
            int bitmap1Width = bitmap1.Width;
            int bitmap1Height = bitmap1.Height;

            Bitmap overlayBitmapResized = new Bitmap(overlayBitmap, new System.Drawing.Size(bitmap1Width / 2, bitmap1Height / 2));

            float marginLeft = (float)((bitmap1Width * 0.7) - (overlayBitmapResized.Width * 0.5));
            float marginTop = (float)((bitmap1Height * 0.7) - (overlayBitmapResized.Height * 0.5));

            Bitmap finalBitmap = new Bitmap(bitmap1Width, bitmap1Height);
            using (Graphics g = Graphics.FromImage(finalBitmap))
            {
                g.DrawImage(bitmap1, System.Drawing.Point.Empty);
                g.DrawImage(overlayBitmapResized, marginLeft, marginTop);
            }

            return finalBitmap;
        }

        /// <summary>
        /// 验证文件是否是真正的 VSCode 可执行文件
        /// 通过检查文件版本信息中的产品名称和公司名称来确认
        /// </summary>
        /// <param name="filePath">可执行文件路径</param>
        /// <returns>如果是 VSCode 返回 true，否则返回 false</returns>
        private static bool IsValidVSCodeExecutable(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                var fileVersionInfo = FileVersionInfo.GetVersionInfo(filePath);

                // 检查产品名称是否包含 "Visual Studio Code"
                var productName = fileVersionInfo.ProductName;
                if (string.IsNullOrEmpty(productName) ||
                    !productName.Contains("Visual Studio Code", StringComparison.OrdinalIgnoreCase))
                {
                    Main.Context?.API.LogWarn("VSCodeInstances",
                        $"文件 {filePath} 的产品名称不是 Visual Studio Code: {productName}");
                    return false;
                }

                // 可选：检查公司名称（应该是 Microsoft Corporation）
                var companyName = fileVersionInfo.CompanyName;
                if (!string.IsNullOrEmpty(companyName) &&
                    !companyName.Contains("Microsoft", StringComparison.OrdinalIgnoreCase))
                {
                    Main.Context?.API.LogWarn("VSCodeInstances",
                        $"文件 {filePath} 的公司名称不是 Microsoft: {companyName}");
                    return false;
                }

                Main.Context?.API.LogInfo("VSCodeInstances",
                    $"验证通过: {filePath} (产品: {productName}, 公司: {companyName})");

                return true;
            }
            catch (Exception ex)
            {
                Main.Context?.API.LogException("VSCodeInstances",
                    $"验证 VSCode 可执行文件时出错: {filePath}", ex);
                return false;
            }
        }

        // Gets the executablePath and AppData foreach instance of VSCode
        [SupportedOSPlatform("windows")]
        public static void LoadVSCodeInstances()
        {
            if (_systemPath == Environment.GetEnvironmentVariable("PATH"))
                return;


            Instances = new List<VSCodeInstance>();

            _systemPath = Environment.GetEnvironmentVariable("PATH") ?? "";

            Main.Context?.API.LogInfo("VSCodeInstances",
                $"开始扫描 VSCode 实例，PATH 环境变量长度: {_systemPath.Length}");

            var paths = _systemPath.Split(";").Where(x =>
                x.Contains("VS Code", StringComparison.OrdinalIgnoreCase) ||
                x.Contains("VisualStudioCode", StringComparison.OrdinalIgnoreCase) ||
                x.Contains("codium", StringComparison.OrdinalIgnoreCase) ||
                x.Contains("vscode", StringComparison.OrdinalIgnoreCase)).ToList();

            Main.Context?.API.LogInfo("VSCodeInstances",
                $"在 PATH 中找到 {paths.Count} 个 VSCode 相关路径");

            foreach (var path in paths)
            {
                if (!Directory.Exists(path))
                    continue;

                Main.Context?.API.LogInfo("VSCodeInstances", $"检查路径: {path}");

                // 确定要查找的可执行文件
                string codeExecutable = null;
                string iconPath = path;
                VSCodeVersion vscodeVersion = VSCodeVersion.Stable;
                string versionName = "Code";

                // 尝试在 bin 目录中查找（标准安装）
                var binPath = Path.GetFileName(path).Equals("bin", StringComparison.OrdinalIgnoreCase)
                    ? path
                    : Path.Combine(path, "bin");

                if (Directory.Exists(binPath))
                {
                    // 查找 Code.exe（Windows 主程序）
                    var parentCodeExe = Path.Combine(Path.GetDirectoryName(binPath) ?? path, "Code.exe");
                    if (File.Exists(parentCodeExe) && IsValidVSCodeExecutable(parentCodeExe))
                    {
                        codeExecutable = parentCodeExe;
                        iconPath = Path.GetDirectoryName(parentCodeExe) ?? path;
                        Main.Context?.API.LogInfo("VSCodeInstances",
                            $"在父目录找到 Code.exe: {codeExecutable}");
                    }
                }

                // 如果没找到，尝试直接在当前目录查找
                if (string.IsNullOrEmpty(codeExecutable))
                {
                    var possibleExecutables = new[]
                    {
                        Path.Combine(path, "Code.exe"),
                        Path.Combine(path, "code.exe"),
                        Path.Combine(path, "Code - Insiders.exe"),
                        Path.Combine(path, "code-insiders.exe"),
                        Path.Combine(path, "Code - Exploration.exe"),
                        Path.Combine(path, "code-exploration.exe"),
                        Path.Combine(path, "VSCodium.exe"),
                        Path.Combine(path, "codium.exe"),
                    };

                    foreach (var exe in possibleExecutables)
                    {
                        if (File.Exists(exe) && IsValidVSCodeExecutable(exe))
                        {
                            codeExecutable = exe;
                            iconPath = path;

                            // 确定版本类型
                            if (exe.Contains("Insiders", StringComparison.OrdinalIgnoreCase))
                            {
                                vscodeVersion = VSCodeVersion.Insiders;
                                versionName = "Code - Insiders";
                            }
                            else if (exe.Contains("Exploration", StringComparison.OrdinalIgnoreCase))
                            {
                                vscodeVersion = VSCodeVersion.Exploration;
                                versionName = "Code - Exploration";
                            }
                            else if (exe.Contains("codium", StringComparison.OrdinalIgnoreCase))
                            {
                                vscodeVersion = VSCodeVersion.Stable;
                                versionName = "VSCodium";
                            }

                            Main.Context?.API.LogInfo("VSCodeInstances",
                                $"找到可执行文件: {codeExecutable} (版本: {versionName})");
                            break;
                        }
                    }
                }

                // 如果还是没找到，尝试在 bin 目录中查找（便携版或其他变体）
                if (string.IsNullOrEmpty(codeExecutable) && Directory.Exists(binPath))
                {
                    var binFiles = Directory.EnumerateFiles(binPath, "*.exe", SearchOption.TopDirectoryOnly)
                        .Where(f => IsValidVSCodeExecutable(f))
                        .FirstOrDefault();

                    if (!string.IsNullOrEmpty(binFiles))
                    {
                        codeExecutable = binFiles;
                        iconPath = Path.GetDirectoryName(binPath) ?? path;
                        Main.Context?.API.LogInfo("VSCodeInstances",
                            $"在 bin 目录找到可执行文件: {codeExecutable}");
                    }
                }

                // 如果没找到有效的可执行文件，跳过
                if (string.IsNullOrEmpty(codeExecutable))
                {
                    Main.Context?.API.LogWarn("VSCodeInstances",
                        $"路径 {path} 未找到有效的 VSCode 可执行文件");
                    continue;
                }

                var instance = new VSCodeInstance
                {
                    ExecutablePath = codeExecutable,
                    VSCodeVersion = vscodeVersion,
                };

                var portableData = Path.Join(iconPath, "data");
                instance.AppData = Directory.Exists(portableData)
                    ? Path.Join(portableData, "user-data")
                    : Path.Combine(_userAppDataPath, versionName);

                Main.Context?.API.LogInfo("VSCodeInstances",
                    $"找到 VSCode 实例: 版本={versionName}, 可执行文件={codeExecutable}, AppData={instance.AppData}, 便携版={Directory.Exists(portableData)}");

                var iconVSCode = Path.Join(iconPath, $"{versionName}.exe");
                var bitmapIconVscode = Icon.ExtractAssociatedIcon(iconVSCode)?.ToBitmap();

                // workspace
                var folderIcon = (Bitmap)Image.FromFile(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "//Images//folder.png");
                instance.WorkspaceIconBitMap = Bitmap2BitmapImage(BitmapOverlayToCenter(folderIcon, bitmapIconVscode));

                // remote
                var monitorIcon = (Bitmap)Image.FromFile(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "//Images//monitor.png");

                instance.RemoteIconBitMap = Bitmap2BitmapImage(BitmapOverlayToCenter(monitorIcon, bitmapIconVscode));

                Instances.Add(instance);
            }

            Main.Context?.API.LogInfo("VSCodeInstances",
                $"VSCode 实例扫描完成，共找到 {Instances.Count} 个实例");
        }
    }
}