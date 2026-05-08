# Habbo CMS Features Skill

A skill for Claude/OpenCode that provides comprehensive guidelines for building features in the KeplerCMS Habbo Hotel Content Management System.

## What This Skill Covers

- Creating Housekeeping (admin panel) features
- ASP.NET Core MVC patterns (controllers, services, models, views)
- Entity Framework Core database models and migrations
- Access control via the Fuse permission system
- Integration with the Kepler Java game server (RabbitMQ/MUS)
- Habbo Hotel domain knowledge (game concepts, protocol)
- UI design based on original Habbo Housekeeping screenshots

## File Structure

```
habbo-cms-features/
├── SKILL.md              # Skill definition (frontmatter + overview)
├── AGENTS.md             # Full compiled guide for LLM consumption
├── metadata.json         # Machine-readable metadata
├── README.md             # This file
└── rules/                # Individual rule files
    ├── _sections.md      # Section metadata
    ├── _template.md      # Template for new rules
    ├── arch-*.md         # Architecture rules
    ├── ctrl-*.md         # Controller rules
    ├── svc-*.md          # Service rules
    ├── data-*.md         # Data model rules
    ├── view-*.md         # View/UI rules
    ├── server-*.md       # Server integration rules
    └── game-*.md         # Game knowledge rules
```

## Referenced Resources

| Resource | Path |
|----------|------|
| KeplerCMS | `C:\Users\Patrick\Documents\github\KeplerCMS` |
| Kepler Server | `C:\Users\Patrick\Documents\github\Kepler` |
| Habbo Client Source | `C:\Users\Patrick\Documents\forgejo\habbo-vibe-playground\decompiled` |
| UI Screenshots | `C:\Users\Patrick\Pictures\housekeeping stuff` |

## Adding New Rules

1. Copy `rules/_template.md` to `rules/{prefix}-{name}.md`
2. Fill in the frontmatter (title, impact, tags)
3. Write the rule content with examples
4. Add the rule to the Quick Reference in `SKILL.md`
5. Update `AGENTS.md` if the rule introduces major new patterns

## Maintenance

- When the KeplerCMS project structure changes, update `AGENTS.md`
- When new Fuse permissions are added, update `ctrl-access-control.md`
- When new audit log types are added, update `svc-audit-logging.md`
