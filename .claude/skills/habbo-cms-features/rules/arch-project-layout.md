---
title: Project Layout
impact: MEDIUM
impactDescription: Files placed in wrong locations won't be discovered or compiled correctly
tags: [architecture, project, organization, files]
---

# Project Layout

Understanding where each type of file belongs in the KeplerCMS project.

## File Placement Guide

| File Type | Location | Naming |
|-----------|----------|--------|
| Main controller | `Controllers/` | `{Name}Controller.cs` |
| Housekeeping controller | `Areas/Housekeeping/Controllers/` | `{Name}Controller.cs` |
| Service interface | `Services/Interfaces/` | `I{Name}Service.cs` |
| Service implementation | `Services/Implementations/` | `{Name}Service.cs` |
| Database entity | `Data/Models/` | `{EntityName}.cs` |
| DbContext | `Data/DataContext.cs` | (single file) |
| View model (main) | `Models/` | `{Name}ViewModel.cs` |
| View model (area) | `Areas/Housekeeping/Models/Views/` | `{Name}ViewModel.cs` |
| Razor view | `Areas/Housekeeping/Views/{Controller}/` | `{Action}.cshtml` |
| Shared partial | `Areas/Housekeeping/Views/Shared/` | `_{Name}.cshtml` |
| Action filter | `Filters/` | `{Name}Filter.cs` |
| Helper class | `Helpers/` or `Areas/Housekeeping/Helpers/` | `{Name}.cs` |
| SignalR hub | `Hubs/` | `{Name}Hub.cs` |
| Background service | `BackgroundServices/` | `{Name}BackgroundService.cs` |
| Static files | `wwwroot/` | CSS, JS, images |
| SQL migrations | `sql/` | `{description}.sql` |
| Enum | `Models/` or `Areas/.../Models/Enums/` | `{Name}.cs` |

## Important Conventions

1. Housekeeping controllers use a **flat namespace** — `KeplerCMS.Areas.Housekeeping` (not nested per controller)
2. Service interfaces and implementations are in **separate directories** but same namespace level
3. View models for Housekeeping go in `Areas/Housekeeping/Models/Views/` (not in the main `Models/` folder)
4. Shared Razor partials are prefixed with underscore: `_MyPartial.cshtml`
5. SQL files don't have a specific naming convention beyond being descriptive
