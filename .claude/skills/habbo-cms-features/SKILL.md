---
name: habbo-cms-features
description: Guidelines for building Habbo Hotel CMS features in the KeplerCMS project (ASP.NET Core MVC). Covers creating Housekeeping admin tools, front-end pages, services, database models, and integration with the Kepler Java game server. Triggers on tasks involving Habbo Hotel CMS development, admin panels, housekeeping tools, moderation features, or game server integration.
license: MIT
metadata:
  author: patrick
  version: "1.0.0"
---

# Habbo Hotel CMS Feature Development

Comprehensive guide for building features in KeplerCMS — a Habbo Hotel retro Content Management System built with ASP.NET Core 6 MVC, Razor views, MySQL (Entity Framework Core), and integrated with the Kepler Java game server via RabbitMQ/MUS protocol.

## When to Apply

Reference these guidelines when:
- Creating new Housekeeping (admin panel) features
- Building front-end CMS pages for users
- Adding new database models or services
- Integrating CMS features with the Kepler game server
- Building moderation tools (banning, alerting, user management)
- Creating catalogue, room, or furniture management tools
- Implementing user-facing features (profiles, groups, news)
- Working with the Habbo game protocol or decompiled client source

## Reference Locations

| Resource | Path |
|----------|------|
| KeplerCMS Project | `C:\Users\Patrick\Documents\github\KeplerCMS` |
| Kepler Server (Java) | `C:\Users\Patrick\Documents\github\Kepler` |
| Habbo Decompiled Client | `C:\Users\Patrick\Documents\forgejo\habbo-vibe-playground\decompiled` |
| UI Design Screenshots | `C:\Users\Patrick\Pictures\housekeeping stuff` |

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Language | C# (.NET 6.0) |
| Framework | ASP.NET Core MVC |
| Views | Razor (.cshtml) |
| Database | MySQL via Entity Framework Core |
| Auth | Cookie-based with Fuse (permission) system |
| Real-time | SignalR |
| Game Server Comms | RabbitMQ + MUS protocol |
| Rich Text | TinyMCE |
| Dialogs | SweetAlert2 |
| Password Hashing | Argon2 |

## Rule Categories

| Priority | Category | Prefix | Description |
|----------|----------|--------|-------------|
| 1 | Architecture | `arch-` | Project structure, areas, DI registration |
| 2 | Controllers | `ctrl-` | Controller patterns, filters, routing |
| 3 | Services | `svc-` | Service layer patterns, interfaces |
| 4 | Data Models | `data-` | EF Core models, DbContext, migrations |
| 5 | Views | `view-` | Razor views, layouts, partials |
| 6 | Server Integration | `server-` | Kepler game server communication |
| 7 | Game Knowledge | `game-` | Habbo game concepts and protocol |

## Quick Reference - All Rules

### Architecture (`arch-`)
- `arch-area-structure` - How to organize features in MVC Areas
- `arch-dependency-injection` - Service registration in Startup.cs
- `arch-project-layout` - Overall project file organization

### Controllers (`ctrl-`)
- `ctrl-housekeeping` - Housekeeping controller patterns
- `ctrl-access-control` - HousekeepingFilter and Fuse permissions
- `ctrl-crud-pattern` - Standard CRUD action patterns
- `ctrl-api-endpoints` - JSON API endpoint patterns

### Services (`svc-`)
- `svc-interface-pattern` - Service interface conventions
- `svc-implementation` - Service implementation patterns
- `svc-audit-logging` - Audit log integration

### Data Models (`data-`)
- `data-ef-models` - Entity Framework model conventions
- `data-dbcontext` - DbContext registration and DbSets
- `data-migrations` - SQL migration patterns

### Views (`view-`)
- `view-layout` - Housekeeping layout and navigation
- `view-forms` - Form patterns and tag helpers
- `view-tables` - Data table patterns
- `view-scripts` - Page-specific JavaScript patterns

### Server Integration (`server-`)
- `server-rabbitmq` - RabbitMQ command publishing
- `server-mus` - MUS protocol communication
- `server-packets` - Game protocol packet reference

### Game Knowledge (`game-`)
- `game-concepts` - Habbo game domain concepts
- `game-protocol` - Client-server message protocol
- `game-client-source` - Using the decompiled Lingo source

## How to Use

1. Read `AGENTS.md` for the full compiled guide with all rules expanded
2. Reference individual rule files in `rules/` for specific patterns
3. Check UI screenshots at the reference path for design guidance
