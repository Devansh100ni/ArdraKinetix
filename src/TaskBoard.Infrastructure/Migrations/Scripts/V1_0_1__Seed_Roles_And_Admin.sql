-- ==========================================================
-- V1_0_1__Seed_Roles_And_Admin.sql
-- Seed standard roles and initial administrator account
-- ==========================================================

DECLARE @AdminRoleId UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @DeveloperRoleId UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @TenantUserRoleId UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';

-- 1. Seed Roles
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = @AdminRoleId)
BEGIN
    INSERT INTO [dbo].[Roles] ([Id], [Name], [NormalizedName], [Description])
    VALUES (@AdminRoleId, 'Admin', 'ADMIN', 'Global System Administrator with full enterprise access');
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = @DeveloperRoleId)
BEGIN
    INSERT INTO [dbo].[Roles] ([Id], [Name], [NormalizedName], [Description])
    VALUES (@DeveloperRoleId, 'Developer', 'DEVELOPER', 'Cross-tenant Developer with access to assigned tenants');
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Id] = @TenantUserRoleId)
BEGIN
    INSERT INTO [dbo].[Roles] ([Id], [Name], [NormalizedName], [Description])
    VALUES (@TenantUserRoleId, 'TenantUser', 'TENANTUSER', 'Tenant member restricted strictly to their organization');
END

-- 2. Seed Initial Administrator Account (Username: admin, Password: AdminPassword123!)
DECLARE @AdminUserId UNIQUEIDENTIFIER = 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA';
DECLARE @AdminPasswordHash NVARCHAR(500) = 'AQABhqAUkzZdhjJZctvWXHE9zbikH4X2G9KkoEED5BdVoseDOQYEPlD7sZrENg8vpNKKgyw=';

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Id] = @AdminUserId OR [Username] = 'admin')
BEGIN
    INSERT INTO [dbo].[Users] ([Id], [FirstName], [LastName], [Email], [Username], [PasswordHash], [IsActive], [CreatedOn], [IsDeleted])
    VALUES (@AdminUserId, 'System', 'Administrator', 'admin@taskboard.local', 'admin', @AdminPasswordHash, 1, SYSUTCDATETIME(), 0);

    INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId], [AssignedOn])
    VALUES (@AdminUserId, @AdminRoleId, SYSUTCDATETIME());
END
