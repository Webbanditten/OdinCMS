# Habbo Hotel CMS Feature Development - Complete Guide

This document contains the full compiled guide for building features in the KeplerCMS Habbo Hotel Content Management System. Follow these patterns when creating any new CMS feature.

---

## 1. Architecture Overview

### Project Structure

```
KeplerCMS/
├── Controllers/              # Main site controllers
├── Views/                    # Main site Razor views
├── Areas/
│   ├── Housekeeping/         # Admin panel (primary feature area)
│   │   ├── Controllers/
│   │   ├── Views/
│   │   ├── Models/
│   │   │   ├── Views/        # View models
│   │   │   └── Enums/        # Menu enums etc.
│   │   └── Helpers/
│   ├── Habbowood/            # Flash movie editor
│   └── MyHabbo/              # User profiles
├── Services/
│   ├── Interfaces/           # Service contracts
│   └── Implementations/      # Service implementations
├── Data/
│   ├── DataContext.cs        # EF Core DbContext
│   ├── Models/               # Database entity models
│   └── Statics/              # Static data (e.g., default settings)
├── Models/                   # Shared view models / DTOs
├── Filters/                  # MVC action filters
├── Helpers/                  # Utility classes
├── Hubs/                     # SignalR hubs
├── BackgroundServices/       # Hosted background tasks
├── wwwroot/                  # Static files (CSS, JS, images, SWF)
├── sql/                      # Database migration SQL files
├── Startup.cs                # DI, middleware, routing configuration
└── Program.cs                # Entry point
```

### Key Architectural Decisions

1. **Service-oriented MVC** — Controllers never access the database directly. All data access goes through service interfaces.
2. **Area-based feature isolation** — Large features (Housekeeping, MyHabbo) live in their own Area with dedicated controllers, views, and models.
3. **Permission-based access** — The "Fuse" system controls who can access what. Each action is protected by a `[HousekeepingFilter(Fuse.xxx)]` attribute.
4. **Audit trail** — All create/update/delete operations log to the audit system via `IAuditLogService`.

---

## 2. Creating a New Housekeeping Feature (Step-by-Step)

### Step 1: Define the Data Model

Create your entity in `Data/Models/`:

```csharp
// Data/Models/MyFeature.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    [Table("my_features")]  // MySQL table name (snake_case)
    public class MyFeature
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        public int CreatedBy { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public bool IsActive { get; set; } = true;
    }
}
```

### Step 2: Register in DbContext

Add a `DbSet` in `Data/DataContext.cs`:

```csharp
public DbSet<MyFeature> MyFeatures { get; set; }
```

### Step 3: Create the Service Interface

```csharp
// Services/Interfaces/IMyFeatureService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Services.Interfaces
{
    public interface IMyFeatureService
    {
        Task<IEnumerable<MyFeature>> GetAll();
        Task<MyFeature> GetById(int id);
        Task<MyFeature> Create(MyFeature model);
        Task<MyFeature> Update(MyFeature model);
        Task Remove(int id);
    }
}
```

### Step 4: Create the Service Implementation

```csharp
// Services/Implementations/MyFeatureService.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Services.Implementations
{
    public class MyFeatureService : IMyFeatureService
    {
        private readonly DataContext _context;

        public MyFeatureService(DataContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MyFeature>> GetAll()
        {
            return await _context.MyFeatures
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<MyFeature> GetById(int id)
        {
            return await _context.MyFeatures.FindAsync(id);
        }

        public async Task<MyFeature> Create(MyFeature model)
        {
            _context.MyFeatures.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<MyFeature> Update(MyFeature model)
        {
            var existing = await _context.MyFeatures.FindAsync(model.Id);
            if (existing == null) return null;
            
            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.IsActive = model.IsActive;
            
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task Remove(int id)
        {
            var item = await _context.MyFeatures.FindAsync(id);
            if (item != null)
            {
                _context.MyFeatures.Remove(item);
                await _context.SaveChangesAsync();
            }
        }
    }
}
```

