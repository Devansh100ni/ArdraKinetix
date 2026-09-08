-- ==========================================================
-- V1_0_0__Initial_Schema.sql
-- Multi-Tenant Task Board Initial Database Schema
-- ==========================================================

-- 1. Tenants Table
CREATE TABLE [dbo].[Tenants] (
    [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_Tenants] PRIMARY KEY,
    [Name] NVARCHAR(150) NOT NULL,
    [Code] NVARCHAR(20) NOT NULL,
    [Prefix] NVARCHAR(10) NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Tenants_IsActive] DEFAULT 1,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_Tenants_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [UpdatedOn] DATETIME2 NULL,
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_Tenants_IsDeleted] DEFAULT 0,
    [DeletedOn] DATETIME2 NULL,
    [DeletedBy] UNIQUEIDENTIFIER NULL
);
CREATE UNIQUE NONCLUSTERED INDEX [IX_Tenants_Code] ON [dbo].[Tenants]([Code]) WHERE [IsDeleted] = 0;
CREATE UNIQUE NONCLUSTERED INDEX [IX_Tenants_Prefix] ON [dbo].[Tenants]([Prefix]) WHERE [IsDeleted] = 0;
CREATE NONCLUSTERED INDEX [IX_Tenants_IsActive] ON [dbo].[Tenants]([IsActive]);
CREATE NONCLUSTERED INDEX [IX_Tenants_IsDeleted] ON [dbo].[Tenants]([IsDeleted]);

-- 2. Users Table
CREATE TABLE [dbo].[Users] (
    [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_Users] PRIMARY KEY,
    [FirstName] NVARCHAR(100) NOT NULL,
    [LastName] NVARCHAR(100) NOT NULL CONSTRAINT [DF_Users_LastName] DEFAULT '',
    [Email] NVARCHAR(256) NOT NULL,
    [Username] NVARCHAR(50) NOT NULL,
    [PasswordHash] NVARCHAR(500) NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Users_IsActive] DEFAULT 1,
    [LastLoginOn] DATETIME2 NULL,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_Users_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [UpdatedOn] DATETIME2 NULL,
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_Users_IsDeleted] DEFAULT 0,
    [DeletedOn] DATETIME2 NULL,
    [DeletedBy] UNIQUEIDENTIFIER NULL
);
CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_Email] ON [dbo].[Users]([Email]) WHERE [IsDeleted] = 0;
CREATE UNIQUE NONCLUSTERED INDEX [IX_Users_Username] ON [dbo].[Users]([Username]) WHERE [IsDeleted] = 0;
CREATE NONCLUSTERED INDEX [IX_Users_IsActive] ON [dbo].[Users]([IsActive]);
CREATE NONCLUSTERED INDEX [IX_Users_IsDeleted] ON [dbo].[Users]([IsDeleted]);

-- 3. Roles Table
CREATE TABLE [dbo].[Roles] (
    [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_Roles] PRIMARY KEY,
    [Name] NVARCHAR(50) NOT NULL,
    [NormalizedName] NVARCHAR(50) NOT NULL,
    [Description] NVARCHAR(250) NULL
);
CREATE UNIQUE NONCLUSTERED INDEX [IX_Roles_NormalizedName] ON [dbo].[Roles]([NormalizedName]);

-- 4. UserRoles Table
CREATE TABLE [dbo].[UserRoles] (
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [RoleId] UNIQUEIDENTIFIER NOT NULL,
    [AssignedOn] DATETIME2 NOT NULL CONSTRAINT [DF_UserRoles_AssignedOn] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id]) ON DELETE CASCADE
);

-- 5. UserTenants Table (Developer multi-tenant mappings & Tenant User single mapping)
CREATE TABLE [dbo].[UserTenants] (
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [TenantId] UNIQUEIDENTIFIER NOT NULL,
    [AssignedOn] DATETIME2 NOT NULL CONSTRAINT [DF_UserTenants_AssignedOn] DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_UserTenants] PRIMARY KEY ([UserId], [TenantId]),
    CONSTRAINT [FK_UserTenants_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserTenants_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]) ON DELETE CASCADE
);
CREATE NONCLUSTERED INDEX [IX_UserTenants_TenantId] ON [dbo].[UserTenants]([TenantId]);
CREATE NONCLUSTERED INDEX [IX_UserTenants_UserId] ON [dbo].[UserTenants]([UserId]);

