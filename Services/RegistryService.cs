using System;
using System.IO;
using System.Text.Json;
using Avalonia.Platform;
using HorizonXIBackupApp.Models;

namespace HorizonXIBackupApp.Services;

public sealed class RegistryService
{
    private AddonRegistry? _cached;

    public AddonRegistry Load()
    {
        if (_cached is not null) return _cached;

        var uri = new Uri("avares://HorizonXIBackupApp/Assets/addons.json");
        using var stream = AssetLoader.Open(uri);
        using var reader = new StreamReader(stream);
        var raw = reader.ReadToEnd();
        var reg = JsonSerializer.Deserialize<AddonRegistry>(raw, JsonStore.Options)
                  ?? new AddonRegistry();
        _cached = reg;
        return reg;
    }

    /// <summary>
    /// Case-insensitive name lookup, with a fallback that strips parenthetical
    /// qualifiers like "Chains (Sippius)" -> "Chains", and a final URL-tail match.
    /// </summary>
    public RegistryEntry? Resolve(string addonName, AddonRegistry? registry = null)
    {
        registry ??= Load();
        var target = addonName.ToLowerInvariant();

        foreach (var entry in registry.Addons)
        {
            if (string.Equals(entry.Name, addonName, StringComparison.OrdinalIgnoreCase))
                return entry;
        }

        foreach (var entry in registry.Addons)
        {
            var bare = System.Text.RegularExpressions.Regex
                .Replace(entry.Name, @"\s*\(.+?\)\s*$", "")
                .ToLowerInvariant();
            if (bare == target) return entry;
        }

        foreach (var entry in registry.Addons)
        {
            var tail = entry.Url.TrimEnd('/').Split('/')[^1].ToLowerInvariant();
            tail = System.Text.RegularExpressions.Regex.Replace(tail, @"\.(lua|dll)$", "");
            if (tail == target) return entry;
        }
        return null;
    }
}
