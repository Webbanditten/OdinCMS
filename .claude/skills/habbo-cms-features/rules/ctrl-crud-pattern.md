---
title: CRUD Action Patterns
impact: HIGH
impactDescription: Inconsistent CRUD patterns lead to data integrity issues and missing audit trails
tags: [controller, crud, forms, post-redirect-get]
---

# CRUD Action Patterns

Standard Create/Read/Update/Delete patterns for Housekeeping controllers.

## Index (List)

```csharp
[HousekeepingFilter(Fuse.housekeeping)]
public async Task<IActionResult> Index(string message = null)
{
    ViewBag.Message = message;
    var items = await _service.GetAll();
    return View(items);
}
```

## Create (GET + POST)

```csharp
[HousekeepingFilter(Fuse.housekeeping)]
public IActionResult Create()
{
    return View();
}

[HousekeepingFilter(Fuse.housekeeping)]
[HttpPost]
public async Task<IActionResult> Create(MyModel model)
{
    if (ModelState.IsValid)
    {
        await _service.Create(model);
        await _auditService.AddLog(AuditLogType.xxx, 
            int.Parse(HttpContext.User.Identity.Name), 0);
        return RedirectToAction("Index", new { message = "Created successfully" });
    }
    return View(model);
}
```

## Update (GET + POST)

```csharp
[HousekeepingFilter(Fuse.housekeeping)]
public async Task<IActionResult> Update(int id)
{
    var item = await _service.GetById(id);
    if (item == null) return NotFound();
    return View(item);
}

[HousekeepingFilter(Fuse.housekeeping)]
[HttpPost]
public async Task<IActionResult> Update(MyModel model)
{
    if (ModelState.IsValid)
    {
        await _service.Update(model);
        await _auditService.AddLog(AuditLogType.xxx, 
            int.Parse(HttpContext.User.Identity.Name), null, null, 0, model.Id);
        return RedirectToAction("Index", new { message = "Updated successfully" });
    }
    return View(model);
}
```

## Remove (GET with redirect)

```csharp
[HousekeepingFilter(Fuse.housekeeping)]
public async Task<IActionResult> Remove(int id)
{
    await _service.Remove(id);
    await _auditService.AddLog(AuditLogType.xxx, 
        int.Parse(HttpContext.User.Identity.Name), null, null, 0, id);
    return RedirectToAction("Index", new { message = "Removed successfully" });
}
```

## Rules

1. Always use PRG (Post-Redirect-Get) — never return a View after POST
2. Always validate `ModelState.IsValid` before processing
3. Always audit create/update/delete operations
4. Use `RedirectToAction` with a message query param for user feedback
5. Check for null on Update/Remove and return `NotFound()` if item doesn't exist