### Step 5: Register in Startup.cs

In the `ConfigureServices` method of `Startup.cs`:

```csharp
services.AddScoped<IMyFeatureService, MyFeatureService>();
```

### Step 6: Create the Controller

```csharp
// Areas/Housekeeping/Controllers/MyFeatureController.cs
using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class MyFeatureController : Controller
    {
        private readonly IMyFeatureService _myFeatureService;
        private readonly IAuditLogService _auditService;

        public MyFeatureController(IMyFeatureService myFeatureService, IAuditLogService auditService)
        {
            _myFeatureService = myFeatureService;
            _auditService = auditService;
        }

        [HousekeepingFilter(Fuse.housekeeping)]
        public async Task<IActionResult> Index()
        {
            var items = await _myFeatureService.GetAll();
            return View(items);
        }

        [HousekeepingFilter(Fuse.housekeeping)]
        public IActionResult Create()
        {
            return View();
        }

        [HousekeepingFilter(Fuse.housekeeping)]
        [HttpPost]
        public async Task<IActionResult> Create(MyFeature model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedBy = int.Parse(HttpContext.User.Identity.Name);
                await _myFeatureService.Create(model);
                await _auditService.AddLog(
                    AuditLogType.create_news, // Use appropriate log type
                    int.Parse(HttpContext.User.Identity.Name), 0);
                return RedirectToAction("Index", new { message = "Created successfully" });
            }
            return View(model);
        }

        [HousekeepingFilter(Fuse.housekeeping)]
        public async Task<IActionResult> Update(int id)
        {
            var item = await _myFeatureService.GetById(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HousekeepingFilter(Fuse.housekeeping)]
        [HttpPost]
        public async Task<IActionResult> Update(MyFeature model)
        {
            if (ModelState.IsValid)
            {
                await _myFeatureService.Update(model);
                await _auditService.AddLog(
                    AuditLogType.edit_news, // Use appropriate log type
                    int.Parse(HttpContext.User.Identity.Name), null, null, 0, model.Id);
                return RedirectToAction("Index", new { message = "Updated successfully" });
            }
            return View(model);
        }

        [HousekeepingFilter(Fuse.housekeeping)]
        public async Task<IActionResult> Remove(int id)
        {
            await _myFeatureService.Remove(id);
            await _auditService.AddLog(
                AuditLogType.delete_news, // Use appropriate log type
                int.Parse(HttpContext.User.Identity.Name), null, null, 0, id);
            return RedirectToAction("Index", new { message = "Removed successfully" });
        }
    }
}
```

### Step 7: Create the Views

Views go in `Areas/Housekeeping/Views/MyFeature/`.

**Index.cshtml** (List view):
```cshtml
@model IEnumerable<KeplerCMS.Data.Models.MyFeature>

@{
    ViewBag.Title = "My Feature Management";
}

@if (!string.IsNullOrEmpty(Context.Request.Query["message"]))
{
    <div class="alert alert-success">@Context.Request.Query["message"]</div>
}

<h2>My Features</h2>
<a href="@Url.Action("Create")" class="btn btn-primary">Create New</a>

<table class="table mt-3">
    <thead>
        <tr>
            <th>ID</th>
            <th>Name</th>
            <th>Created</th>
            <th>Active</th>
            <th>Actions</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var item in Model)
        {
            <tr>
                <td>@item.Id</td>
                <td>@item.Name</td>
                <td>@item.CreatedAt.ToString("yyyy-MM-dd HH:mm")</td>
                <td>@(item.IsActive ? "Yes" : "No")</td>
                <td>
                    <a href="@Url.Action("Update", new { id = item.Id })">Edit</a> |
                    <a href="@Url.Action("Remove", new { id = item.Id })" 
                       onclick="return confirm('Are you sure?')">Delete</a>
                </td>
            </tr>
        }
    </tbody>
</table>
```

