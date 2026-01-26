// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Flow.Plugin.VSCodeWorkspaces.SshConfigParser;
using Flow.Plugin.VSCodeWorkspaces.VSCodeHelper;

namespace Flow.Plugin.VSCodeWorkspaces.RemoteMachinesHelper
{
    public class VSCodeRemoteMachinesApi
    {
        public VSCodeRemoteMachinesApi()
        {
        }

        public List<VSCodeRemoteMachine> Machines
        {
            get
            {
                var results = new List<VSCodeRemoteMachine>();
                var totalMachines = 0;

                // 使用 HashSet 避免重复处理同一个 SSH 配置文件
                var processedSshConfigFiles = new HashSet<string>();

                foreach (var vscodeInstance in VSCodeInstances.Instances)
                {
                    var instanceName = vscodeInstance.VSCodeVersion.ToString();
                    // settings.json contains path of ssh_config
                    var vscode_settings = Path.Combine(vscodeInstance.AppData, "User", "settings.json");

                    if (File.Exists(vscode_settings))
                    {
                        var fileContent = File.ReadAllText(vscode_settings);

                        try
                        {
                            JsonElement vscodeSettingsFile = JsonSerializer.Deserialize<JsonElement>(fileContent, new JsonSerializerOptions
                            {
                                AllowTrailingCommas = true,
                                ReadCommentHandling = JsonCommentHandling.Skip,
                            });
                            if (vscodeSettingsFile.TryGetProperty("remote.SSH.configFile", out var pathElement))
                            {
                                var path = pathElement.GetString();

                                if (File.Exists(path))
                                {
                                    // 避免重复读取同一个文件
                                    if (!processedSshConfigFiles.Contains(path))
                                    {
                                        processedSshConfigFiles.Add(path);
                                        var instanceMachinesCount = 0;
                                        foreach (SshHost h in SshConfig.ParseFile(path))
                                        {
                                            var machine = new VSCodeRemoteMachine();
                                            machine.Host = h.Host;
                                            machine.VSCodeInstance = vscodeInstance;
                                            machine.HostName = h.HostName != null ? h.HostName : string.Empty;
                                            machine.User = h.User != null ? h.User : string.Empty;

                                            results.Add(machine);
                                            instanceMachinesCount++;
                                        }

                                        if (instanceMachinesCount > 0)
                                        {
                                            totalMachines += instanceMachinesCount;
                                            Main.Context.API.LogInfo("VSCodeRemoteMachines",
                                                $"[{instanceName}] 从 SSH 配置文件 ({path}) 读取了 {instanceMachinesCount} 个远程机器");
                                        }
                                    }
                                }
                                else
                                {
                                    Main.Context.API.LogInfo("VSCodeRemoteMachines",
                                        $"[{instanceName}] SSH 配置文件不存在: {path}");
                                }
                            }
                            else
                            {
                                Main.Context.API.LogInfo("VSCodeRemoteMachines",
                                    $"[{instanceName}] settings.json 中未找到 remote.SSH.configFile 配置，尝试使用默认路径");
                            }
                        }
                        catch (Exception ex)
                        {
                            var message = $"Failed to deserialize ${vscode_settings}";
                            Main.Context.API.LogException("VSCodeWorkSpaces", message, ex);
                        }
                    }
                    else
                    {
                        Main.Context.API.LogInfo("VSCodeRemoteMachines",
                            $"[{instanceName}] settings.json 不存在: {vscode_settings}");
                    }
                }

                // 如果没有找到任何配置的 SSH 配置文件，尝试默认路径
                if (processedSshConfigFiles.Count == 0)
                {
                    var defaultSshConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", "config");
                    Main.Context.API.LogInfo("VSCodeRemoteMachines",
                        $"未找到任何配置的 SSH 配置文件，尝试默认路径: {defaultSshConfigPath}");

                    if (File.Exists(defaultSshConfigPath))
                    {
                        processedSshConfigFiles.Add(defaultSshConfigPath);
                        var defaultMachinesCount = 0;
                        foreach (SshHost h in SshConfig.ParseFile(defaultSshConfigPath))
                        {
                            var machine = new VSCodeRemoteMachine();
                            machine.Host = h.Host;
                            machine.VSCodeInstance = VSCodeInstances.Instances.FirstOrDefault(); // 使用第一个可用的 VSCode 实例
                            machine.HostName = h.HostName != null ? h.HostName : string.Empty;
                            machine.User = h.User != null ? h.User : string.Empty;

                            results.Add(machine);
                            defaultMachinesCount++;
                        }

                        if (defaultMachinesCount > 0)
                        {
                            totalMachines += defaultMachinesCount;
                            Main.Context.API.LogInfo("VSCodeRemoteMachines",
                                $"从默认 SSH 配置文件 ({defaultSshConfigPath}) 读取了 {defaultMachinesCount} 个远程机器");
                        }
                    }
                    else
                    {
                        Main.Context.API.LogInfo("VSCodeRemoteMachines",
                            $"默认 SSH 配置文件不存在: {defaultSshConfigPath}");
                    }
                }

                if (totalMachines > 0)
                {
                    Main.Context.API.LogInfo("VSCodeRemoteMachines",
                        $"总共从 {VSCodeInstances.Instances.Count} 个 VSCode 实例读取了 {totalMachines} 个远程机器");
                }

                return results;
            }
        }
    }
}