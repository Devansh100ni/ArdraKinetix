namespace TaskBoard.Web.Authorization;

public static class PolicyNames
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireDeveloperOrAdmin = "RequireDeveloperOrAdmin";
    public const string RequireAuthenticated = "RequireAuthenticated";
}