**Create.cshtml** (Create form):
```cshtml
@model KeplerCMS.Data.Models.MyFeature

@{
    ViewBag.Title = "Create My Feature";
}

<h2>Create My Feature</h2>

<form asp-action="Create" method="post">
    <div class="form-group">
        <label asp-for="Name"></label>
        <input asp-for="Name" class="form-control" />
        <span asp-validation-for="Name" class="text-danger"></span>
    </div>
    <div class="form-group">
        <label asp-for="Description"></label>
        <textarea asp-for="Description" class="form-control" rows="5"></textarea>
    </div>
    <div class="form-group">
        <label asp-for="IsActive"></label>
        <input asp-for="IsActive" type="checkbox" />
    </div>
    <button type="submit" class="btn btn-success">Create</button>
    <a href="@Url.Action("Index")" class="btn btn-secondary">Cancel</a>
</form>
```

### Step 8: Add Menu Navigation

Update the Housekeeping layout or menu helper to include navigation to your new feature. Add an entry in the sidebar menu in `_Housekeeping.cshtml` or register it in the `HousekeepingMenu` enum if it warrants its own section.

### Step 9: Create SQL Migration

Add a SQL file in `sql/` for the table creation:

```sql
-- sql/my_features.sql
CREATE TABLE IF NOT EXISTS `my_features` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `Name` VARCHAR(100) NOT NULL,
    `Description` TEXT NULL,
    `CreatedBy` INT NOT NULL,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    INDEX `idx_created_by` (`CreatedBy`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

## 3. Access Control (Fuse System)

### How Fuses Work

The permission system uses "fuses" — named permission strings assigned to ranks. Each rank has a set of fuses that determine what actions users of that rank can perform.

### Using HousekeepingFilter

```csharp
// Single fuse requirement
[HousekeepingFilter(Fuse.housekeeping)]

// The filter:
// 1. Checks if user is authenticated
// 2. Loads the user from database
// 3. Injects user into ViewData["user"]
// 4. Checks if user's rank has the required fuse
// 5. Returns Challenge() (403) if not authorized
```

### Available Fuse Categories

- `Fuse.housekeeping` — Base housekeeping access
- `Fuse.housekeeping_news` — News management
- `Fuse.fuse_kick` — Kick/user management
- `Fuse.fuse_ban` — Ban management
- `Fuse.fuse_alert` — Alert sending
- `Fuse.fuse_badges` — Badge management
- `Fuse.fuse_catalogue_manager` — Catalogue editing

### Adding New Fuses

If your feature needs a new permission, add it to the `Fuse` enum in `Models/Fuse.cs` with a `[Description("fuse_name")]` attribute.

---

## 4. UI Design Patterns

### Classic Habbo Housekeeping Style

Based on the original Habbo Hotel housekeeping screenshots, the UI follows these patterns:

#### Layout Structure
- **Top bar**: Blue gradient with yellow stripe, showing logged-in user and logout link
- **Left sidebar**: Blue-header sections with white backgrounds containing navigation links
- **Main content area**: White background, left-aligned content

#### Sidebar Navigation Categories
Sections have blue header bars with white text:
- "Hobba discussion board" (Forums)
- "Hobba tools" (Remote alerting, Remote banning and kicking, List of current bans)
- "Supervisor Hobba tools" (Hobba activity log, Mass ban, Mass alert)
- "Scam detection" (Habbo search & information tool, Purchases/phone, Current furniture)
- "Private rooms" (Room admin, Room action log)
- "Staff moderator tools" (Mass unban, Habbo restore tool, Habbo name changer)
- "Customer service" (Feedback messages, Bug reports, Habbo problem reports, Calls for help, Reported messages)

#### Tables
- **Yellow/gold header row** with bold text
- **Alternating yellow-highlighted rows** for data
- Columns: sortable, clickable links for navigation
- Standard columns: Username, Email, IP, Last access, Access count, Actions

#### Forms
- Simple HTML forms with labels on the left, inputs on the right
- Text inputs, dropdowns (`<select>`), textareas, checkboxes
- Action buttons: "Alert", "Ban", "Kick", "Search", "Query"
- Confirmation messages displayed inline after actions ("Gave alert to [username]")
- "Choose a common message" dropdown for pre-filled messages
- Ban time dropdown (24 hours, 100000 hours, etc.)
- "Extra info" textarea for additional notes

#### Moderation Tools Pattern
- **Remote alerting**: Recipient field + message dropdown + custom message + Alert button
- **Remote banning**: Username field + message + ban time dropdown + extra info + Ban/Kick buttons
- **Mass banning**: Multi-select list of users + message + ban time + checkboxes for IP/machine ban
- **User search**: Search by name/letter with paginated results table
- **Reports view**: Table with Caller, Time, Room, Category, Message, Picked by columns

#### Colors
- Blue headers: `#006699` or similar deep blue
- Yellow/gold highlights: `#FFCC00` / `#FFD700` for table rows
- White backgrounds for content areas
- Red text for warnings/errors
- Green text for success confirmations
- Link blue for clickable items

