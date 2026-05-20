using System;
using System.IO;

namespace HorizonXIBackupApp.Services;

public static class AppPaths
{
    public const string AppDataFolderName = "HorizonXIBackupApp";

    public static string AppDataRoot
    {
        get
        {
            var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(roaming, AppDataFolderName);
        }
    }

    public static string ConfigPath => Path.Combine(AppDataRoot, "config.json");
    public static string StatePath => Path.Combine(AppDataRoot, "state.json");

    public static string DefaultBackupDestination
    {
        get
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(docs, "HorizonXI Backups");
        }
    }

    public static void EnsureAppDataExists()
    {
        Directory.CreateDirectory(AppDataRoot);
    }
}
