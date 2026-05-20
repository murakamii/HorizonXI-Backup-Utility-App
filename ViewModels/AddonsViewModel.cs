using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HorizonXIBackupApp.Models;
using HorizonXIBackupApp.Services;

namespace HorizonXIBackupApp.ViewModels;

public sealed class AddonRowViewModel
{
    public string Name { get; set; } = "";
    public string Installed { get; set; } = "";
    public string Ignored { get; set; } = "";
    public string Source { get; set; } = "";
    public string Status { get; set; } = "";
    public string Sha { get; set; } = "";
    public string Date { get; set; } = "";
    public string Url { get; set; } = "";
    public string CommitUrl { get; set; } = "";
}

public partial class AddonsViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _root;

    public ObservableCollection<AddonRowViewModel> Rows { get; } = new();

    [ObservableProperty] private string _statusMessage = "";

    public AddonsViewModel(MainWindowViewModel root)
    {
        _root = root;
        Refresh();
    }

    public void Refresh()
    {
        Rows.Clear();
        if (!_root.IsInstallValid())
        {
            StatusMessage = "Install path is not configured. Set it in Settings.";
            return;
        }

        var loaded = DefaultTxtParser.Parse(_root.Config.InstallRoot);
        var registry = _root.RegistryService.Load();
        var ignoreSet = new System.Collections.Generic.HashSet<string>(
            _root.Config.IgnoreAddons, System.StringComparer.OrdinalIgnoreCase);
        var customUrls = new System.Collections.Generic.Dictionary<string, string>(
            _root.Config.Updates.CustomUrls, System.StringComparer.OrdinalIgnoreCase);

        var addonsDir = System.IO.Path.Combine(_root.Config.InstallRoot, "addons");
        var installedFolders = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        if (System.IO.Directory.Exists(addonsDir))
        {
            foreach (var d in System.IO.Directory.GetDirectories(addonsDir))
                installedFolders.Add(System.IO.Path.GetFileName(d));
        }

        var state = _root.ConfigService.LoadState();

        foreach (var name in loaded.Addons)
        {
            string url = "";
            string regLabel;
            string source;
            string displayName = name;

            if (customUrls.TryGetValue(name, out var custom))
            {
                url = custom;
                regLabel = "(custom_urls)";
                source = GitHubClient.ParseUrl(url) is not null ? "github" : "other";
            }
            else
            {
                var entry = _root.RegistryService.Resolve(name, registry);
                if (entry is not null)
                {
                    url = entry.Url;
                    regLabel = entry.Name;
                    displayName = entry.Name;
                    source = GitHubClient.ParseUrl(url) is not null ? "github" : "other";
                }
                else
                {
                    regLabel = "(not in registry)";
                    source = "-";
                }
            }

            state.Addons.TryGetValue(displayName, out var s);
            var sha = s?.Sha;
            var dateStr = s?.CheckedAt;

            Rows.Add(new AddonRowViewModel
            {
                Name = name,
                Installed = installedFolders.Contains(name) ? "yes" : "NO",
                Ignored = ignoreSet.Contains(name) ? "yes" : "",
                Source = source,
                Status = regLabel,
                Sha = sha is null ? "" : sha.Substring(0, System.Math.Min(7, sha.Length)),
                Date = dateStr ?? "",
                Url = url,
                CommitUrl = ""
            });
        }
        StatusMessage = $"{Rows.Count} addon(s) loaded from default.txt.";
    }

    public void PopulateFromSummary(UpdateCheckSummary summary)
    {
        // Annotate existing rows with the latest commit info
        foreach (var result in summary.Results)
        {
            foreach (var row in Rows)
            {
                if (string.Equals(row.Name, result.Name, System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(row.Status, result.Name, System.StringComparison.OrdinalIgnoreCase))
                {
                    row.Status = result.Status switch
                    {
                        UpdateStatus.New => "UPDATE AVAILABLE",
                        UpdateStatus.FirstSeen => "baseline recorded",
                        UpdateStatus.Unchanged => "up to date",
                        UpdateStatus.Error => $"error: {result.Detail}",
                        UpdateStatus.Skipped => "non-GitHub",
                        UpdateStatus.Unknown => "no URL known",
                        _ => row.Status
                    };
                    row.Sha = result.Sha ?? row.Sha;
                    row.Date = result.Date ?? row.Date;
                    row.CommitUrl = result.CommitUrl ?? "";
                    break;
                }
            }
        }
        StatusMessage = $"{summary.NewCount} updates, {summary.UnchangedCount} up-to-date, {summary.FirstSeenCount} baselined.";
    }
}
