namespace SchoolsManagement.Api.Configuration;

public sealed class DatabaseConfigState(bool isConfigured, string connectionString)
{
    public bool IsConfigured { get; } = isConfigured;
    public string ConnectionString { get; } = connectionString;
}
