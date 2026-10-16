using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Security;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = CrmRoles.Admin)]
public class RolesController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var vm = new PermissionsMatrixViewModel
        {
            Permissions = new List<PermissionMatrixItem>
            {
                new() { Module = "Dashboard", AdminAccess = "Full (All metrics, user stats, audit totals)", ManagerAccess = "Team (All team deals & follow-ups)", SalesExecutiveAccess = "Own / Assigned (Personal pipeline & leads)" },
                new() { Module = "Customers", AdminAccess = "Full CRUD & Reassign", ManagerAccess = "Full CRUD & Team Scope", SalesExecutiveAccess = "Own / Assigned only" },
                new() { Module = "Leads", AdminAccess = "Full CRUD & Reassign", ManagerAccess = "Full CRUD & Team Scope", SalesExecutiveAccess = "Own / Assigned only" },
                new() { Module = "Opportunities", AdminAccess = "Full CRUD + Reopen closed deals", ManagerAccess = "Full CRUD (Open deals)", SalesExecutiveAccess = "Own / Assigned only (Open deals)" },
                new() { Module = "Follow-Ups", AdminAccess = "Full Schedule & Complete", ManagerAccess = "Team Schedule & Complete", SalesExecutiveAccess = "Own / Assigned only" },
                new() { Module = "Activities", AdminAccess = "Full Logging & Deletion", ManagerAccess = "Team Logging & View", SalesExecutiveAccess = "Own / Assigned only" },
                new() { Module = "User Management", AdminAccess = "Full (Create, Edit, Deactivate, Unlock, Reset Password)", ManagerAccess = "View / Limited only", SalesExecutiveAccess = "No Access (403)" },
                new() { Module = "Role Management", AdminAccess = "Full (Matrix & Policies)", ManagerAccess = "No Access (403)", SalesExecutiveAccess = "No Access (403)" },
                new() { Module = "Audit Log", AdminAccess = "Full View & CSV Export (Append-only)", ManagerAccess = "Limited View (Team events)", SalesExecutiveAccess = "No Access (403)" },
                new() { Module = "Reports", AdminAccess = "All Reports & CSV Export", ManagerAccess = "Management & Team Reports", SalesExecutiveAccess = "Own / Assigned Scope Reports" },
                new() { Module = "REST API", AdminAccess = "Full JWT Authorized", ManagerAccess = "Full Team JWT Authorized", SalesExecutiveAccess = "Own Scoped JWT Authorized" }
            }
        };

        return View(vm);
    }
}
