using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using HorizonXIBackupApp.Models;

namespace HorizonXIBackupApp.Services;

public static class DefaultTxtParser
{
    private static readonly Regex AddonLoadRegex =
        new(@"^/addon\s+load\s+(\S+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PluginLoadRegex =
        new(@"^/load\s+(\S+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static LoadedAddons Parse(string installRoot)
    {
        var result = new LoadedAddons();
        var path = Path.Combine(installRoot, "scripts", "default.txt");
        if (!File.Exists(path)) return result;

        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            var m = AddonLoadRegex.Match(line);
            if (m.Success)
            {
                result.Addons.Add(m.Groups[1].Value);
                continue;
            }

            m = PluginLoadRegex.Match(line);
            if (m.Success && m.Groups[1].Value != "addons")
            {
                result.Plugins.Add(m.Groups[1].Value);
            }
        }
        return result;
    }
}
