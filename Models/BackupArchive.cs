using System;

namespace HorizonXIBackupApp.Models;

public sealed class BackupArchive
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime Modified { get; set; }

    public double SizeMB => Math.Round(SizeBytes / 1024.0 / 1024.0, 2);
    public string SizeDisplay => $"{SizeMB:N1} MB";
    public string ModifiedDisplay => Modified.ToString("yyyy-MM-dd HH:mm");
}
