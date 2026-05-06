---
title: Audit Logging
impact: HIGH
impactDescription: Missing audit logs make it impossible to track admin actions for accountability
tags: [service, audit, logging, accountability]
---

# Audit Logging

All data-modifying admin actions must be logged for accountability.

## Usage in Controllers

```csharp
// After a create operation:
await _auditService.AddLog(
    AuditLogType.create_news,           // Log type enum
    int.Parse(HttpContext.User.Identity.Name), // Acting user ID
    0                                    // Extra param
);

// After an update operation:
await _auditService.AddLog(
    AuditLogType.edit_news,
    int.Parse(HttpContext.User.Identity.Name),
    null,  // optional string param
    null,  // optional string param  
    0,     // optional int param
    itemId // related entity ID
);

// After a delete operation:
await _auditService.AddLog(
    AuditLogType.delete_news,
    int.Parse(HttpContext.User.Identity.Name),
    null, null, 0, itemId
);
```

## Adding New Audit Log Types

If your feature needs specific log types, add them to the `AuditLogType` enum:

```csharp
// In the AuditLogType enum:
create_my_feature,
edit_my_feature,
delete_my_feature,
```

## Rules

1. ALWAYS audit: create, update, delete, ban, kick, alert, permission changes
2. Include the acting user's ID (who performed the action)
3. Include the target entity ID when applicable
4. The audit log is viewable in Housekeeping > Audit Log section
5. Never skip audit logging — it's required for moderation accountability
