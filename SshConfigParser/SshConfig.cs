// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Flow.Plugin.VSCodeWorkspaces.SshConfigParser
{
    public class SshConfig
    {
        private static readonly Regex _sshConfig = new Regex(@"^(\w[\s\S]*?\w)$(?=(?:\s+^\w|\z))", RegexOptions.Multiline);
        private static readonly Regex _keyValue = new Regex(@"(\w+\s\S+)", RegexOptions.Multiline);

        public static IEnumerable<SshHost> ParseFile(string path)
        {
            return Parse(File.ReadAllText(path));
        }

        public static IEnumerable<SshHost> Parse(string str)
        {
            str = str.Replace('\r', '\0');
            var list = new List<SshHost>();
            foreach (Match match in _sshConfig.Matches(str))
            {
                // 跳过不成功的匹配
                if (!match.Success)
                {
                    continue;
                }

                var sshHost = new SshHost();
                // 使用 Groups[1] 获取第一个捕获组，如果不存在则使用整个匹配内容
                var groupsList = match.Groups.Values.ToList();
                string content = groupsList.Count > 0 ? groupsList[0].Value : match.Value;

                foreach (Match match1 in _keyValue.Matches(content))
                {
                    var split = match1.Value.Split(" ");
                    if (split.Length >= 2)
                    {
                        var key = split[0];
                        var value = split[1];
                        sshHost.Properties[key] = value;
                    }
                }

                // 只有当 Host 属性存在时才添加到列表
                if (!string.IsNullOrEmpty(sshHost.Host))
                {
                    list.Add(sshHost);
                }
            }

            return list;
        }
    }
}
