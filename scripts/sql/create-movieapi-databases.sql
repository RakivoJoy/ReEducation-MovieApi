-- Run against the local SQL Server default instance with Windows Authentication.
-- EF Core migrations will create the application schema.
USE [master];
GO

IF DB_ID(N'MovieApiDev') IS NULL
    CREATE DATABASE [MovieApiDev];
GO

IF DB_ID(N'MovieApiIntegrationTests') IS NULL
    CREATE DATABASE [MovieApiIntegrationTests];
GO

IF (SELECT COUNT(*) FROM sys.databases
    WHERE name IN (N'MovieApiDev', N'MovieApiIntegrationTests') AND state_desc = N'ONLINE') <> 2
    THROW 50000, 'Both MovieApi databases must exist and be online.', 1;

SELECT name, state_desc
FROM sys.databases
WHERE name IN (N'MovieApiDev', N'MovieApiIntegrationTests')
ORDER BY name;
GO
