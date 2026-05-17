-- جداول الباصات على SQL Server (نفس هجرة EF BusUsersAndBusSites)
-- تنفيذ حسب الحاجة أو استخدم: dotnet ef database update من مشروع الـ API.

IF OBJECT_ID(N'dbo.bus_users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.bus_users (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_bus_users PRIMARY KEY,
        full_name NVARCHAR(500) NOT NULL,
        phone_number NVARCHAR(40) NOT NULL,
        username NVARCHAR(120) NOT NULL,
        password NVARCHAR(500) NOT NULL,
        created_at DATETIMEOFFSET(7) NULL
    );
    CREATE UNIQUE INDEX IX_bus_users_username ON dbo.bus_users(username);
END;

IF OBJECT_ID(N'dbo.bus_sites', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.bus_sites (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_bus_sites PRIMARY KEY,
        site_name NVARCHAR(500) NOT NULL,
        fee_amount DECIMAL(14, 2) NOT NULL,
        created_at DATETIMEOFFSET(7) NULL
    );
    CREATE UNIQUE INDEX IX_bus_sites_site_name ON dbo.bus_sites(site_name);
END;
