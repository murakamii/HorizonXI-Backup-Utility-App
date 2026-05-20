using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HorizonXIBackupApp.Services;

namespace HorizonXIBackupApp.ViewModels;

public sealed class CustomUrlRow
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
}

public partial class SettingsViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _root;

    [ObservableProperty] private string _installRoot = "";
    [ObservableProperty] private string _backupDestination = "";
    [ObservableProperty] private int _retention;
    [ObservableProperty] private bool _includeLoadedAddons;
    [ObservableProperty] private string _ignoreAddonsText = "";
    [ObservableProperty] private string _githubToken = "";
    [ObservableProperty] private bool _notifyOnUpdate;
    [ObservableProperty] private string _theme = ThemeService.Dark;
    [ObservableProperty] private string _statusMessage = "";

    [ObservableProperty] private string _newCustomUrlName = "";
    [ObservableProperty] private string _newCustomUrlUrl = "";

    public string[] ThemeOptions => ThemeService.Options;

    public ObservableCollection<CustomUrlRow> CustomUrls { get; } = new();

    public SettingsViewModel(MainWindowViewModel root)
    {
        _root = root;
        LoadFromConfig();
    }

    public void LoadFromConfig()
    {
        InstallRoot = _root.Config.InstallRoot;
        BackupDestination = _root.Config.Backup.Destination;
        Retention = _root.Config.Backup.Retention;
        IncludeLoadedAddons = _root.Config.Backup.IncludeLoadedAddons;
        IgnoreAddonsText = string.Join(", ", _root.Config.IgnoreAddons);
        GithubToken = _root.Config.Updates.GithubToken;
        NotifyOnUpdate = _root.Config.Updates.NotifyOnUpdate;
        Theme = string.IsNullOrEmpty(_root.Config.Theme) ? ThemeService.Dark : _root.Config.Theme;

        CustomUrls.Clear();
        foreach (var kv in _root.Config.Updates.CustomUrls)
            CustomUrls.Add(new CustomUrlRow { Name = kv.Key, Url = kv.Value });
    }

    [RelayCommand]
    public void Save()
    {
        _root.Config.InstallRoot = InstallRoot.Trim();
        _root.Config.Backup.Destination = BackupDestination.Trim();
        _root.Config.Backup.Retention = Retention < 0 ? 0 : Retention;
        _root.Config.Backup.IncludeLoadedAddons = IncludeLoadedAddons;
        _root.Config.IgnoreAddons.Clear();
        foreach (var token in IgnoreAddonsText.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries))
        {
            _root.Config.IgnoreAddons.Add(token);
        }
        _root.Config.Updates.GithubToken = GithubToken.Trim();
        _root.Config.Updates.NotifyOnUpdate = NotifyOnUpdate;
        _root.Config.Theme = Theme;
        ThemeService.Apply(Theme);

        _root.Config.Updates.CustomUrls.Clear();
        foreach (var row in CustomUrls)
        {
            if (!string.IsNullOrWhiteSpace(row.Name) && !string.IsNullOrWhiteSpace(row.Url))
            {
                _root.Config.Updates.CustomUrls[row.Name.Trim()] = row.Url.Trim();
            }
        }

        _root.SaveConfig();
        _root.Dashboard.RefreshDisplay();
        _root.Addons.Refresh();
        _root.Backups.Refresh();
        StatusMessage = "Saved.";
    }

    [RelayCommand]
    public void AddCustomUrl()
    {
        if (string.IsNullOrWhiteSpace(NewCustomUrlName) || string.IsNullOrWhiteSpace(NewCustomUrlUrl))
        {
            StatusMessage = "Enter both a name and a URL.";
            return;
        }
        CustomUrls.Add(new CustomUrlRow { Name = NewCustomUrlName.Trim(), Url = NewCustomUrlUrl.Trim() });
        NewCustomUrlName = "";
        NewCustomUrlUrl = "";
        StatusMessage = "Added. Click Save to persist.";
    }

    [RelayCommand]
    public void RemoveCustomUrl(CustomUrlRow? row)
    {
        if (row is null) return;
        CustomUrls.Remove(row);
        StatusMessage = "Removed. Click Save to persist.";
    }

    [RelayCommand]
    public void TestNotification()
    {
        _root.Notifier.ShowTest();
        StatusMessage = "Test toast sent. Check your notification center if you missed it.";
    }

    [RelayCommand]
    public void DetectInstallPath()
    {
        var detected = InstallPathDetector.Detect();
        if (string.IsNullOrEmpty(detected))
        {
            StatusMessage = "Could not auto-detect a HorizonXI install in the usual locations.";
            return;
        }
        InstallRoot = detected;
        StatusMessage = $"Detected: {detected}. Click Save to apply.";
    }
}
