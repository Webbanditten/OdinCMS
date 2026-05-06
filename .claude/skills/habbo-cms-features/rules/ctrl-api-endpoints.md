---
title: JSON API Endpoints
impact: MEDIUM
impactDescription: API endpoints that don't follow the pattern will have inconsistent error handling
tags: [controller, api, json, ajax]
---

# JSON API Endpoints

For AJAX-driven features, use JSON API endpoints alongside standard MVC actions.

## Pattern

```csharp
[HousekeepingFilter(Fuse.fuse_badges)]
[HttpPost]
public async Task<IActionResult> AddBadge([FromBody] UsersBadges badge)
{
    if (badge == null || string.IsNullOrEmpty(badge.BadgeCode))
    {
        return BadRequest(new { error = "Badge code is required" });
    }

    var user = await _userService.GetUserById(badge.UserId);
    if (user == null)
    {
        return NotFound(new { error = "User not found" });
    }

    var result = await _userService.AddBadge(badge);
    await _auditService.AddLog(AuditLogType.add_badge,
        int.Parse(HttpContext.User.Identity.Name), null, null, 0, badge.UserId);

    return Ok(new { success = true, data = result });
}
```

## Rules

1. Use `[HttpPost]` (or appropriate HTTP verb)
2. Accept model via `[FromBody]` for JSON payloads
3. Return `Ok()` for success, `BadRequest()` for validation errors, `NotFound()` for missing entities
4. Always include a message/error field in response objects
5. Still apply `[HousekeepingFilter]` for authorization
6. Still audit data modifications

## Client-Side Pattern (JavaScript)

```javascript
async function addBadge(userId, badgeCode) {
    const response = await fetch('/Housekeeping/Users/AddBadge', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId: userId, badgeCode: badgeCode })
    });
    
    if (response.ok) {
        const data = await response.json();
        // Handle success
    } else {
        const error = await response.json();
        Swal.fire('Error', error.error, 'error');
    }
}
```
