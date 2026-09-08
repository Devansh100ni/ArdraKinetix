-- ==========================================================
-- V1_0_5__Security_RefreshTokens_Notifications_Lockout.sql
-- ArdraKinetix Enterprise Security, Session Revocation,
-- Refresh Tokens, Lockout Policies & Notifications
-- ==========================================================

-- 1. Extend Users table with enterprise security fields
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = N'IsLockedOut')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [IsLockedOut] BIT NOT NULL CONSTRAINT [DF_Users_IsLockedOut] DEFAULT 0;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = N'LockoutEndUtc')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [LockoutEndUtc] DATETIME2 NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = N'FailedLoginAttempts')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [FailedLoginAttempts] INT NOT NULL CONSTRAINT [DF_Users_FailedLoginAttempts] DEFAULT 0;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = N'LockoutReason')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [LockoutReason] NVARCHAR(250) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = N'MustChangePassword')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [MustChangePassword] BIT NOT NULL CONSTRAINT [DF_Users_MustChangePassword] DEFAULT 0;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = N'SecurityStamp')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [SecurityStamp] NVARCHAR(100) NOT NULL CONSTRAINT [DF_Users_SecurityStamp] DEFAULT NEWID();
END;

-- 2. Create RefreshTokens Table for 5-Day Token Persistence & Session Revocation
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'RefreshTokens' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE [dbo].[RefreshTokens] (
        [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_RefreshTokens] PRIMARY KEY,
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [Token] NVARCHAR(256) NOT NULL,
        [ExpiresUtc] DATETIME2 NOT NULL,
        [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_RefreshTokens_CreatedOn] DEFAULT SYSUTCDATETIME(),
        [CreatedByIp] NVARCHAR(50) NULL,
        [RevokedOn] DATETIME2 NULL,
        [RevokedByIp] NVARCHAR(50) NULL,
        [ReplacedByToken] NVARCHAR(256) NULL,
        [ReasonRevoked] NVARCHAR(250) NULL,
        CONSTRAINT [FK_RefreshTokens_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_RefreshTokens_UserId_Token] ON [dbo].[RefreshTokens]([UserId], [Token]);
    CREATE NONCLUSTERED INDEX [IX_RefreshTokens_ExpiresUtc] ON [dbo].[RefreshTokens]([ExpiresUtc]);
END;

-- 3. Create Notifications Table for Real-time SignalR Alerts & User Center
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Notifications' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_Notifications] PRIMARY KEY,
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [TenantId] UNIQUEIDENTIFIER NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [Message] NVARCHAR(1000) NOT NULL,
        [Type] NVARCHAR(50) NOT NULL, -- e.g., 'TaskAssigned', 'TaskStatusChanged', 'CommentAdded', 'SecurityAlert'
        [TargetUrl] NVARCHAR(500) NULL,
        [IsRead] BIT NOT NULL CONSTRAINT [DF_Notifications_IsRead] DEFAULT 0,
        [ReadOn] DATETIME2 NULL,
        [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_Notifications_CreatedOn] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_Notifications_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Notifications_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_Notifications_UserId_IsRead] ON [dbo].[Notifications]([UserId], [IsRead]);
    CREATE NONCLUSTERED INDEX [IX_Notifications_UserId_CreatedOn] ON [dbo].[Notifications]([UserId], [CreatedOn] DESC);
END;
