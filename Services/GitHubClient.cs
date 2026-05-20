using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HorizonXIBackupApp.Models;

namespace HorizonXIBackupApp.Services;

public sealed class GitHubClient
{
    private static readonly Regex UrlRegex = new(
        @"^https?://github\.com/([^/]+)/([^/]+)(?:/(tree|blob)/([^/]+)(?:/(.*))?)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly HttpClient _http;

    public GitHubClient()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("HorizonXIBackupApp/0.1");
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    public void SetToken(string? token)
    {
        _http.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    public static GitHubRef? ParseUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var trimmed = url.TrimEnd('/');
        var m = UrlRegex.Match(trimmed);
        if (!m.Success) return null;

        var r = new GitHubRef
        {
            Owner = m.Groups[1].Value,
            Repo = m.Groups[2].Value,
            Kind = "root"
        };
        if (m.Groups[3].Success)
        {
            r.Kind = m.Groups[3].Value.ToLowerInvariant();
            r.Branch = m.Groups[4].Value;
            r.Path = m.Groups[5].Success ? m.Groups[5].Value : null;
        }
        return r;
    }

    public async Task<(GitHubCommit? Commit, string? Error, int? StatusCode)> GetLatestCommitAsync(
        GitHubRef gref, CancellationToken ct = default)
    {
        var query = new List<string> { "per_page=1" };
        if (!string.IsNullOrEmpty(gref.Branch)) query.Add("sha=" + Uri.EscapeDataString(gref.Branch));
        if (!string.IsNullOrEmpty(gref.Path)) query.Add("path=" + Uri.EscapeDataString(gref.Path!));

        var url = $"https://api.github.com/repos/{gref.Owner}/{gref.Repo}/commits?{string.Join("&", query)}";
        HttpResponseMessage? resp = null;
        try
        {
            resp = await _http.GetAsync(url, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return (null, resp.ReasonPhrase ?? "request failed", (int)resp.StatusCode);
            }
            var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
            {
                return (null, "no commits returned", (int)resp.StatusCode);
            }
            var first = doc.RootElement[0];
            var sha = first.GetProperty("sha").GetString() ?? "";
            var commitObj = first.GetProperty("commit");
            var msg = commitObj.GetProperty("message").GetString() ?? "";
            var firstLine = msg.Split('\n')[0];
            var date = commitObj.GetProperty("author").GetProperty("date").GetString() ?? "";
            var htmlUrl = first.GetProperty("html_url").GetString() ?? "";

            return (new GitHubCommit
            {
                Sha = sha,
                Message = firstLine,
                Date = date,
                Url = htmlUrl
            }, null, (int)resp.StatusCode);
        }
        catch (Exception ex)
        {
            return (null, ex.Message, null);
        }
        finally
        {
            resp?.Dispose();
        }
    }
}
