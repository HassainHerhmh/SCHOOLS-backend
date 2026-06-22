using Microsoft.EntityFrameworkCore;
using SchoolsManagement.Api.Configuration;

namespace SchoolsManagement.Api.Data;

public static class BusAppTablesBootstrap
{
    private const string SqlServerSql = """
IF OBJECT_ID(N'dbo.bus_driver_locations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.bus_driver_locations (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_bus_driver_locations PRIMARY KEY,
        driver_id uniqueidentifier NOT NULL,
        latitude float NOT NULL,
        longitude float NOT NULL,
        speed_kmh float NULL,
        heading float NULL,
        recorded_at datetimeoffset(7) NOT NULL CONSTRAINT DF_bus_driver_locations_recorded DEFAULT (sysdatetimeoffset())
    );
    CREATE INDEX IX_bus_driver_locations_driver ON dbo.bus_driver_locations(driver_id, recorded_at DESC);
END

IF OBJECT_ID(N'dbo.bus_app_drivers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.bus_app_drivers (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_bus_app_drivers PRIMARY KEY,
        school_id nvarchar(120) NULL,
        full_name nvarchar(500) NOT NULL,
        phone_number nvarchar(40) NOT NULL,
        username nvarchar(120) NOT NULL,
        password nvarchar(500) NOT NULL,
        synced_at datetimeoffset(7) NOT NULL CONSTRAINT DF_bus_app_drivers_synced DEFAULT (sysdatetimeoffset())
    );
    CREATE UNIQUE INDEX UX_bus_app_drivers_username ON dbo.bus_app_drivers(username);
END

IF OBJECT_ID(N'dbo.bus_app_students', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.bus_app_students (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_bus_app_students PRIMARY KEY,
        driver_id uniqueidentifier NOT NULL,
        school_id nvarchar(120) NULL,
        name nvarchar(500) NOT NULL,
        parent_phone nvarchar(40) NULL,
        level nvarchar(200) NOT NULL,
        section nvarchar(200) NOT NULL,
        bus_site_name nvarchar(300) NULL,
        synced_at datetimeoffset(7) NOT NULL CONSTRAINT DF_bus_app_students_synced DEFAULT (sysdatetimeoffset())
    );
    CREATE INDEX IX_bus_app_students_driver ON dbo.bus_app_students(driver_id);
END

        IF OBJECT_ID(N'dbo.bus_app_locations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.bus_app_locations (
        driver_id uniqueidentifier NOT NULL CONSTRAINT PK_bus_app_locations PRIMARY KEY,
        school_id nvarchar(120) NULL,
        latitude float NOT NULL,
        longitude float NOT NULL,
        speed_kmh float NULL,
        heading float NULL,
        recorded_at datetimeoffset(7) NOT NULL CONSTRAINT DF_bus_app_locations_recorded DEFAULT (sysdatetimeoffset())
    );
END

IF OBJECT_ID(N'dbo.bus_school_settings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.bus_school_settings (
        id int NOT NULL CONSTRAINT PK_bus_school_settings PRIMARY KEY,
        location_url nvarchar(2000) NULL,
        updated_at datetimeoffset(7) NOT NULL CONSTRAINT DF_bus_school_settings_updated DEFAULT (sysdatetimeoffset())
    );
    INSERT INTO dbo.bus_school_settings (id, location_url, updated_at) VALUES (1, NULL, sysdatetimeoffset());
END
""";

    private const string SqlServerAlterSql = """
IF COL_LENGTH(N'dbo.bus_app_students', N'bus_location_url') IS NULL
BEGIN
    ALTER TABLE dbo.bus_app_students ADD bus_location_url nvarchar(2000) NULL;
END
""";

    private const string MySqlAlterSql =
        "ALTER TABLE bus_app_students ADD COLUMN bus_location_url varchar(2000) NULL";

