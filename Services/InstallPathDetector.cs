using System;
using System.Collections.Generic;
using System.IO;

namespace HorizonXIBackupApp.Services;

public static class InstallPathDetector
{
    /// <summary>
    /// Scan common locations for a HorizonXI install and return the first that
    /// contains scripts\default.txt. Returns null if none are found.
    /// </summary>
    public static string? Detect()
    {
        foreach (var candidate in Candidates())
        {
            if (IsValidInstall(candidate)) return candidate;
        }
        return null;
    }

    public static bool IsValidInstall(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (!Directory.Exists(path)) return false;
        var scriptsPath = Path.Combine(path, "scripts", "default.txt");
        return File.Exists(scriptsPath);
    }

    private static IEnumerable<string> Candidates()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        var roots = new[]
        {
            @"C:\HorizonXI",
            @"D:\HorizonXI",
            @"E:\HorizonXI",
            Path.Combine(programFiles, "HorizonXI"),
            Path.Combine(programFilesX86, "HorizonXI"),
            Path.Combine(userProfile, "HorizonXI"),
            Path.Combine(localAppData, "HorizonXI"),
            Path.Combine(docs, "HorizonXI"),
        };

        foreach (var r in roots)
        {
            yield return Path.Combine(r, "Game");
            yield return r;
        }
    }
}
