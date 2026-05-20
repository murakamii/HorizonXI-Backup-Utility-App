using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HorizonXIBackupApp.Models;

namespace HorizonXIBackupApp.ViewModels;

public partial class BackupsViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _root;

    public ObservableCollection<BackupArchive> Backups { get; } = new();

    [ObservableProperty] private BackupArchive? _selectedBackup;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _busyMessage = "";

    public BackupsViewModel(MainWindowViewModel root)
    {
        _root = root;
        Refresh();
    }

    public void Refresh()
    {
        Backups.Clear();
        var items = _root.BackupService.ListBackups(_root.Config.Backup.Destination);
        foreach (var b in items) Backups.Add(b);
        StatusMessage = $"{Backups.Count} snapshot(s).";
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    public async Task RestoreAsync()
    {
        if (SelectedBackup is null) return;
        IsBusy = true;
        BusyMessage = "Restoring...";
        try
        {
            var progress = new Progress<string>(s => BusyMessage = s);
            var result = await Task.Run(() => _root.RestoreService.RestoreAsync(SelectedBackup, _root.Config, progress: progress));
            StatusMessage = $"Restored {result.RestoredEntries} files. Safety snapshot: {System.IO.Path.GetFileName(result.SafetyBackupPath)}";
            Refresh();
            _root.Dashboard.RefreshDisplay();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Restore failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = "";
        }
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    public void DeleteSelected()
    {
        if (SelectedBackup is null) return;
        try
        {
            _root.BackupService.DeleteBackup(SelectedBackup.FullPath);
            StatusMessage = $"Deleted {SelectedBackup.Name}.";
            Refresh();
            _root.Dashboard.RefreshDisplay();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete failed: {ex.Message}";
        }
    }

    [RelayCommand]
    public void OpenFolder()
    {
        try
        {
            var dest = _root.Config.Backup.Destination;
            if (!System.IO.Directory.Exists(dest)) System.IO.Directory.CreateDirectory(dest);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = dest,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open folder: {ex.Message}";
        }
    }

    private bool CanAct() => SelectedBackup is not null && !IsBusy;

    partial void OnSelectedBackupChanged(BackupArchive? value)
    {
        RestoreCommand.NotifyCanExecuteChanged();
        DeleteSelectedCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        RestoreCommand.NotifyCanExecuteChanged();
        DeleteSelectedCommand.NotifyCanExecuteChanged();
    }
}
