using HorizonXIBackupApp.Models;

namespace HorizonXIBackupApp.Services;

public sealed class ConfigService
{
    public AppConfig Load()
    {
        AppPaths.EnsureAppDataExists();
        var cfg = JsonStore.LoadOrDefault(AppPaths.ConfigPath, new AppConfig());
        if (cfg is null) cfg = new AppConfig();
        if (string.IsNullOrWhiteSpace(cfg.Backup.Destination))
        {
            cfg.Backup.Destination = AppPaths.DefaultBackupDestination;
        }
        return cfg;
    }

    public void Save(AppConfig config) => JsonStore.Save(AppPaths.ConfigPath, config);

    public AppState LoadState()
    {
        AppPaths.EnsureAppDataExists();
        var state = JsonStore.LoadOrDefault(AppPaths.StatePath, new AppState());
        return state ?? new AppState();
    }

    public void SaveState(AppState state) => JsonStore.Save(AppPaths.StatePath, state);
}
