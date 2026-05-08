---
title: Housekeeping Layout and Navigation
impact: HIGH
impactDescription: Views without proper layout integration won't have navigation or consistent styling
tags: [views, layout, navigation, menu]
---

# Housekeeping Layout and Navigation

The Housekeeping area uses a shared layout with a horizontal nav bar and sidebar menu.

## Layout Structure

The layout (`_Housekeeping.cshtml`) provides:
- Top bar with user info and logout
- Horizontal main navigation (Home, Website management, Hobba tools, Admin tools, Localization)
- Left sidebar with context-sensitive menu sections
- Main content area (`@RenderBody()`)
- Scripts section (`@await RenderSectionAsync("scripts", false)`)

## Setting Page Title

```cshtml
@{
    ViewBag.Title = "My Feature - List";
}
```

## Adding to Sidebar Navigation

The sidebar menu is driven by the `HousekeepingMenu` enum and the layout file. To add your feature to the sidebar:

1. Determine which menu section it belongs to (WebsiteAdmin, HobbaTools, AdminTools, etc.)
2. Add a menu item in the `_Housekeeping.cshtml` layout within the appropriate section:

```cshtml
@await Html.PartialAsync("_HousekeepingMenuItem", new HousekeepingMenuItemModel {
    Name = "My Feature",
    Url = "/Housekeeping/MyFeature",
    UserFuses = user.Fuses.Select(f => f.FuseName).ToList(),
    Fuses = new List<Fuse> { Fuse.housekeeping },
    IsActive = Context.Request.Path.StartsWithSegments("/Housekeeping/MyFeature")
})
```

## Menu Sections

| Section | Enum Value | Features |
|---------|-----------|----------|
| Home/Dashboard | `Home` | Statistics, overview |
| Website Management | `WebsiteAdmin` | News, pages, promos, settings |
| Hobba Tools | `HobbaTools` | Alerting, banning, moderation |
| Admin Tools | `AdminTools` | Users, rooms, catalogue, ranks |
| Localization | `Localization` | Translation management |

## Classic Habbo Housekeeping Design

The original Habbo Housekeeping had:
- Blue header bars (`background-color: #006699; color: white`) for section labels
- White background link lists below each header
- Active item highlighted
- Fuse-based visibility (items hidden if user lacks permission)