-- 6. TenantTaskSequences Table (Concurrency safe sequence generation per tenant)
CREATE TABLE [dbo].[TenantTaskSequences] (
    [TenantId] UNIQUEIDENTIFIER NOT NULL,
    [LastNumber] BIGINT NOT NULL CONSTRAINT [DF_TenantTaskSequences_LastNumber] DEFAULT 0,
    CONSTRAINT [PK_TenantTaskSequences] PRIMARY KEY ([TenantId]),
    CONSTRAINT [FK_TenantTaskSequences_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]) ON DELETE CASCADE
);

-- 7. TaskStatuses Table
CREATE TABLE [dbo].[TaskStatuses] (
    [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_TaskStatuses] PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Code] NVARCHAR(50) NOT NULL,
    [Description] NVARCHAR(250) NULL,
    [DisplayOrder] INT NOT NULL CONSTRAINT [DF_TaskStatuses_DisplayOrder] DEFAULT 0,
    [Color] NVARCHAR(20) NOT NULL CONSTRAINT [DF_TaskStatuses_Color] DEFAULT '#64748b',
    [IsActive] BIT NOT NULL CONSTRAINT [DF_TaskStatuses_IsActive] DEFAULT 1,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_TaskStatuses_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [UpdatedOn] DATETIME2 NULL,
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_TaskStatuses_IsDeleted] DEFAULT 0,
    [DeletedOn] DATETIME2 NULL,
    [DeletedBy] UNIQUEIDENTIFIER NULL
);
CREATE UNIQUE NONCLUSTERED INDEX [IX_TaskStatuses_Code] ON [dbo].[TaskStatuses]([Code]);
CREATE NONCLUSTERED INDEX [IX_TaskStatuses_DisplayOrder] ON [dbo].[TaskStatuses]([DisplayOrder]);

-- 8. TaskPriorities Table
CREATE TABLE [dbo].[TaskPriorities] (
    [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_TaskPriorities] PRIMARY KEY,
    [Name] NVARCHAR(50) NOT NULL,
    [Code] NVARCHAR(50) NOT NULL,
    [DisplayOrder] INT NOT NULL CONSTRAINT [DF_TaskPriorities_DisplayOrder] DEFAULT 0,
    [Color] NVARCHAR(20) NOT NULL CONSTRAINT [DF_TaskPriorities_Color] DEFAULT '#64748b',
    [IsActive] BIT NOT NULL CONSTRAINT [DF_TaskPriorities_IsActive] DEFAULT 1,
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_TaskPriorities_IsDeleted] DEFAULT 0,
    [DeletedOn] DATETIME2 NULL,
    [DeletedBy] UNIQUEIDENTIFIER NULL
);
CREATE UNIQUE NONCLUSTERED INDEX [IX_TaskPriorities_Code] ON [dbo].[TaskPriorities]([Code]);
CREATE NONCLUSTERED INDEX [IX_TaskPriorities_DisplayOrder] ON [dbo].[TaskPriorities]([DisplayOrder]);

