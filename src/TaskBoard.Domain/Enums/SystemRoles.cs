namespace TaskBoard.Domain.Enums;

public static class SystemRoles
{
    public const string Admin = "Admin";
    public const string Developer = "Developer";
    public const string TenantUser = "TenantUser";

    public static readonly IReadOnlyList<string> AllRoles = [Admin, Developer, TenantUser];
}
