using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HorizonXIBackupApp.Models;

namespace HorizonXIBackupApp.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _root;

    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private string _installPathDisplay = "";
    [ObservableProperty] private bool _isInstallValid;
    [ObservableProperty] private string _lastBackupDisplay = "(no backups yet)";
    [ObservableProperty] private string _updateSummaryDisplay = "(not checked)";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _busyMessage = "";

    public DashboardViewModel(MainWindowViewModel root)
    {
        _root = root;
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        InstallPathDisplay = _root.Config.InstallRoot;
        IsInstallValid = _root.IsInstallValid();

        var backups = _root.BackupService.ListBackups(_root.Config.Backup.Destination);
        if (backups.Count == 0)
        {
            LastBackupDisplay = "No backups yet.";
        }
        else
        {
            var last = backups[0];
            var ago = (DateTime.Now - last.Modified);
            string relative = ago.TotalDays > 1
                ? $"{(int)ago.TotalDays}d ago"
                : ago.TotalHours > 1 ? $"{(int)ago.TotalHours}h ago"
                : ago.TotalMinutes > 1 ? $"{(int)ago.TotalMinutes}m ago"
                : "just now";
            LastBackupDisplay = $"{relative} - {last.SizeDisplay} ({backups.Count} snapshots kept)";
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    public async Task BackupNowAsync()
    {
        IsBusy = true;
        BusyMessage = "Backing up...";
        StatusMessage = "";
        try
        {
            var progress = new Progress<string>(s => BusyMessage = s);
            var result = await Task.Run(() => _root.BackupService.CreateBackupAsync(_root.Config, label: null, progress: progress));
            StatusMessage = $"Backup complete: {System.IO.Path.GetFileName(result.ArchivePath)} ({result.ArchiveBytes / 1024.0 / 1024.0:N1} MB).";
            _root.Backups.Refresh();
            RefreshDisplay();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Backup failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = "";
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    public async Task CheckUpdatesAsync()
    {
        IsBusy = true;
        BusyMessage = "Checking updates...";
        StatusMessage = "";
        try
        {
            var progress = new Progress<string>(s => BusyMessage = s);
            var summary = await Task.Run(() => _root.UpdateChecker.CheckAsync(_root.Config, progress: progress));
            var parts = new System.Collections.Generic.List<string>();
            if (summary.NewCount > 0) parts.Add($"{summary.NewCount} updated");
            if (summary.FirstSeenCount > 0) parts.Add($"{summary.FirstSeenCount} baselined");
            if (summary.UnchangedCount > 0) parts.Add($"{summary.UnchangedCount} up to date");
            if (summary.UnknownCount > 0) parts.Add($"{summary.UnknownCount} unknown");
            if (summary.ErrorCount > 0) parts.Add($"{summary.ErrorCount} errors");
            UpdateSummaryDisplay = string.Join(" - ", parts);
            StatusMessage = summary.NewCount > 0
                ? $"{summary.NewCount} addon(s) have updates. Check the Addons tab."
                : "All loaded addons are up to date.";

            _root.Addons.PopulateFromSummary(summary);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Update check failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = "";
        }
    }

    private bool CanRun() => !IsBusy && IsInstallValid;

    partial void OnIsBusyChanged(bool value)
    {
        BackupNowCommand.NotifyCanExecuteChanged();
        CheckUpdatesCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsInstallValidChanged(bool value)
    {
        BackupNowCommand.NotifyCanExecuteChanged();
        CheckUpdatesCommand.NotifyCanExecuteChanged();
    }
}
