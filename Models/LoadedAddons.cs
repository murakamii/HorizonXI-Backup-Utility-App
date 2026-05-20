using System.Collections.Generic;

namespace HorizonXIBackupApp.Models;

public sealed class LoadedAddons
{
    public List<string> Addons { get; set; } = new();
    public List<string> Plugins { get; set; } = new();
}
