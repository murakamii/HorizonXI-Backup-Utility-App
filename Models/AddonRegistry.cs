using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HorizonXIBackupApp.Models;

public sealed class AddonRegistry
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = "";

    [JsonPropertyName("last_synced")]
    public string LastSynced { get; set; } = "";

    [JsonPropertyName("addons")]
    public List<RegistryEntry> Addons { get; set; } = new();
}

public sealed class RegistryEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "active";
}
