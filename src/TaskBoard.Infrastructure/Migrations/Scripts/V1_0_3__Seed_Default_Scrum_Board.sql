-- ==========================================================
-- V1_0_3__Seed_Default_Scrum_Board.sql
-- Seed global default Scrum board and dynamic columns
-- ==========================================================

DECLARE @DefaultBoardId UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000001';

DECLARE @StatusBacklog UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000001';
DECLARE @StatusToDo UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000002';
DECLARE @StatusInProgress UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000003';
DECLARE @StatusCodeReview UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000004';
DECLARE @StatusTesting UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000005';
DECLARE @StatusDone UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000006';

IF NOT EXISTS (SELECT 1 FROM [dbo].[ScrumBoards] WHERE [Id] = @DefaultBoardId)
BEGIN
    INSERT INTO [dbo].[ScrumBoards] ([Id], [TenantId], [Name], [Description], [IsActive], [CreatedOn])
    VALUES (@DefaultBoardId, NULL, 'Standard Agile Scrum Board', 'Global default multi-stage Agile development workflow', 1, SYSUTCDATETIME());

    INSERT INTO [dbo].[ScrumBoardColumns] ([Id], [BoardId], [Name], [Description], [DisplayOrder], [StatusId], [WipLimit], [IsActive], [CreatedOn])
    VALUES 
    (NEWID(), @DefaultBoardId, 'Backlog', 'Items in backlog awaiting sprint grooming', 1, @StatusBacklog, NULL, 1, SYSUTCDATETIME()),
    (NEWID(), @DefaultBoardId, 'To Do', 'Committed items ready for development', 2, @StatusToDo, NULL, 1, SYSUTCDATETIME()),
    (NEWID(), @DefaultBoardId, 'In Development', 'Actively in progress', 3, @StatusInProgress, 6, 1, SYSUTCDATETIME()),
    (NEWID(), @DefaultBoardId, 'Code Review', 'Peer inspection and pull request review', 4, @StatusCodeReview, 3, 1, SYSUTCDATETIME()),
    (NEWID(), @DefaultBoardId, 'QA / Testing', 'Quality assurance and testing', 5, @StatusTesting, 4, 1, SYSUTCDATETIME()),
    (NEWID(), @DefaultBoardId, 'Done', 'Completed and delivered', 6, @StatusDone, NULL, 1, SYSUTCDATETIME());
END
