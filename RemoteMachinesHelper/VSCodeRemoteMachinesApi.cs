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
                                else
                                {
                                    Main.Context.API.LogInfo("VSCodeRemoteMachines",
                                        $"[{instanceName}] SSH 配置文件不存在: {path}");
                                }
                            }
                            else
                            {
                                Main.Context.API.LogInfo("VSCodeRemoteMachines",
                                    $"[{instanceName}] settings.json 中未找到 remote.SSH.configFile 配置");
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