---
title: SQL Migrations
impact: MEDIUM
impactDescription: Missing or incorrect SQL migrations prevent features from working in production
tags: [data, sql, migrations, mysql]
---

# SQL Migrations

Database schema changes are managed via SQL files in the `sql/` directory.

## Template

```sql
-- sql/my_features.sql
-- Description: Create table for [feature name]
-- Date: YYYY-MM-DD

CREATE TABLE IF NOT EXISTS `my_features` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `name` VARCHAR(100) NOT NULL,
    `description` TEXT NULL,
    `created_by` INT NOT NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    `is_active` TINYINT(1) NOT NULL DEFAULT 1,
    INDEX `idx_created_by` (`created_by`),
    INDEX `idx_is_active` (`is_active`),
    CONSTRAINT `fk_my_features_created_by` 
        FOREIGN KEY (`created_by`) REFERENCES `users`(`id`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

## Rules

1. Always use `CREATE TABLE IF NOT EXISTS` (idempotent)
2. Use `InnoDB` engine for foreign key support
3. Use `utf8mb4` charset for full Unicode support
4. Add indexes on columns used in WHERE clauses and foreign keys
5. Use `TINYINT(1)` for boolean fields
6. Use `DATETIME` for timestamps (not TIMESTAMP)
7. `AUTO_INCREMENT` on primary keys
8. Foreign keys with appropriate `ON DELETE` behavior
9. Include a comment header with description and date

## Adding Columns to Existing Tables

```sql
-- sql/alter_my_features_add_category.sql
ALTER TABLE `my_features` 
ADD COLUMN `category` VARCHAR(50) NULL AFTER `description`,
ADD INDEX `idx_category` (`category`);
```

## Seeding Data

```sql
-- Initial data for the feature
INSERT INTO `my_features` (`name`, `description`, `created_by`, `is_active`)
VALUES 
    ('Default Item', 'A default entry', 1, 1);
```
