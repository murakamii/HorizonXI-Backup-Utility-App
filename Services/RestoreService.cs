using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using HorizonXIBackupApp.Models;

namespace HorizonXIBackupApp.Services;

public sealed class RestoreResult
{
    public string SafetyBackupPath { get; set; } = "";
    public int RestoredEntries { get; set; }
}

public sealed class RestoreService
{
    private readonly BackupService _backupService;

    public RestoreService(BackupService backupService)
    {
        _backupService = backupService;
    }

    /// <summary>
    /// Restore the contents of an archive over the configured install_root.
    /// Before restoring, creates a safety snapshot of the live install so the user
    /// can roll back. Files in the archive overwrite files in the install.
    /// Files NOT in the archive are left untouched (we do NOT wipe-and-restore).
    /// </summary>
    public async Task<RestoreResult> RestoreAsync(
        BackupArchive archive,
        AppConfig config,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        if (!File.Exists(archive.FullPath))
            throw new FileNotFoundException("Archive not found", archive.FullPath);
        if (!Directory.Exists(config.InstallRoot))
            throw new DirectoryNotFoundException($"Install root not found: {config.InstallRoot}");

        progress?.Report("Creating safety snapshot of current install...");
        var safety = await _backupService.CreateBackupAsync(config, label: "pre-restore", progress: progress, ct: ct)
            .ConfigureAwait(false);

        progress?.Report("Extracting archive over install...");
        int restored = await Task.Run(() =>
        {
            int count = 0;
            using var zip = ZipFile.OpenRead(archive.FullPath);
            foreach (var entry in zip.Entries)
            {
                ct.ThrowIfCancellationRequested();
                if (entry.FullName.EndsWith("/") || entry.Length == 0)
                {
                    continue;
                }
                // Skip the MANIFEST.json from the staging root - it's metadata.
                if (string.Equals(entry.FullName, "MANIFEST.json", StringComparison.OrdinalIgnoreCase))
                    continue;

                var targetPath = Path.Combine(config.InstallRoot, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
                var targetDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDir)) Directory.CreateDirectory(targetDir);
                entry.ExtractToFile(targetPath, overwrite: true);
                count++;
            }
            return count;
        }, ct).ConfigureAwait(false);

        progress?.Report($"Restored {restored} entries.");

        return new RestoreResult
        {
            SafetyBackupPath = safety.ArchivePath,
            RestoredEntries = restored
        };
    }
}
