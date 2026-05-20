using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HorizonXIBackupApp.Models;

public sealed class AppState
{
    [JsonPropertyName("addons")]
    public Dictionary<string, AddonState> Addons { get; set; } = new();
}

public sealed class AddonState
{
    [JsonPropertyName("sha")]
    public string Sha { get; set; } = "";

    [JsonPropertyName("checked_at")]
    public string CheckedAt { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
}
