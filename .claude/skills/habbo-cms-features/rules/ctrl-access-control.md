---
title: Access Control with Fuses
impact: CRITICAL
impactDescription: Missing access control exposes admin features to unauthorized users
tags: [security, access-control, fuses, filter]
---

# Access Control with Fuses

The Fuse system is KeplerCMS's permission model. Every admin action must be protected.

## How It Works

1. Ranks have assigned fuses (permissions)
2. The `[HousekeepingFilter(Fuse.xxx)]` attribute checks if the logged-in user's rank has the required fuse
3. If not authorized, the user gets a 401 Challenge response
4. The filter also injects `ViewData["user"]` with the full user object

## Usage

```csharp
// Single permission
[HousekeepingFilter(Fuse.housekeeping)]

// Multiple permissions (user needs ANY of them)
[HousekeepingFilter(new[] { Fuse.fuse_kick, Fuse.fuse_ban })]
```

## Common Fuses

| Fuse | Purpose |
|------|---------|
| `Fuse.housekeeping` | Base admin panel access |
| `Fuse.housekeeping_news` | News management |
| `Fuse.fuse_kick` | Kick users / user management |
| `Fuse.fuse_ban` | Ban users |
| `Fuse.fuse_alert` | Send alerts to users |
| `Fuse.fuse_badges` | Manage user badges |
| `Fuse.fuse_catalogue_manager` | Edit catalogue |

## Adding New Fuses

1. Add to the `Fuse` enum in `Models/Fuse.cs`:
```csharp
[Description("fuse_my_feature")]
fuse_my_feature,
```

2. Insert into database for the appropriate rank:
```sql
INSERT INTO rank_fuserights (rank_id, fuse_name) VALUES (7, 'fuse_my_feature');
```

## Sidebar Visibility

Menu items in the sidebar also use fuse checks via `HousekeepingAccess.HasAccess()` to hide links the user can't access.