    private static void EnsureMySqlBusLocationColumn(ApplicationDbContext db)
    {
        var exists = db.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*) AS `Value`
                FROM information_schema.columns
                WHERE table_schema = DATABASE()
                  AND table_name = 'bus_app_students'
                  AND column_name = 'bus_location_url'
                """)
            .FirstOrDefault() > 0;

        if (!exists)
        {
            db.Database.ExecuteSqlRaw(MySqlAlterSql);
        }
    }

    private static async Task EnsureMySqlBusLocationColumnAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        var exists = await db.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*) AS `Value`
                FROM information_schema.columns
                WHERE table_schema = DATABASE()
                  AND table_name = 'bus_app_students'
                  AND column_name = 'bus_location_url'
                """)
            .FirstOrDefaultAsync(cancellationToken) > 0;

        if (!exists)
        {
            await db.Database.ExecuteSqlRawAsync(MySqlAlterSql, cancellationToken);
        }
    }

    public static void EnsureExists(ApplicationDbContext db)
    {
        if (DatabaseProviderHelper.IsMySql(db))
        {
            EnsureMySqlBusTables(db);
            return;
        }

        db.Database.ExecuteSqlRaw(SqlServerSql);
        db.Database.ExecuteSqlRaw(SqlServerAlterSql);
    }

    public static async Task EnsureExistsAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        if (DatabaseProviderHelper.IsMySql(db))
        {
            await EnsureMySqlBusTablesAsync(db, cancellationToken);
            return;
        }

        await db.Database.ExecuteSqlRawAsync(SqlServerSql, cancellationToken);
        await db.Database.ExecuteSqlRawAsync(SqlServerAlterSql, cancellationToken);
    }

    private const string MySqlSql = """
CREATE TABLE IF NOT EXISTS bus_driver_locations (
    Id char(36) NOT NULL PRIMARY KEY,
    driver_id char(36) NOT NULL,
    latitude double NOT NULL,
    longitude double NOT NULL,
    speed_kmh double NULL,
    heading double NULL,
    recorded_at datetime(6) NOT NULL,
    INDEX IX_bus_driver_locations_driver (driver_id, recorded_at)
);

CREATE TABLE IF NOT EXISTS bus_app_drivers (
    Id char(36) NOT NULL PRIMARY KEY,
    school_id varchar(120) NULL,
    full_name varchar(500) NOT NULL,
    phone_number varchar(40) NOT NULL,
    username varchar(120) NOT NULL,
    password varchar(500) NOT NULL,
    synced_at datetime(6) NOT NULL,
    UNIQUE KEY UX_bus_app_drivers_username (username)
);

CREATE TABLE IF NOT EXISTS bus_app_students (
    Id char(36) NOT NULL PRIMARY KEY,
    driver_id char(36) NOT NULL,
    school_id varchar(120) NULL,
    name varchar(500) NOT NULL,
    parent_phone varchar(40) NULL,
    level varchar(200) NOT NULL,
    section varchar(200) NOT NULL,
    bus_site_name varchar(300) NULL,
    bus_location_url varchar(2000) NULL,
    synced_at datetime(6) NOT NULL,
    INDEX IX_bus_app_students_driver (driver_id)
);

CREATE TABLE IF NOT EXISTS bus_app_locations (
    driver_id char(36) NOT NULL PRIMARY KEY,
    school_id varchar(120) NULL,
    latitude double NOT NULL,
    longitude double NOT NULL,
    speed_kmh double NULL,
    heading double NULL,
    recorded_at datetime(6) NOT NULL
);

CREATE TABLE IF NOT EXISTS bus_school_settings (
    id int NOT NULL PRIMARY KEY,
    location_url varchar(2000) NULL,
    updated_at datetime(6) NOT NULL
);
""";

    private static void EnsureMySqlBusTables(ApplicationDbContext db)
    {
        foreach (var statement in MySqlSql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (statement.Length == 0)
            {
                continue;
            }

            db.Database.ExecuteSqlRaw(statement);
        }

        EnsureMySqlBusLocationColumn(db);
        EnsureMySqlSchoolSettingsRow(db);
    }

    public static async Task EnsureMySqlBusTablesAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        foreach (var statement in MySqlSql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (statement.Length == 0)
            {
                continue;
            }

            await db.Database.ExecuteSqlRawAsync(statement, cancellationToken);
        }

        await EnsureMySqlBusLocationColumnAsync(db, cancellationToken);
        await EnsureMySqlSchoolSettingsRowAsync(db, cancellationToken);
    }

    private static void EnsureMySqlSchoolSettingsRow(ApplicationDbContext db)
    {
        var count = db.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*) AS `Value` FROM bus_school_settings WHERE id = 1")
            .FirstOrDefault();
        if (count <= 0)
        {
            db.Database.ExecuteSqlRaw(
                "INSERT INTO bus_school_settings (id, location_url, updated_at) VALUES (1, NULL, UTC_TIMESTAMP(6))");
        }
    }

    private static async Task EnsureMySqlSchoolSettingsRowAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        var count = await db.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*) AS `Value` FROM bus_school_settings WHERE id = 1")
            .FirstOrDefaultAsync(cancellationToken);
        if (count <= 0)
        {
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO bus_school_settings (id, location_url, updated_at) VALUES (1, NULL, UTC_TIMESTAMP(6))",
                cancellationToken);
        }
    }
}