-- 9. Tasks Table
CREATE TABLE [dbo].[Tasks] (
    [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_Tasks] PRIMARY KEY,
    [TaskNumber] NVARCHAR(30) NOT NULL,
    [TenantId] UNIQUEIDENTIFIER NOT NULL,
    [Title] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(4000) NULL,
    [AssignedUserId] UNIQUEIDENTIFIER NULL,
    [ParentTaskId] UNIQUEIDENTIFIER NULL,
    [EstimatedHours] DECIMAL(6,2) NULL,
    [StartedOn] DATETIME2 NULL,
    [EndOn] DATETIME2 NULL,
    [StatusId] UNIQUEIDENTIFIER NOT NULL,
    [PriorityId] UNIQUEIDENTIFIER NOT NULL,
    [CreatedBy] UNIQUEIDENTIFIER NOT NULL,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_Tasks_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [UpdatedBy] UNIQUEIDENTIFIER NULL,
    [UpdatedOn] DATETIME2 NULL,
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_Tasks_IsDeleted] DEFAULT 0,
    [DeletedOn] DATETIME2 NULL,
    [DeletedBy] UNIQUEIDENTIFIER NULL,
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [FK_Tasks_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]),
    CONSTRAINT [FK_Tasks_Users_Assigned] FOREIGN KEY ([AssignedUserId]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT [FK_Tasks_Users_Creator] FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT [FK_Tasks_Tasks_Parent] FOREIGN KEY ([ParentTaskId]) REFERENCES [dbo].[Tasks]([Id]),
    CONSTRAINT [FK_Tasks_TaskStatuses] FOREIGN KEY ([StatusId]) REFERENCES [dbo].[TaskStatuses]([Id]),
    CONSTRAINT [FK_Tasks_TaskPriorities] FOREIGN KEY ([PriorityId]) REFERENCES [dbo].[TaskPriorities]([Id])
);
CREATE UNIQUE NONCLUSTERED INDEX [IX_Tasks_TenantId_TaskNumber] ON [dbo].[Tasks]([TenantId], [TaskNumber]);
CREATE NONCLUSTERED INDEX [IX_Tasks_TenantId] ON [dbo].[Tasks]([TenantId]);
CREATE NONCLUSTERED INDEX [IX_Tasks_AssignedUserId] ON [dbo].[Tasks]([AssignedUserId]);
CREATE NONCLUSTERED INDEX [IX_Tasks_StatusId] ON [dbo].[Tasks]([StatusId]);
CREATE NONCLUSTERED INDEX [IX_Tasks_PriorityId] ON [dbo].[Tasks]([PriorityId]);
CREATE NONCLUSTERED INDEX [IX_Tasks_ParentTaskId] ON [dbo].[Tasks]([ParentTaskId]);
CREATE NONCLUSTERED INDEX [IX_Tasks_CreatedOn] ON [dbo].[Tasks]([CreatedOn]);
CREATE NONCLUSTERED INDEX [IX_Tasks_StartedOn] ON [dbo].[Tasks]([StartedOn]);
CREATE NONCLUSTERED INDEX [IX_Tasks_EndOn] ON [dbo].[Tasks]([EndOn]);
CREATE NONCLUSTERED INDEX [IX_Tasks_IsDeleted] ON [dbo].[Tasks]([IsDeleted]);

-- 10. TaskComments Table
CREATE TABLE [dbo].[TaskComments] (
    [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_TaskComments] PRIMARY KEY,
    [TaskId] UNIQUEIDENTIFIER NOT NULL,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [Comment] NVARCHAR(4000) NOT NULL,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_TaskComments_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [UpdatedOn] DATETIME2 NULL,
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_TaskComments_IsDeleted] DEFAULT 0,
    [DeletedOn] DATETIME2 NULL,
    [DeletedBy] UNIQUEIDENTIFIER NULL,
    CONSTRAINT [FK_TaskComments_Tasks] FOREIGN KEY ([TaskId]) REFERENCES [dbo].[Tasks]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TaskComments_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
);
CREATE NONCLUSTERED INDEX [IX_TaskComments_TaskId] ON [dbo].[TaskComments]([TaskId]);
CREATE NONCLUSTERED INDEX [IX_TaskComments_CreatedOn] ON [dbo].[TaskComments]([CreatedOn]);

-- 11. TaskAttachments Table
CREATE TABLE [dbo].[TaskAttachments] (
    [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_TaskAttachments] PRIMARY KEY,
    [TaskId] UNIQUEIDENTIFIER NOT NULL,
    [FileName] NVARCHAR(255) NOT NULL,
    [StoredFileName] NVARCHAR(255) NOT NULL,
    [ContentType] NVARCHAR(100) NOT NULL,
    [FileSize] BIGINT NOT NULL,
    [StoragePath] NVARCHAR(500) NOT NULL,
    [UploadedBy] UNIQUEIDENTIFIER NOT NULL,
    [UploadedOn] DATETIME2 NOT NULL CONSTRAINT [DF_TaskAttachments_UploadedOn] DEFAULT SYSUTCDATETIME(),
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_TaskAttachments_IsDeleted] DEFAULT 0,
    [DeletedOn] DATETIME2 NULL,
    [DeletedBy] UNIQUEIDENTIFIER NULL,
    CONSTRAINT [FK_TaskAttachments_Tasks] FOREIGN KEY ([TaskId]) REFERENCES [dbo].[Tasks]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TaskAttachments_Users] FOREIGN KEY ([UploadedBy]) REFERENCES [dbo].[Users]([Id])
);
CREATE NONCLUSTERED INDEX [IX_TaskAttachments_TaskId] ON [dbo].[TaskAttachments]([TaskId]);

