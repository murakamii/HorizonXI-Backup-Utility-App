namespace HorizonXIBackupApp.Models;

public sealed class GitHubRef
{
    public string Owner { get; set; } = "";
    public string Repo { get; set; } = "";
    public string? Branch { get; set; }
    public string? Path { get; set; }
    public string Kind { get; set; } = "root";
}

public sealed class GitHubCommit
{
    public string Sha { get; set; } = "";
    public string ShortSha => Sha.Length >= 7 ? Sha.Substring(0, 7) : Sha;
    public string Message { get; set; } = "";
    public string Date { get; set; } = "";
    public string Url { get; set; } = "";
}
