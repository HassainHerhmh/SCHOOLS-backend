namespace SchoolsManagement.Api.Configuration;

public sealed class DatabaseConfigState(bool isConfigured, string connectionString, bool isMySql)
{
    public bool IsConfigured { get; } = isConfigured;
    public string ConnectionString { get; } = connectionString;
    public bool IsMySql { get; } = isMySql;
}
