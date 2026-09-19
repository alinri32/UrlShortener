-- Database Initialization
USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'UrlShortenerDb')
BEGIN
    CREATE DATABASE UrlShortenerDb
    COLLATE Latin1_General_100_CI_AS_SC_UTF8;
END
GO

USE UrlShortenerDb;
GO

-- Cleanup Existing Artifacts
IF OBJECT_ID(N'dbo.usp_AllocateIdRange', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_AllocateIdRange;
IF OBJECT_ID(N'dbo.ShortenedUrls', N'U') IS NOT NULL DROP TABLE dbo.ShortenedUrls;
IF OBJECT_ID(N'dbo.ApiKeys', N'U') IS NOT NULL DROP TABLE dbo.ApiKeys;
IF OBJECT_ID(N'dbo.IdAllocationState', N'U') IS NOT NULL DROP TABLE dbo.IdAllocationState;
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE dbo.Users;
GO

-- Users Table
CREATE TABLE dbo.Users
(
    Id BIGINT IDENTITY(1,1) NOT NULL,
    FirstName NVARCHAR(60) NOT NULL,
    LastName NVARCHAR(60) NOT NULL,
    Email VARCHAR(256) NOT NULL,
    PhoneNumber VARCHAR(20) NULL,
    PasswordHash VARCHAR(128) NOT NULL,
    CreatedAt DATETIME2(2) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,

    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX IX_Users_Email 
ON dbo.Users(Email);

CREATE UNIQUE NONCLUSTERED INDEX IX_Users_PhoneNumber 
ON dbo.Users(PhoneNumber) 
WHERE PhoneNumber IS NOT NULL;
GO

-- ApiKeys Table
CREATE TABLE dbo.ApiKeys
(
    Id BIGINT IDENTITY(1,1) NOT NULL,
    UserId BIGINT NOT NULL,
    KeyHash CHAR(64) NOT NULL, -- SHA-256 Hex Representation
    KeyPrefix VARCHAR(8) NOT NULL, -- Public Key Prefix For Fast Filtering
    CreatedAt DATETIME2(2) NOT NULL CONSTRAINT DF_ApiKeys_CreatedAt DEFAULT SYSUTCDATETIME(),
    ExpiresAt DATETIME2(2) NULL,
    IsRevoked BIT NOT NULL CONSTRAINT DF_ApiKeys_IsRevoked DEFAULT 0,

    CONSTRAINT PK_ApiKeys PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_ApiKeys_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
);
GO

CREATE UNIQUE NONCLUSTERED INDEX IX_ApiKeys_KeyHash 
ON dbo.ApiKeys(KeyHash) 
INCLUDE (UserId, IsRevoked, ExpiresAt);

CREATE NONCLUSTERED INDEX IX_ApiKeys_UserId 
ON dbo.ApiKeys(UserId);
GO

-- ShortenedUrls Table
CREATE TABLE dbo.ShortenedUrls
(
    Id BIGINT NOT NULL, -- Numeric ID mapped to Base62
    ShortCode VARCHAR(8) COLLATE Latin1_General_100_BIN2 NOT NULL,
    OriginalUrl VARCHAR(2048) NOT NULL,
    CreatedByUserId BIGINT NULL,
    CreatedAt DATETIME2(2) NOT NULL CONSTRAINT DF_ShortenedUrls_CreatedAt DEFAULT SYSUTCDATETIME(),
    ExpiresAt DATETIME2(2) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_ShortenedUrls_IsActive DEFAULT 1,

    CONSTRAINT PK_ShortenedUrls PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_ShortenedUrls_Users FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(Id) ON DELETE SET NULL
);
GO

-- Point-Lookup Index for High-Speed Redirection
CREATE UNIQUE NONCLUSTERED INDEX IX_ShortenedUrls_ShortCode 
ON dbo.ShortenedUrls(ShortCode)
INCLUDE (OriginalUrl, IsActive, ExpiresAt);

CREATE NONCLUSTERED INDEX IX_ShortenedUrls_CreatedByUserId 
ON dbo.ShortenedUrls(CreatedByUserId) 
WHERE CreatedByUserId IS NOT NULL;
GO

-- ID Allocation State Table (Single-Row Lock Engine)
CREATE TABLE dbo.IdAllocationState
(
    Id TINYINT NOT NULL CONSTRAINT PK_IdAllocationState PRIMARY KEY CLUSTERED,
    CurrentMaxId BIGINT NOT NULL,
    LastAllocatedAt DATETIME2(2) NOT NULL CONSTRAINT DF_IdAllocationState_LastAllocatedAt DEFAULT SYSUTCDATETIME()
);
GO

-- Seed Starting Block (Minimum 10,000,000 to produce clean multi-char Base62 tokens)
INSERT INTO dbo.IdAllocationState (Id, CurrentMaxId)
VALUES (1, 10000000);
GO

-- High-Performance Atomic Range Allocation Routine
CREATE OR ALTER PROCEDURE dbo.usp_AllocateIdRange
    @BatchSize INT = 100000,
    @AllocatedStart BIGINT OUTPUT,
    @AllocatedEnd BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Concurrency Lock & Atomic Update
    UPDATE dbo.IdAllocationState WITH (UPDLOCK, ROWLOCK)
    SET 
        @AllocatedStart = CurrentMaxId + 1,
        @AllocatedEnd = CurrentMaxId + @BatchSize,
        CurrentMaxId = CurrentMaxId + @BatchSize,
        LastAllocatedAt = SYSUTCDATETIME()
    WHERE Id = 1;
END;
GO