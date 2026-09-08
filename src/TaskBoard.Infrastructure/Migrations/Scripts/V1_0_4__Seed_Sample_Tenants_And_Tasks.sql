-- ==========================================================
-- V1_0_4__Seed_Sample_Tenants_And_Tasks.sql
-- Seed sample tenants, developer multi-tenant mapping, tenant users, and tasks
-- ==========================================================

DECLARE @TenantAcmeId UNIQUEIDENTIFIER = '40000000-0000-0000-0000-000000000001';
DECLARE @TenantBetaId UNIQUEIDENTIFIER = '40000000-0000-0000-0000-000000000002';

DECLARE @DeveloperRoleId UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @TenantUserRoleId UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';

DECLARE @DevUserId UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000001';
DECLARE @AcmeUserId UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000002';
DECLARE @BetaUserId UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000003';

DECLARE @DevPasswordHash NVARCHAR(500) = 'AQABhqA3uNbuEkfpzXMerzvIIeXT1I7Fln1csvKDWpH9hHW6arMI2zXJKhChT7Z46I/nLYM='; -- Developer123!
DECLARE @TenantPasswordHash NVARCHAR(500) = 'AQABhqAIj/jNfCDIsEJI6X0mFYSYYzRIbYvkS1Y8RyYnQRKUHVE1sgWbNPZtmhp1h0G9ykQ='; -- TenantUser123!

-- 1. Seed Tenants
IF NOT EXISTS (SELECT 1 FROM [dbo].[Tenants] WHERE [Id] = @TenantAcmeId)
BEGIN
    INSERT INTO [dbo].[Tenants] ([Id], [Name], [Code], [Prefix], [IsActive], [CreatedOn])
    VALUES (@TenantAcmeId, 'Acme Corporation', 'ACM', 'ACM', 1, SYSUTCDATETIME());

    INSERT INTO [dbo].[TenantTaskSequences] ([TenantId], [LastNumber])
    VALUES (@TenantAcmeId, 3);
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Tenants] WHERE [Id] = @TenantBetaId)
BEGIN
    INSERT INTO [dbo].[Tenants] ([Id], [Name], [Code], [Prefix], [IsActive], [CreatedOn])
    VALUES (@TenantBetaId, 'Beta Dynamics', 'BET', 'BET', 1, SYSUTCDATETIME());

    INSERT INTO [dbo].[TenantTaskSequences] ([TenantId], [LastNumber])
    VALUES (@TenantBetaId, 2);
END

-- 2. Seed Developer User (assigned to both Acme and Beta)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Id] = @DevUserId)
BEGIN
    INSERT INTO [dbo].[Users] ([Id], [FirstName], [LastName], [Email], [Username], [PasswordHash], [IsActive], [CreatedOn])
    VALUES (@DevUserId, 'Alex', 'Developer', 'developer@taskboard.local', 'devuser', @DevPasswordHash, 1, SYSUTCDATETIME());

    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (@DevUserId, @DeveloperRoleId);
    INSERT INTO [dbo].[UserTenants] ([UserId], [TenantId]) VALUES (@DevUserId, @TenantAcmeId);
    INSERT INTO [dbo].[UserTenants] ([UserId], [TenantId]) VALUES (@DevUserId, @TenantBetaId);
END

-- 3. Seed Acme Tenant User
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Id] = @AcmeUserId)
BEGIN
    INSERT INTO [dbo].[Users] ([Id], [FirstName], [LastName], [Email], [Username], [PasswordHash], [IsActive], [CreatedOn])
    VALUES (@AcmeUserId, 'Sarah', 'Acme', 'tenantuser@acme.local', 'acmeuser', @TenantPasswordHash, 1, SYSUTCDATETIME());

    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (@AcmeUserId, @TenantUserRoleId);
    INSERT INTO [dbo].[UserTenants] ([UserId], [TenantId]) VALUES (@AcmeUserId, @TenantAcmeId);
END

-- 4. Seed Beta Tenant User
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Id] = @BetaUserId)
BEGIN
    INSERT INTO [dbo].[Users] ([Id], [FirstName], [LastName], [Email], [Username], [PasswordHash], [IsActive], [CreatedOn])
    VALUES (@BetaUserId, 'David', 'Beta', 'tenantuser@beta.local', 'betauser', @TenantPasswordHash, 1, SYSUTCDATETIME());

    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId]) VALUES (@BetaUserId, @TenantUserRoleId);
    INSERT INTO [dbo].[UserTenants] ([UserId], [TenantId]) VALUES (@BetaUserId, @TenantBetaId);
END

-- 5. Seed Tasks for Acme Corporation
DECLARE @StatusBacklog UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000001';
DECLARE @StatusToDo UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000002';
DECLARE @StatusInProgress UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000003';
DECLARE @StatusCodeReview UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000004';
DECLARE @StatusTesting UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000005';
DECLARE @StatusDone UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000006';

