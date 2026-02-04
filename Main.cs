// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Flow.Plugin.VSCodeWorkspaces
{
    using Flow.Launcher.Plugin;
    using Properties;
    using RemoteMachinesHelper;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Controls;
    using VSCodeHelper;
    using WorkspacesHelper;

    public class Main : IPlugin, IPluginI18n, ISettingProvider, IContextMenu
    {
        internal static PluginInitContext Context { get; private set; }

        private static Settings _settings;

        public string Name => GetTranslatedPluginTitle();

        public string Description => GetTranslatedPluginDescription();

        private VSCodeInstance _defaultInstance;

        private readonly VSCodeWorkspacesApi _workspacesApi = new();

        private readonly VSCodeRemoteMachinesApi _machinesApi = new();

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();
            var workspaces = new List<VsCodeWorkspace>();

            // User defined extra workspaces
            var customWorkspaceCount = 0;
            if (_defaultInstance != null)
            {
                var customWorkspaces = _settings.CustomWorkspaces.Select(uri =>
                    VSCodeWorkspacesApi.ParseVSCodeUri(uri, _defaultInstance)).ToList();
                customWorkspaceCount = customWorkspaces.Count(w => w != null);
                workspaces.AddRange(customWorkspaces);
            }

            // Search opened workspaces
            var discoveredWorkspaceCount = 0;
            if (_settings.DiscoverWorkspaces)
            {
                var discoveredWorkspaces = _workspacesApi.Workspaces;
                discoveredWorkspaceCount = discoveredWorkspaces.Count;
                workspaces.AddRange(discoveredWorkspaces);
            }

            // Simple de-duplication
            var distinctWorkspaces = workspaces.Distinct().ToList();
            results.AddRange(distinctWorkspaces.Select(CreateWorkspaceResult));

            // 输出汇总日志
            Context.API.LogInfo("VSCodeWorkspaces",
                $"数据汇总: 自定义工作区={customWorkspaceCount}, 发现的工作区={discoveredWorkspaceCount}, " +
                $"去重后={distinctWorkspaces.Count}, 远程机器={(_settings.DiscoverMachines ? _machinesApi.Machines.Count : 0)}");

            // Search opened remote machines
            if (_settings.DiscoverMachines)
            {
                _machinesApi.Machines.ForEach(a =>
                {
                    var title = $"SSH: {a.Host}";

                    if (!string.IsNullOrEmpty(a.User) && !string.IsNullOrEmpty(a.HostName))
                    {
                        title += $" [{a.User}@{a.HostName}]";
                    }

                    var tooltip = Resources.SSHRemoteMachine;

                    results.Add(new Result
                    {
                        Title = title,
                        SubTitle = Resources.SSHRemoteMachine,
                        Icon = a.VSCodeInstance.RemoteIcon,
                        TitleToolTip = tooltip,
                        Action = c =>
                        {
                            bool hide;
                            try
                            {
                                var process = new ProcessStartInfo
                                {
                                    FileName = a.VSCodeInstance.ExecutablePath,
                                    UseShellExecute = true,
                                    Arguments =
                                        $"--new-window --enable-proposed-api ms-vscode-remote.remote-ssh --remote ssh-remote+{((char)34) + a.Host + ((char)34)}",
                                    WindowStyle = ProcessWindowStyle.Hidden,
                                };
                                Process.Start(process);

                                hide = true;
                            }
                            catch (Win32Exception)
                            {
                                var name = $"{Context.CurrentPluginMetadata.Name}";
                                string msg = Resources.OpenFail;
                                Context.API.ShowMsg(name, msg, string.Empty);
                                hide = false;
                            }

                            return hide;
                        },
                        ContextData = a,
                    });
                });
            }

            if (query.ActionKeyword == string.Empty ||
                (query.ActionKeyword != string.Empty && query.Search != string.Empty))
            {
                results = results.Where(r =>
                {
                    var matchResult = Context.API.FuzzySearch(query.Search, r.Title);
                    r.Score = matchResult.Score;
                    // 当模糊搜索得分为 0 时，用子串匹配兜底（例如 "kr1" 匹配 "43.128.131.41-proxy-kr1"）
                    if (r.Score == 0 && !string.IsNullOrWhiteSpace(query.Search) &&
                        r.Title.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
                    {
                        r.Score = 1;
                    }
                    return r.Score > 0;
                }).ToList();
            }


            return results;
        }

        private static Result CreateWorkspaceResult(VsCodeWorkspace ws)
        {
            var title = $"{ws.FolderName}";
            var typeWorkspace = ws.WorkspaceTypeToString();

            if (ws.WorkspaceLocation != WorkspaceLocation.Local)
            {
                title = ws.Label != null
                    ? $"{ws.Label}"
                    : $"{title}{(ws.ExtraInfo != null ? $" - {ws.ExtraInfo}" : string.Empty)} ({typeWorkspace})";
            }

            var tooltip =
                $"{Resources.Workspace}{(ws.WorkspaceLocation != WorkspaceLocation.Local ? $" {Resources.In} {typeWorkspace}" : string.Empty)}: {SystemPath.RealPath(ws.RelativePath)}";

            return new Result
            {
                Title = title,
                SubTitle = tooltip,
                Icon = ws.VSCodeInstance.WorkspaceIcon,
                TitleToolTip = tooltip,
                Action = c =>
                {
                    try
                    {
                        var modifierKeys = c.SpecialKeyState.ToModifierKeys();
                        if (modifierKeys == System.Windows.Input.ModifierKeys.Control)
                        {
                            Context.API.OpenDirectory(SystemPath.RealPath(ws.RelativePath));
                            return true;
                        }

                        var argumentPrefix = ws.WorkspaceType == WorkspaceType.Workspace
                            ? "--file-uri"
                            : "--folder-uri";

                        var arguments = $"{argumentPrefix} \"{ws.Path}\"";

                        var process = new ProcessStartInfo
                        {
                            FileName = ws.VSCodeInstance.ExecutablePath,
                            UseShellExecute = true,
                            Arguments = arguments,
                            WindowStyle = ProcessWindowStyle.Hidden,
                        };

                        Process.Start(process);
                        return true;
                    }
                    catch (Win32Exception)
                    {
                        var name = $"{Context.CurrentPluginMetadata.Name}";
                        string msg = Resources.OpenFail;
                        Context.API.ShowMsg(name, msg, string.Empty);
                    }

                    return false;
                },
                ContextData = ws,
            };
        }

        public void Init(PluginInitContext context)
        {
            Context = context;
            _settings = context.API.LoadSettingJsonStorage<Settings>();

            VSCodeInstances.LoadVSCodeInstances();

            // Prefer stable version, or the first one we got
            _defaultInstance = VSCodeInstances.Instances.Find(e => e.VSCodeVersion == VSCodeVersion.Stable) ??
                              VSCodeInstances.Instances.FirstOrDefault();
        }

        public Control CreateSettingPanel() => new SettingsView(Context, _settings);

        public void OnCultureInfoChanged(CultureInfo newCulture)
        {
            Resources.Culture = newCulture;
        }

        public string GetTranslatedPluginTitle()
        {
            return Resources.PluginTitle;
        }

        public string GetTranslatedPluginDescription()
        {
            return Resources.PluginDescription;
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            List<Result> results = new();
            if (selectedResult.ContextData is VsCodeWorkspace ws && ws.WorkspaceLocation == WorkspaceLocation.Local)
            {
                results.Add(new Result
                {
                    Title = Resources.OpenFolder,
                    SubTitle = Resources.OpenFolderSubTitle,
                    Icon = ws.VSCodeInstance.WorkspaceIcon,
                    TitleToolTip = Resources.OpenFolderSubTitle,
                    Action = c =>
                    {
                        Context.API.OpenDirectory(SystemPath.RealPath(ws.RelativePath));
                        return true;
                    },
                    ContextData = ws,
                });
            }

            return results;
        }
    }
}