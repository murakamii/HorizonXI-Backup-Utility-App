using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HorizonXIBackupApp.Models;

public sealed class AppConfig
{
    [JsonPropertyName("install_root")]
    public string InstallRoot { get; set; } = @"C:\HorizonXI\Game";

    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "Dark";

    [JsonPropertyName("ignore_addons")]
    public List<string> IgnoreAddons { get; set; } = new() { "dynamic_entity_renamer" };

    [JsonPropertyName("backup")]
    public BackupConfig Backup { get; set; } = new();

    [JsonPropertyName("updates")]
    public UpdatesConfig Updates { get; set; } = new();
}

public sealed class BackupConfig
{
    [JsonPropertyName("destination")]
    public string Destination { get; set; } = "";

    [JsonPropertyName("retention")]
    public int Retention { get; set; } = 14;

    [JsonPropertyName("paths")]
    public List<string> Paths { get; set; } = new() { "config", @"scripts\default.txt" };

    [JsonPropertyName("include_loaded_addons")]
    public bool IncludeLoadedAddons { get; set; } = true;

    [JsonPropertyName("exclude_globs")]
    public List<string> ExcludeGlobs { get; set; } = new() { "*.log", "*.tmp" };
}

public sealed class UpdatesConfig
{
    [JsonPropertyName("github_token")]
    public string GithubToken { get; set; } = "";

    [JsonPropertyName("custom_urls")]
    public Dictionary<string, string> CustomUrls { get; set; } = new();

    [JsonPropertyName("notify_on_update")]
    public bool NotifyOnUpdate { get; set; } = true;
}