DECLARE @PriorityLow UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000001';
DECLARE @PriorityMedium UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000002';
DECLARE @PriorityHigh UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000003';
DECLARE @PriorityCritical UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000004';

DECLARE @TaskAcme1 UNIQUEIDENTIFIER = '60000000-0000-0000-0000-000000000001';
DECLARE @TaskAcme2 UNIQUEIDENTIFIER = '60000000-0000-0000-0000-000000000002';
DECLARE @TaskAcme3 UNIQUEIDENTIFIER = '60000000-0000-0000-0000-000000000003';

IF NOT EXISTS (SELECT 1 FROM [dbo].[Tasks] WHERE [Id] = @TaskAcme1)
BEGIN
    INSERT INTO [dbo].[Tasks] ([Id], [TaskNumber], [TenantId], [Title], [Description], [AssignedUserId], [EstimatedHours], [StatusId], [PriorityId], [CreatedBy], [CreatedOn])
    VALUES (@TaskAcme1, 'ACM-000001', @TenantAcmeId, 'Multi-Tenant Architecture Setup', 'Setup Clean Architecture layers and tenant isolation filters.', @DevUserId, 16.0, @StatusDone, @PriorityCritical, @AcmeUserId, SYSUTCDATETIME());

    INSERT INTO [dbo].[TaskAudits] ([Id], [TaskId], [UserId], [Action], [FieldName], [NewValue], [CreatedOn])
    VALUES (NEWID(), @TaskAcme1, @AcmeUserId, 'Created', 'Task', 'Task created: ACM-000001', SYSUTCDATETIME());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Tasks] WHERE [Id] = @TaskAcme2)
BEGIN
    INSERT INTO [dbo].[Tasks] ([Id], [TaskNumber], [TenantId], [Title], [Description], [AssignedUserId], [EstimatedHours], [StatusId], [PriorityId], [CreatedBy], [CreatedOn])
    VALUES (@TaskAcme2, 'ACM-000002', @TenantAcmeId, 'Interactive Drag-and-Drop Scrum Board', 'Implement dynamic HTML5 drag and drop with instant AJAX status mutation.', @DevUserId, 12.0, @StatusInProgress, @PriorityHigh, @AcmeUserId, SYSUTCDATETIME());

    INSERT INTO [dbo].[TaskComments] ([Id], [TaskId], [UserId], [Comment], [CreatedOn])
    VALUES (NEWID(), @TaskAcme2, @DevUserId, 'Drag-and-drop animation with WIP limits check is implemented and tested smoothly.', SYSUTCDATETIME());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Tasks] WHERE [Id] = @TaskAcme3)
BEGIN
    INSERT INTO [dbo].[Tasks] ([Id], [TaskNumber], [TenantId], [Title], [Description], [AssignedUserId], [ParentTaskId], [EstimatedHours], [StatusId], [PriorityId], [CreatedBy], [CreatedOn])
    VALUES (@TaskAcme3, 'ACM-000003', @TenantAcmeId, 'Child Subtask: Evolve SQL Migrations Verification', 'Verify Evolve migrations on local SQL Server instance.', @DevUserId, @TaskAcme1, 4.0, @StatusTesting, @PriorityMedium, @AcmeUserId, SYSUTCDATETIME());
END

-- 6. Seed Tasks for Beta Dynamics
DECLARE @TaskBeta1 UNIQUEIDENTIFIER = '70000000-0000-0000-0000-000000000001';
DECLARE @TaskBeta2 UNIQUEIDENTIFIER = '70000000-0000-0000-0000-000000000002';

IF NOT EXISTS (SELECT 1 FROM [dbo].[Tasks] WHERE [Id] = @TaskBeta1)
BEGIN
    INSERT INTO [dbo].[Tasks] ([Id], [TaskNumber], [TenantId], [Title], [Description], [AssignedUserId], [EstimatedHours], [StatusId], [PriorityId], [CreatedBy], [CreatedOn])
    VALUES (@TaskBeta1, 'BET-000001', @TenantBetaId, 'Modern Tailwind CSS Redesign', 'Adopt rich dark/light UI palette with glassmorphic cards.', @DevUserId, 20.0, @StatusToDo, @PriorityMedium, @BetaUserId, SYSUTCDATETIME());
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Tasks] WHERE [Id] = @TaskBeta2)
BEGIN
    INSERT INTO [dbo].[Tasks] ([Id], [TaskNumber], [TenantId], [Title], [Description], [AssignedUserId], [EstimatedHours], [StatusId], [PriorityId], [CreatedBy], [CreatedOn])
    VALUES (@TaskBeta2, 'BET-000002', @TenantBetaId, 'Attachment Support for PDF and Images', 'Validate file headers, safe GUID filenames, and preview modals.', @DevUserId, 8.0, @StatusInProgress, @PriorityHigh, @BetaUserId, SYSUTCDATETIME());
END
