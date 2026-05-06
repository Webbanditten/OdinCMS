---
title: Entity Framework Models
impact: HIGH
impactDescription: Incorrect models cause database schema mismatches and runtime errors
tags: [data, entity-framework, models, database]
---

# Entity Framework Models

Database entities are defined in `Data/Models/` and mapped to MySQL tables.

## Model Template

```csharp
// Data/Models/MyFeature.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    [Table("my_features")]
    public class MyFeature
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Column("created_by")]
        public int CreatedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        // Navigation property (optional)
        [ForeignKey("CreatedBy")]
        public Users CreatedByUser { get; set; }
    }
}
```

## Rules

1. Use `[Table("snake_case_name")]` for table mapping
2. Use `[Key]` on the primary key property
3. Use `[Required]` and `[StringLength(n)]` for validation
4. Use `[Column("snake_case")]` when C# property name differs from DB column
5. Default values set in C# (e.g., `= DateTime.Now`) for create operations
6. Navigation properties are optional — only add if you need `Include()` queries
7. Use nullable types (`int?`, `DateTime?`) for optional columns
8. Enum backing fields should be `int` in the database

## Registering in DbContext

```csharp
// In Data/DataContext.cs:
public DbSet<MyFeature> MyFeatures { get; set; }
```

## Naming Conventions

| C# | MySQL |
|----|-------|
| `PascalCase` property | `snake_case` column |
| `MyFeature` class | `my_features` table |
| `int Id` | `id` INT AUTO_INCREMENT PRIMARY KEY |
| `bool IsActive` | `is_active` TINYINT(1) |
| `DateTime CreatedAt` | `created_at` DATETIME |
