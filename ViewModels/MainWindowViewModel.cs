using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using HorizonXIBackupApp.Models;
using HorizonXIBackupApp.Services;

namespace HorizonXIBackupApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ConfigService ConfigService { get; }
    public RegistryService RegistryService { get; }
    public BackupService BackupService { get; }
    public RestoreService RestoreService { get; }
    public UpdateCheckerService UpdateChecker { get; }
    public NotificationService Notifier { get; }

    [ObservableProperty] private AppConfig _config;

    public DashboardViewModel Dashboard { get; }
    public AddonsViewModel Addons { get; }
    public BackupsViewModel Backups { get; }
    public SettingsViewModel Settings { get; }

    public string Title => "HorizonXI Backup Utility";

    public MainWindowViewModel()
    {
        ConfigService = new ConfigService();
        RegistryService = new RegistryService();
        BackupService = new BackupService();
        RestoreService = new RestoreService(BackupService);
        var github = new GitHubClient();
        UpdateChecker = new UpdateCheckerService(ConfigService, RegistryService, github);
        Notifier = new NotificationService();

        _config = ConfigService.Load();
        // Persist with defaults filled in (first-run case)
        ConfigService.Save(_config);

        Dashboard = new DashboardViewModel(this);
        Addons = new AddonsViewModel(this);
        Backups = new BackupsViewModel(this);
        Settings = new SettingsViewModel(this);
    }

    public void SaveConfig() => ConfigService.Save(Config);

    public bool IsInstallValid()
    {
        return Directory.Exists(Config.InstallRoot)
            && File.Exists(Path.Combine(Config.InstallRoot, "scripts", "default.txt"));
    }
}
