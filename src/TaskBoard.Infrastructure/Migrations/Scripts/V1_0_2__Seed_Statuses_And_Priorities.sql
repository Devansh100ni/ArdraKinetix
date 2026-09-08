-- ==========================================================
-- V1_0_2__Seed_Statuses_And_Priorities.sql
-- Seed standard task statuses and priorities
-- ==========================================================

-- 1. Seed Statuses
DECLARE @StatusBacklog UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000001';
DECLARE @StatusToDo UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000002';
DECLARE @StatusInProgress UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000003';
DECLARE @StatusCodeReview UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000004';
DECLARE @StatusTesting UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000005';
DECLARE @StatusDone UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000006';

IF NOT EXISTS (SELECT 1 FROM [dbo].[TaskStatuses] WHERE [Id] = @StatusBacklog)
    INSERT INTO [dbo].[TaskStatuses] ([Id], [Name], [Code], [Description], [DisplayOrder], [Color], [IsActive])
    VALUES (@StatusBacklog, 'Backlog', 'BACKLOG', 'Initial backlog of tasks', 1, '#64748b', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[TaskStatuses] WHERE [Id] = @StatusToDo)
    INSERT INTO [dbo].[TaskStatuses] ([Id], [Name], [Code], [Description], [DisplayOrder], [Color], [IsActive])
    VALUES (@StatusToDo, 'To Do', 'TODO', 'Ready for development', 2, '#3b82f6', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[TaskStatuses] WHERE [Id] = @StatusInProgress)
    INSERT INTO [dbo].[TaskStatuses] ([Id], [Name], [Code], [Description], [DisplayOrder], [Color], [IsActive])
    VALUES (@StatusInProgress, 'In Progress', 'IN_PROGRESS', 'Currently being worked on', 3, '#f59e0b', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[TaskStatuses] WHERE [Id] = @StatusCodeReview)
    INSERT INTO [dbo].[TaskStatuses] ([Id], [Name], [Code], [Description], [DisplayOrder], [Color], [IsActive])
    VALUES (@StatusCodeReview, 'Code Review', 'CODE_REVIEW', 'Peer review and pull request inspection', 4, '#8b5cf6', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[TaskStatuses] WHERE [Id] = @StatusTesting)
    INSERT INTO [dbo].[TaskStatuses] ([Id], [Name], [Code], [Description], [DisplayOrder], [Color], [IsActive])
    VALUES (@StatusTesting, 'Testing', 'TESTING', 'QA validation and automated testing', 5, '#06b6d4', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[TaskStatuses] WHERE [Id] = @StatusDone)
    INSERT INTO [dbo].[TaskStatuses] ([Id], [Name], [Code], [Description], [DisplayOrder], [Color], [IsActive])
    VALUES (@StatusDone, 'Done', 'DONE', 'Completed and verified', 6, '#10b981', 1);

-- 2. Seed Priorities
DECLARE @PriorityLow UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000001';
DECLARE @PriorityMedium UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000002';
DECLARE @PriorityHigh UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000003';
DECLARE @PriorityCritical UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000004';

IF NOT EXISTS (SELECT 1 FROM [dbo].[TaskPriorities] WHERE [Id] = @PriorityLow)
    INSERT INTO [dbo].[TaskPriorities] ([Id], [Name], [Code], [DisplayOrder], [Color], [IsActive])
    VALUES (@PriorityLow, 'Low', 'LOW', 1, '#94a3b8', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[TaskPriorities] WHERE [Id] = @PriorityMedium)
    INSERT INTO [dbo].[TaskPriorities] ([Id], [Name], [Code], [DisplayOrder], [Color], [IsActive])
    VALUES (@PriorityMedium, 'Medium', 'MEDIUM', 2, '#3b82f6', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[TaskPriorities] WHERE [Id] = @PriorityHigh)
    INSERT INTO [dbo].[TaskPriorities] ([Id], [Name], [Code], [DisplayOrder], [Color], [IsActive])
    VALUES (@PriorityHigh, 'High', 'HIGH', 3, '#f97316', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[TaskPriorities] WHERE [Id] = @PriorityCritical)
    INSERT INTO [dbo].[TaskPriorities] ([Id], [Name], [Code], [DisplayOrder], [Color], [IsActive])
    VALUES (@PriorityCritical, 'Critical', 'CRITICAL', 4, '#ef4444', 1);
