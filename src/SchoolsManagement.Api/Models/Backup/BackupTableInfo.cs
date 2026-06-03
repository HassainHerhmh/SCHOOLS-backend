namespace SchoolsManagement.Api.Models.Backup;

public sealed class BackupColumnInfo
{
    public string Key { get; init; } = string.Empty;
    public string LabelAr { get; init; } = string.Empty;
}

public sealed class BackupTableInfo
{
    public string Key { get; init; } = string.Empty;
    public string LabelAr { get; init; } = string.Empty;
    public IReadOnlyList<BackupColumnInfo> Columns { get; init; } = Array.Empty<BackupColumnInfo>();
}