---

## 5. Kepler Game Server Integration

### Communication Methods

The CMS communicates with the Kepler Java game server through:

1. **RabbitMQ** — For sending commands/events to the game server
2. **MUS (Multi User Server)** — Secondary TCP socket for real-time housekeeping operations
3. **Shared Database** — Both CMS and game server read/write the same MySQL database

### RabbitMQ Integration

```csharp
// Publishing a command to the game server via RabbitMQ
// The game server's CommandQueueManager consumes these messages

// Example: Alerting a user in-game
public async Task AlertUser(string username, string message)
{
    var command = new { action = "alert", username = username, message = message };
    var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(command));
    
    _channel.BasicPublish(
        exchange: "commands",
        routingKey: "",
        basicProperties: null,
        body: body);
}
```

### MUS Protocol

The MUS server in Kepler listens on a separate port and accepts commands from trusted hosts (the CMS). Common operations:

- Send user alerts
- Disconnect/kick users
- Refresh catalogue
- Update room data
- Broadcast messages

### Shared Database Operations

Since both systems share the same MySQL database, the CMS can directly modify game data:

- User accounts, ranks, badges
- Room settings, room models
- Catalogue pages, items, prices
- Bans, moderation logs
- News/announcements that the game client reads

---

## 6. Habbo Game Domain Knowledge

### Core Concepts

| Concept | Description |
|---------|-------------|
| **Habbo** | A user/player account |
| **Credits** | In-game currency |
| **Habbo Club** | Premium membership |
| **Furni** | Furniture items that can be placed in rooms |
| **Room** | A virtual space users can visit and decorate |
| **Navigator** | Room browser/search |
| **Catalogue** | In-game shop for purchasing furni |
| **Messenger** | Friend list and messaging system |
| **Hobba** | Volunteer moderator |
| **Fuse** | Permission/capability assigned to a rank |
| **Badge** | Visible achievement/role indicator |
| **Trade** | Item exchange between users |
| **Stickies** | Post-it note furniture |
| **Dice** | Interactive rolling dice furniture |
| **Teleporter** | Furniture that teleports between rooms |
| **Pets** | Virtual pets that live in rooms |
| **BattleBall/SnowWar** | Mini-games |
| **Infobus** | Special event room |

### Moderation System

The Habbo moderation hierarchy:
1. **Normal user** — No special powers
2. **Hobba** (volunteer moderator) — Can alert, kick from rooms, basic moderation
3. **Supervisor Hobba** — Mass ban, activity logs, escalated tools
4. **Staff** — Full admin access (room editing, user management, catalogue)
5. **Admin** — Everything including system settings

### Network Protocol

The Kepler server uses a custom binary TCP protocol:
- **Framing**: 3-byte Base64-encoded length prefix
- **Header**: 2-byte Base64-encoded message ID
- **Payload**: Mixed format (strings terminated by char(2), integers as VL64/Base64)

