---
title: Housekeeping Controller Pattern
impact: CRITICAL
impactDescription: Controllers that don't follow this pattern will lack access control and audit capabilities
tags: [controller, housekeeping, pattern]
---

# Housekeeping Controller Pattern

Every Housekeeping controller follows a consistent structure.

## Template

```csharp
using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class {Feature}Controller : Controller
    {
        private readonly I{Feature}Service _{feature}Service;
        private readonly IAuditLogService _auditService;

        public {Feature}Controller(I{Feature}Service {feature}Service, IAuditLogService auditService)
        {
            _{feature}Service = {feature}Service;
            _auditService = auditService;
        }

        [HousekeepingFilter(Fuse.housekeeping)]
        public async Task<IActionResult> Index()
        {
            var items = await _{feature}Service.GetAll();
            return View(items);
        }
    }
}
```

## Rules

1. Namespace is always `KeplerCMS.Areas.Housekeeping` (flat, not nested further)
2. `[Area("Housekeeping")]` on the class
3. `[HousekeepingFilter(Fuse.xxx)]` on every action method
4. Inject services via constructor (never resolve from HttpContext)
5. Always inject `IAuditLogService` for any controller that modifies data
6. Use `async Task<IActionResult>` return type for all actions
7. Get current user ID via `int.Parse(HttpContext.User.Identity.Name)`
8. The `HousekeepingFilter` automatically populates `ViewData["user"]` with the current user object

## Getting Current User in Views

```cshtml
@{
    var user = (Users)ViewData["user"];
}
<p>Logged in as: @user.Username</p>
```