-- 12. TaskAudits Table
CREATE TABLE [dbo].[TaskAudits] (
    [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_TaskAudits] PRIMARY KEY,
    [TaskId] UNIQUEIDENTIFIER NOT NULL,
    [UserId] UNIQUEIDENTIFIER NULL,
    [Action] NVARCHAR(100) NOT NULL,
    [FieldName] NVARCHAR(100) NULL,
    [OldValue] NVARCHAR(4000) NULL,
    [NewValue] NVARCHAR(4000) NULL,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_TaskAudits_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [IpAddress] NVARCHAR(50) NULL,
    CONSTRAINT [FK_TaskAudits_Tasks] FOREIGN KEY ([TaskId]) REFERENCES [dbo].[Tasks]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TaskAudits_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE SET NULL
);
CREATE NONCLUSTERED INDEX [IX_TaskAudits_TaskId] ON [dbo].[TaskAudits]([TaskId]);
CREATE NONCLUSTERED INDEX [IX_TaskAudits_CreatedOn] ON [dbo].[TaskAudits]([CreatedOn]);

-- 13. AdminAudits Table
CREATE TABLE [dbo].[AdminAudits] (
    [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_AdminAudits] PRIMARY KEY,
    [UserId] UNIQUEIDENTIFIER NULL,
    [Action] NVARCHAR(100) NOT NULL,
    [EntityType] NVARCHAR(100) NOT NULL,
    [EntityId] NVARCHAR(100) NULL,
    [Details] NVARCHAR(4000) NULL,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_AdminAudits_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [IpAddress] NVARCHAR(50) NULL,
    CONSTRAINT [FK_AdminAudits_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE SET NULL
);
CREATE NONCLUSTERED INDEX [IX_AdminAudits_EntityType] ON [dbo].[AdminAudits]([EntityType]);
CREATE NONCLUSTERED INDEX [IX_AdminAudits_CreatedOn] ON [dbo].[AdminAudits]([CreatedOn]);

-- 14. ScrumBoards Table
CREATE TABLE [dbo].[ScrumBoards] (
    [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_ScrumBoards] PRIMARY KEY,
    [TenantId] UNIQUEIDENTIFIER NULL,
    [Name] NVARCHAR(150) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_ScrumBoards_IsActive] DEFAULT 1,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_ScrumBoards_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [UpdatedOn] DATETIME2 NULL,
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_ScrumBoards_IsDeleted] DEFAULT 0,
    [DeletedOn] DATETIME2 NULL,
    [DeletedBy] UNIQUEIDENTIFIER NULL,
    CONSTRAINT [FK_ScrumBoards_Tenants] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]) ON DELETE CASCADE
);
CREATE NONCLUSTERED INDEX [IX_ScrumBoards_TenantId] ON [dbo].[ScrumBoards]([TenantId]);

-- 15. ScrumBoardColumns Table
CREATE TABLE [dbo].[ScrumBoardColumns] (
    [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_ScrumBoardColumns] PRIMARY KEY,
    [BoardId] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(250) NULL,
    [DisplayOrder] INT NOT NULL CONSTRAINT [DF_ScrumBoardColumns_DisplayOrder] DEFAULT 0,
    [StatusId] UNIQUEIDENTIFIER NOT NULL,
    [WipLimit] INT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_ScrumBoardColumns_IsActive] DEFAULT 1,
    [CreatedOn] DATETIME2 NOT NULL CONSTRAINT [DF_ScrumBoardColumns_CreatedOn] DEFAULT SYSUTCDATETIME(),
    [UpdatedOn] DATETIME2 NULL,
    [IsDeleted] BIT NOT NULL CONSTRAINT [DF_ScrumBoardColumns_IsDeleted] DEFAULT 0,
    [DeletedOn] DATETIME2 NULL,
    [DeletedBy] UNIQUEIDENTIFIER NULL,
    CONSTRAINT [FK_ScrumBoardColumns_ScrumBoards] FOREIGN KEY ([BoardId]) REFERENCES [dbo].[ScrumBoards]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ScrumBoardColumns_TaskStatuses] FOREIGN KEY ([StatusId]) REFERENCES [dbo].[TaskStatuses]([Id])
);
CREATE NONCLUSTERED INDEX [IX_ScrumBoardColumns_BoardId] ON [dbo].[ScrumBoardColumns]([BoardId]);
CREATE NONCLUSTERED INDEX [IX_ScrumBoardColumns_StatusId] ON [dbo].[ScrumBoardColumns]([StatusId]);
CREATE NONCLUSTERED INDEX [IX_ScrumBoardColumns_DisplayOrder] ON [dbo].[ScrumBoardColumns]([DisplayOrder]);
