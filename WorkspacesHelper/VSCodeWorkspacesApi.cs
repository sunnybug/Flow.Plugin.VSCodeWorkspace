// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Flow.Plugin.VSCodeWorkspaces.VSCodeHelper;
using JetBrains.Annotations;
using Microsoft.Data.Sqlite;

namespace Flow.Plugin.VSCodeWorkspaces.WorkspacesHelper
{
    public class VSCodeWorkspacesApi
    {
        public VSCodeWorkspacesApi()
        {
        }

        public static VsCodeWorkspace ParseVSCodeUri(string uri, VSCodeInstance vscodeInstance)
        {
            if (uri is not null)
            {
                var unescapeUri = Uri.UnescapeDataString(uri);
                var typeWorkspace = WorkspacesHelper.ParseVSCodeUri.GetTypeWorkspace(unescapeUri);
                if (!typeWorkspace.workspaceLocation.HasValue) return null;
                var folderName = Path.GetFileName(unescapeUri);

                // Check we haven't returned '' if we have a path like C:\
                if (string.IsNullOrEmpty(folderName))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(unescapeUri);
                    folderName = dirInfo.Name.TrimEnd(':');
                }

                return new VsCodeWorkspace()
                {
                    Path = unescapeUri,
                    RelativePath = typeWorkspace.Path,
                    FolderName = folderName,
                    ExtraInfo = typeWorkspace.MachineName,
                    WorkspaceLocation = typeWorkspace.workspaceLocation.Value,
                    VSCodeInstance = vscodeInstance,
                };
            }

            return null;
        }

        public readonly Regex WorkspaceLabelParser = new Regex("(.+?)(\\[.+\\])");

