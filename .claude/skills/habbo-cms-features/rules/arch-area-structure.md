---
title: Area Structure
impact: HIGH
impactDescription: Features placed outside the proper area structure will not route correctly or follow access control patterns
tags: [architecture, areas, organization]
---

# Area Structure

KeplerCMS uses ASP.NET Core Areas to isolate large feature groups. The primary admin area is `Housekeeping`.

## Structure

```
Areas/Housekeeping/
├── Controllers/          # [Area("Housekeeping")] controllers
├── Views/
│   ├── {Controller}/     # Views per controller
│   ├── Shared/           # Shared layout and partials
│   ├── _ViewStart.cshtml # Sets layout to _Housekeeping
│   └── _ViewImports.cshtml
├── Models/
│   ├── Views/            # View models
│   └── Enums/            # Feature enums
└── Helpers/              # Area-specific helpers
```

## Rules

1. Every Housekeeping controller MUST have `[Area("Housekeeping")]` attribute
2. Views are auto-discovered at `Areas/Housekeeping/Views/{Controller}/{Action}.cshtml`
3. Shared layout lives at `Areas/Housekeeping/Views/Shared/_Housekeeping.cshtml`
4. `_ViewStart.cshtml` sets the layout — individual views don't need to specify it
5. `_ViewImports.cshtml` provides shared `@using` statements and tag helpers

## Incorrect

```csharp
// Missing Area attribute - routes won't work
public class MyController : Controller
{
    public IActionResult Index() => View();
}
```

## Correct

```csharp
[Area("Housekeeping")]
public class MyController : Controller
{
    [HousekeepingFilter(Fuse.housekeeping)]
    public IActionResult Index() => View();
}
```
