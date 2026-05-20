using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HorizonXIBackupApp.Models;

namespace HorizonXIBackupApp.Services;

public sealed class BackupResult
{
    public string ArchivePath { get; set; } = "";
    public int FileCount { get; set; }
    public long ByteCount { get; set; }
    public long ArchiveBytes { get; set; }
    public int Retained { get; set; }
    public int Removed { get; set; }
    public List<string> MissingAddons { get; set; } = new();
}

public sealed class BackupService
{
    public IReadOnlyList<BackupArchive> ListBackups(string destination)
    {
        if (!Directory.Exists(destination)) return Array.Empty<BackupArchive>();
        return new DirectoryInfo(destination)
            .GetFiles("hxbackup_*.zip")
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .Select(f => new BackupArchive
            {
                Name = f.Name,
                FullPath = f.FullName,
                SizeBytes = f.Length,
                Modified = f.LastWriteTime
            })
            .ToList();
    }

    public async Task<BackupResult> CreateBackupAsync(
        AppConfig config,
        string? label = null,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var installRoot = config.InstallRoot;
        if (!Directory.Exists(installRoot))
            throw new DirectoryNotFoundException($"Install root not found: {installRoot}");

        var dest = config.Backup.Destination;
        if (string.IsNullOrWhiteSpace(dest)) dest = AppPaths.DefaultBackupDestination;
        Directory.CreateDirectory(dest);

        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var suffix = string.IsNullOrWhiteSpace(label) ? "" : "_" + Sanitize(label);
        var archiveName = $"hxbackup_{timestamp}{suffix}.zip";
        var archivePath = Path.Combine(dest, archiveName);

        var stagingRoot = Path.Combine(Path.GetTempPath(), $"hxapp_stage_{timestamp}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingRoot);

        var missingAddons = new List<string>();

        try
        {
            var paths = new List<string>(config.Backup.Paths);

            var ignoreSet = new HashSet<string>(
                config.IgnoreAddons.Select(a => a.ToLowerInvariant()),
                StringComparer.OrdinalIgnoreCase);

            if (config.Backup.IncludeLoadedAddons)
            {
                var loaded = DefaultTxtParser.Parse(installRoot);
                var addonsDir = Path.Combine(installRoot, "addons");
                Dictionary<string, string> diskFolders = new(StringComparer.OrdinalIgnoreCase);
                if (Directory.Exists(addonsDir))
                {
                    foreach (var d in Directory.GetDirectories(addonsDir))
                        diskFolders[Path.GetFileName(d)] = Path.GetFileName(d);
                }

                foreach (var name in loaded.Addons)
                {
                    if (ignoreSet.Contains(name)) continue;
                    if (diskFolders.TryGetValue(name, out var actual))
                    {
                        paths.Add(Path.Combine("addons", actual));
                    }
                    else
                    {
                        missingAddons.Add(name);
                    }
                }
            }

            var excludeRegexes = config.Backup.ExcludeGlobs
                .Where(g => !string.IsNullOrEmpty(g))
                .Select(GlobToRegex)
                .ToList();

            long totalBytes = 0;
            int totalFiles = 0;

            foreach (var rel in paths)
            {
                ct.ThrowIfCancellationRequested();
                var src = Path.Combine(installRoot, rel);
                if (!File.Exists(src) && !Directory.Exists(src))
                {
                    progress?.Report($"(skipped, missing) {rel}");
                    continue;
                }

                var stagedTarget = Path.Combine(stagingRoot, rel);
                var stagedParent = Path.GetDirectoryName(stagedTarget);
                if (!string.IsNullOrEmpty(stagedParent)) Directory.CreateDirectory(stagedParent);

                if (Directory.Exists(src))
                {
                    var (files, bytes) = await CopyDirectoryAsync(src, stagedTarget, excludeRegexes, ct).ConfigureAwait(false);
                    totalFiles += files;
                    totalBytes += bytes;
                    progress?.Report($"staged {rel} ({files} files, {bytes / 1024} KB)");
                }
                else
                {
                    File.Copy(src, stagedTarget, overwrite: true);
                    var fi = new FileInfo(stagedTarget);
                    totalFiles += 1;
                    totalBytes += fi.Length;
                    progress?.Report($"staged {rel} ({fi.Length / 1024} KB)");
                }
            }

            // Write manifest at staging root
            var manifest = new
            {
                timestamp = DateTime.UtcNow.ToString("o"),
                install_root = installRoot,
                paths = paths.ToArray(),
                file_count = totalFiles,
                byte_count = totalBytes,
                missing_addons = missingAddons,
                tool_version = "0.1.0"
            };
            File.WriteAllText(Path.Combine(stagingRoot, "MANIFEST.json"),
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

            progress?.Report($"Compressing {totalFiles} files ({totalBytes / 1024 / 1024} MB)...");
            if (File.Exists(archivePath)) File.Delete(archivePath);
            ZipFile.CreateFromDirectory(stagingRoot, archivePath, CompressionLevel.Optimal, includeBaseDirectory: false);

            var archiveBytes = new FileInfo(archivePath).Length;
            var (kept, removed) = Rotate(dest, config.Backup.Retention);

            return new BackupResult
            {
                ArchivePath = archivePath,
                FileCount = totalFiles,
                ByteCount = totalBytes,
                ArchiveBytes = archiveBytes,
                Retained = kept,
                Removed = removed,
                MissingAddons = missingAddons
            };
        }
        finally
        {
            try { if (Directory.Exists(stagingRoot)) Directory.Delete(stagingRoot, recursive: true); } catch { }
        }
    }

    public void DeleteBackup(string archivePath)
    {
        if (File.Exists(archivePath)) File.Delete(archivePath);
    }

    private static Task<(int FileCount, long ByteCount)> CopyDirectoryAsync(
        string src, string dest, List<Regex> excludes, CancellationToken ct)
    {
        return Task.Run<(int, long)>(() =>
        {
            int files = 0;
            long bytes = 0;
            CopyRecursive(new DirectoryInfo(src), dest, excludes, ct, ref files, ref bytes);
            return (files, bytes);
        }, ct);
    }

    private static void CopyRecursive(DirectoryInfo srcDir, string destDir, List<Regex> excludes,
        CancellationToken ct, ref int files, ref long bytes)
    {
        ct.ThrowIfCancellationRequested();
        Directory.CreateDirectory(destDir);
        foreach (var f in srcDir.GetFiles())
        {
            if (excludes.Any(r => r.IsMatch(f.Name))) continue;
            var target = Path.Combine(destDir, f.Name);
            f.CopyTo(target, overwrite: true);
            files += 1;
            bytes += f.Length;
        }
        foreach (var d in srcDir.GetDirectories())
        {
            CopyRecursive(d, Path.Combine(destDir, d.Name), excludes, ct, ref files, ref bytes);
        }
    }

    private static (int Kept, int Removed) Rotate(string destination, int retention)
    {
        var archives = new DirectoryInfo(destination)
            .GetFiles("hxbackup_*.zip")
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .ToList();
        if (retention <= 0) return (archives.Count, 0);
        int kept = Math.Min(retention, archives.Count);
        int removed = 0;
        for (int i = retention; i < archives.Count; i++)
        {
            try { archives[i].Delete(); removed++; } catch { }
        }
        return (kept, removed);
    }

    private static Regex GlobToRegex(string glob)
    {
        var sb = new System.Text.StringBuilder("^");
        foreach (var ch in glob)
        {
            sb.Append(ch switch
            {
                '*' => ".*",
                '?' => ".",
                '.' or '(' or ')' or '+' or '|' or '^' or '$' or '@' or '%' or '{' or '}' or '[' or ']' or '\\' => Regex.Escape(ch.ToString()),
                _ => ch.ToString()
            });
        }
        sb.Append('$');
        return new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    private static string Sanitize(string s)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(s.Where(c => !invalid.Contains(c) && c != ' ').ToArray());
    }
}