Client-to-server packets are defined in `messages/incoming/` and server-to-client responses in `messages/outgoing/`.

---

## 7. Using the Decompiled Habbo Source

### Location and Structure

The decompiled Shockwave/Director client source is at:
`C:\Users\Patrick\Documents\forgejo\habbo-vibe-playground\decompiled`

### File Types
- `.ls` — Lingo Script (main logic files)
- `.txt` — Window layout definitions (XML-like UI markup)
- `.png` — UI graphics
- `Members.csv` — Cast member index

### Key Modules to Reference

| Directory | Contains |
|-----------|----------|
| `fuse_client/` | Core networking, RC4 crypto, object management |
| `hh_room/` | Room engine, pathfinding, chat |
| `hh_navigator/` | Room browser logic |
| `hh_messenger/` | Friend list, messaging |
| `hh_cat_code/` | Catalogue/shop system |
| `hh_furni_classes/` | All furniture behaviors |
| `hh_human/` | Avatar rendering |
| `hh_shared/` | Common utilities, window system |

### Lingo Script Patterns

```lingo
-- Property declarations (instance variables)
property pRoomID, pRoomName, pUsers

-- Constructor
on construct me
  pRoomID = 0
  pRoomName = ""
  pUsers = []
  return 1
end

-- Message handler (processes server packets)
on handle_room_info me, tMsg
  tConn = tMsg.connection
  pRoomID = tConn.GetIntFrom()
  pRoomName = tConn.GetStrFrom()
  return 1
end

-- Inter-module communication
on sendRoomData me
  executeMessage(#cycleNavigation, [#id: pRoomID])
end
```

### When to Reference the Client Source

- Understanding what data the game client expects/sends
- Determining packet structures for new features
- Understanding game mechanics (e.g., how furniture interactions work)
- Replicating client-side validation logic server-side
- Building CMS tools that match in-game behavior

---

## 8. Common Patterns and Anti-Patterns

### DO:
- Always use service interfaces (never inject implementations directly)
- Always add `[HousekeepingFilter]` to admin actions
- Always log admin actions via `IAuditLogService`
- Use `async/await` throughout the stack
- Use view models for complex views (don't pass raw entities when multiple data sources are needed)
- Use `RedirectToAction` after POST operations (PRG pattern)
- Validate `ModelState.IsValid` before processing form submissions
- Use `int.Parse(HttpContext.User.Identity.Name)` to get current user ID
- Use snake_case for MySQL table/column names in `[Table]`/`[Column]` attributes
- Add indexes on frequently-queried columns in SQL migrations

### DON'T:
- Don't access `DataContext` directly from controllers
- Don't skip the `[HousekeepingFilter]` attribute
- Don't return views after POST (always redirect)
- Don't hardcode permission checks — use the Fuse system
- Don't put business logic in controllers — delegate to services
- Don't forget to register new services in `Startup.cs`
- Don't use synchronous database calls
- Don't expose internal IDs or sensitive data in URLs without permission checks

---

## 9. Database Naming Conventions

- Table names: `snake_case` (e.g., `my_features`, `users_badges`)
- Column names: `PascalCase` in C# model, mapped to `snake_case` via EF conventions or `[Column("name")]`
- Primary keys: `Id` (auto-increment integer)
- Foreign keys: `{RelatedEntity}Id` (e.g., `UserId`, `RoomId`)
- Boolean fields: `Is{Adjective}` (e.g., `IsActive`, `IsDeleted`)
- Timestamps: `CreatedAt`, `UpdatedAt`

---

## 10. Testing Your Feature

1. **Build**: Run `dotnet build` to verify compilation
2. **Database**: Apply SQL migration manually or via EF migrations
3. **Access**: Navigate to `/Housekeeping/YourController` after logging in with an admin account
4. **Permissions**: Verify that users without the required fuse cannot access the feature
5. **Audit**: Check that admin actions appear in the audit log
6. **Server Integration**: If the feature communicates with the game server, test with Kepler running
