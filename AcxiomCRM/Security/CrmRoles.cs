namespace AcxiomCRM.Security;

public static class CrmRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string SalesExecutive = "SalesExecutive";

    public static readonly IReadOnlyList<string> AllRoles = new[] { Admin, Manager, SalesExecutive };
}

public static class CrmPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string ManagerOrAdmin = "ManagerOrAdmin";
    public const string AnyCrmUser = "AnyCrmUser";
}
