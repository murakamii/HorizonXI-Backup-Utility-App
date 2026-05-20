using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HorizonXIBackupApp.Models;

namespace HorizonXIBackupApp.Services;

public sealed class UpdateCheckerService
{
    private readonly ConfigService _configService;
    private readonly RegistryService _registryService;
    private readonly GitHubClient _github;

    public UpdateCheckerService(ConfigService configService, RegistryService registryService, GitHubClient github)
    {
        _configService = configService;
        _registryService = registryService;
        _github = github;
    }

    public async Task<UpdateCheckSummary> CheckAsync(
        AppConfig config,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        _github.SetToken(string.IsNullOrWhiteSpace(config.Updates.GithubToken) ? null : config.Updates.GithubToken);

        var summary = new UpdateCheckSummary();
        var registry = _registryService.Load();
        var state = _configService.LoadState();

        var loaded = DefaultTxtParser.Parse(config.InstallRoot);
        var allNames = loaded.Addons.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var ignoreSet = new HashSet<string>(
            config.IgnoreAddons.Where(s => !string.IsNullOrEmpty(s)),
            StringComparer.OrdinalIgnoreCase);

        var customUrls = new Dictionary<string, string>(config.Updates.CustomUrls, StringComparer.OrdinalIgnoreCase);

        var names = allNames.Where(n => !ignoreSet.Contains(n)).ToList();
        summary.IgnoredCount = allNames.Count - names.Count;

        foreach (var name in names)
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report($"Checking {name}...");

            string? url = null;
            string resolvedName = name;
            if (customUrls.TryGetValue(name, out var custom))
            {
                url = custom;
            }
            else
            {
                var entry = _registryService.Resolve(name, registry);
                if (entry is not null)
                {
                    url = entry.Url;
                    resolvedName = entry.Name;
                }
            }

            if (string.IsNullOrEmpty(url))
            {
                summary.Results.Add(new AddonCheckResult
                {
                    Name = name,
                    Status = UpdateStatus.Unknown,
                    Detail = "Not in registry. Add a custom URL in Settings."
                });
                summary.UnknownCount++;
                continue;
            }

            var gref = GitHubClient.ParseUrl(url);
            if (gref is null)
            {
                summary.Results.Add(new AddonCheckResult
                {
                    Name = resolvedName,
                    Url = url,
                    Status = UpdateStatus.Skipped,
                    Detail = $"Non-GitHub source: {url}"
                });
                summary.SkippedCount++;
                continue;
            }

            var (commit, error, status) = await _github.GetLatestCommitAsync(gref, ct).ConfigureAwait(false);
            if (commit is null)
            {
                summary.Results.Add(new AddonCheckResult
                {
                    Name = resolvedName,
                    Url = url,
                    Status = UpdateStatus.Error,
                    Detail = $"{status}: {error}"
                });
                summary.ErrorCount++;
                continue;
            }

            state.Addons.TryGetValue(resolvedName, out var prevState);
            var prevSha = prevState?.Sha;
            var isFirstSeen = string.IsNullOrEmpty(prevSha);
            var hasUpdate = !isFirstSeen && !string.Equals(prevSha, commit.Sha, StringComparison.Ordinal);

            var resultStatus = hasUpdate ? UpdateStatus.New
                              : isFirstSeen ? UpdateStatus.FirstSeen
                              : UpdateStatus.Unchanged;

            if (hasUpdate) summary.NewCount++;
            else if (isFirstSeen) summary.FirstSeenCount++;
            else summary.UnchangedCount++;

            summary.Results.Add(new AddonCheckResult
            {
                Name = resolvedName,
                Status = resultStatus,
                PrevSha = string.IsNullOrEmpty(prevSha) ? null : prevSha.Substring(0, Math.Min(7, prevSha.Length)),
                Sha = commit.ShortSha,
                Date = commit.Date,
                Message = commit.Message,
                CommitUrl = commit.Url,
                Url = url
            });

            state.Addons[resolvedName] = new AddonState
            {
                Sha = commit.Sha,
                CheckedAt = DateTime.UtcNow.ToString("o"),
                Url = url
            };
        }

        _configService.SaveState(state);
        return summary;
    }
}