        public List<VsCodeWorkspace> Workspaces
        {
            get
            {
                var results = new List<VsCodeWorkspace>();
                var totalWorkspaces = 0;

                Main.Context.API.LogInfo("VSCodeWorkspaceApi",
                    $"开始扫描工作区，找到 {VSCodeInstances.Instances.Count} 个 VSCode 实例");

                foreach (var vscodeInstance in VSCodeInstances.Instances)
                {
                    var instanceCount = 0;
                    var instanceName = vscodeInstance.VSCodeVersion.ToString();

                    Main.Context.API.LogInfo("VSCodeWorkspaceApi",
                        $"[{instanceName}] 处理实例: 可执行文件={vscodeInstance.ExecutablePath}, AppData={vscodeInstance.AppData}");

                    // storage.json contains opened Workspaces
                    var vscodeStorage = Path.Combine(vscodeInstance.AppData, "storage.json");

                    if (File.Exists(vscodeStorage))
                    {
                        var fileContent = File.ReadAllText(vscodeStorage);

                        try
                        {
                            var vscodeStorageFile = JsonSerializer.Deserialize<VSCodeStorageFile>(fileContent);

                            if (vscodeStorageFile != null)
                            {
                                // for previous versions of vscode
                                if (vscodeStorageFile.OpenedPathsList?.Workspaces3 != null)
                                {
                                    var workspaces3Count = vscodeStorageFile.OpenedPathsList.Workspaces3
                                        .Select(workspaceUri => ParseVSCodeUri(workspaceUri, vscodeInstance))
                                        .Where(uri => uri != null)
                                        .Select(uri => (VsCodeWorkspace)uri).Count();

                                    results.AddRange(
                                        vscodeStorageFile.OpenedPathsList.Workspaces3
                                            .Select(workspaceUri => ParseVSCodeUri(workspaceUri, vscodeInstance))
                                            .Where(uri => uri != null)
                                            .Select(uri => (VsCodeWorkspace)uri));

                                    instanceCount += workspaces3Count;
                                    Main.Context.API.LogInfo("VSCodeWorkspaceApi",
                                        $"[{instanceName}] 从 storage.json (Workspaces3) 读取了 {workspaces3Count} 个工作区");
                                }

                                // vscode v1.55.0 or later
                                if (vscodeStorageFile.OpenedPathsList?.Entries != null)
                                {
                                    var entriesCount = vscodeStorageFile.OpenedPathsList.Entries
                                        .Select(x => x.FolderUri)
                                        .Select(workspaceUri => ParseVSCodeUri(workspaceUri, vscodeInstance))
                                        .Where(uri => uri != null).Count();

                                    results.AddRange(vscodeStorageFile.OpenedPathsList.Entries
                                        .Select(x => x.FolderUri)
                                        .Select(workspaceUri => ParseVSCodeUri(workspaceUri, vscodeInstance))
                                        .Where(uri => uri != null));

                                    instanceCount += entriesCount;
                                    Main.Context.API.LogInfo("VSCodeWorkspaceApi",
                                        $"[{instanceName}] 从 storage.json (Entries) 读取了 {entriesCount} 个工作区");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var message = $"Failed to deserialize ${vscodeStorage}";
                            Main.Context.API.LogException("VSCodeWorkspaceApi", message, ex);
                        }
                    }

                    // for vscode v1.64.0 or later
                    var stateDbPath = Path.Combine(vscodeInstance.AppData, "User", "globalStorage", "state.vscdb");

                    Main.Context.API.LogInfo("VSCodeWorkspaceApi",
                        $"[{instanceName}] 检查 state.vscdb 路径: {stateDbPath}, 存在={File.Exists(stateDbPath)}");

                    if (!File.Exists(stateDbPath))
                    {
                        if (instanceCount > 0)
                        {
                            Main.Context.API.LogInfo("VSCodeWorkspaceApi",
                                $"[{instanceName}] 共读取了 {instanceCount} 个工作区");
                        }
                        continue;
                    }

                    try
                    {
                        using var connection = new SqliteConnection(
                            $"Data Source={stateDbPath};mode=readonly;cache=shared;");
                        connection.Open();
                        var command = connection.CreateCommand();
                        command.CommandText = "SELECT value FROM ItemTable where key = 'history.recentlyOpenedPathsList'";
                        var result = command.ExecuteScalar();

                        Main.Context.API.LogInfo("VSCodeWorkspaceApi",
                            $"[{instanceName}] 数据库查询结果: {(result != null ? "成功" : "null")}");

                        if (result != null)
                        {
                            var dbCount = 0;
                            using var historyDoc = JsonDocument.Parse(result.ToString()!);
                            var root = historyDoc.RootElement;

                            Main.Context.API.LogInfo("VSCodeWorkspaceApi",
                                $"[{instanceName}] JSON 根属性: {string.Join(", ", root.EnumerateObject().Select(x => x.Name))}");

                            if (!root.TryGetProperty("entries", out var entries))
                            {
                                Main.Context.API.LogInfo("VSCodeWorkspaceApi",
                                    $"[{instanceName}] 警告: JSON 中未找到 'entries' 属性");
                                continue;
                            }

                            Main.Context.API.LogInfo("VSCodeWorkspaceApi",
                                $"[{instanceName}] entries 数组长度: {entries.GetArrayLength()}");
                            foreach (var entry in entries.EnumerateArray())
                            {
                                if (entry.TryGetProperty("folderUri", out var folderUri) &&
                                    ParseFolderEntry(folderUri, vscodeInstance, entry) is { } folderWorkspace)
                                {
                                    results.Add(folderWorkspace);
                                    dbCount++;
                                }
                                else if (entry.TryGetProperty("workspace", out var workspaceInfo) &&
                                         ParseWorkspaceEntry(workspaceInfo, vscodeInstance, entry) is { } workspace)
                                {
                                    results.Add(workspace);
                                    dbCount++;
                                }
                            }

                            if (dbCount > 0)
                            {
                                instanceCount += dbCount;
                                Main.Context.API.LogInfo("VSCodeWorkspaceApi",
                                    $"[{instanceName}] 从 state.vscdb 读取了 {dbCount} 个工作区");
                            }
                        }

                        if (instanceCount > 0)
                        {
                            Main.Context.API.LogInfo("VSCodeWorkspaceApi",
                                $"[{instanceName}] 共读取了 {instanceCount} 个工作区");
                        }
                    }
                    catch (Exception ex)
                    {
                        var message = $"Failed to read {stateDbPath}";
                        Main.Context.API.LogException("VSCodeWorkspaceApi", message, ex);
                    }

                    totalWorkspaces += instanceCount;
                }

                if (totalWorkspaces > 0)
                {
                    Main.Context.API.LogInfo("VSCodeWorkspaceApi",
                        $"总共从 {VSCodeInstances.Instances.Count} 个 VSCode 实例读取了 {totalWorkspaces} 个工作区");
                }

                return results;
            }
        }

        [CanBeNull]
        private VsCodeWorkspace ParseWorkspaceEntry(JsonElement workspaceInfo, VSCodeInstance vscodeInstance,
            JsonElement entry)
        {
            if (workspaceInfo.TryGetProperty("configPath", out var configPath))
            {
                var workspace = ParseVSCodeUri(configPath.GetString(), vscodeInstance);
                if (workspace == null)
                    return null;

                if (entry.TryGetProperty("label", out var label))
                {
                    var labelString = label.GetString()!;
                    var matchGroup = WorkspaceLabelParser.Match(labelString);
                    workspace = workspace with
                    {
                        Label = $"{matchGroup.Groups[2]} {matchGroup.Groups[1]}",
                        WorkspaceType = WorkspaceType.Workspace
                    };
                }

                return workspace;
            }

            return null;
        }


        [CanBeNull]
        private VsCodeWorkspace ParseFolderEntry(JsonElement folderUri, VSCodeInstance vscodeInstance,
            JsonElement entry)
        {
            var workspaceUri = folderUri.GetString();
            var workspace = ParseVSCodeUri(workspaceUri, vscodeInstance);
            if (workspace == null)
                return null;

            if (entry.TryGetProperty("label", out var label))
            {
                var labelString = label.GetString()!;
                var matchGroup = WorkspaceLabelParser.Match(labelString);
                workspace = workspace with
                {
                    Label = $"{matchGroup.Groups[2]} {matchGroup.Groups[1]}",
                    WorkspaceType = WorkspaceType.Folder
                };
            }

            return workspace;
        }
    }
}