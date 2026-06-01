-- =====================================================================
-- Campaign Management System — Production Migration
-- =====================================================================
-- Run this ONCE on a production database that does NOT yet have the
-- campaign feature. It is fully idempotent (safe to re-run): every
-- statement uses IF NOT EXISTS / INSERT IGNORE / ON DUPLICATE KEY, so
-- it will not error or duplicate data if parts already exist.
--
-- What it creates:
--   1. cms_campaigns           — campaign definitions + scheduling/status
--   2. cms_campaign_actions    — ordered actions belonging to a campaign
--   3. cms_campaign_snapshots  — pre-activation state, for clean rollback
--   4. fuse_campaigns          — the permission that gates the admin UI
--   5. rank grants             — gives staff ranks access to that fuse
--
-- No existing tables are altered. The feature reads/writes existing
-- `settings`, `rooms`, `catalogue_pages`, and `news` rows at runtime,
-- but requires no schema changes to them.
--
-- NOTE: the new reload_catalogue / reload_navigator server commands are
-- RabbitMQ routing keys handled in the Kepler Java server — they need a
-- server rebuild/deploy, NOT a database change.
-- =====================================================================

-- ---------------------------------------------------------------------
-- 1. Campaign definitions
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `cms_campaigns` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(255) NOT NULL,
  `description` text DEFAULT NULL,
  `start_date` datetime DEFAULT NULL,
  `end_date` datetime DEFAULT NULL,
  `status` varchar(50) NOT NULL DEFAULT 'draft',
  `activated_at` datetime DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `created_by_id` int(11) NOT NULL,
  `cloned_from_id` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_status` (`status`),
  KEY `idx_start_date` (`start_date`),
  KEY `idx_end_date` (`end_date`),
  KEY `idx_created_by_id` (`created_by_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ---------------------------------------------------------------------
-- 2. Campaign actions (the changes a campaign applies)
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `cms_campaign_actions` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `campaign_id` int(11) NOT NULL,
  `action_type` varchar(100) NOT NULL,
  `action_data` text NOT NULL,
  `order_index` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`),
  KEY `idx_campaign_id` (`campaign_id`),
  CONSTRAINT `fk_campaign_actions_campaign` FOREIGN KEY (`campaign_id`) REFERENCES `cms_campaigns` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ---------------------------------------------------------------------
-- 3. Campaign snapshots (original values captured for rollback)
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `cms_campaign_snapshots` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `campaign_id` int(11) NOT NULL,
  `action_id` int(11) NOT NULL,
  `conflict_key` varchar(255) NOT NULL,
  `original_data` text NOT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_campaign_id` (`campaign_id`),
  KEY `idx_conflict_key` (`conflict_key`),
  CONSTRAINT `fk_campaign_snapshots_campaign` FOREIGN KEY (`campaign_id`) REFERENCES `cms_campaigns` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_campaign_snapshots_action` FOREIGN KEY (`action_id`) REFERENCES `cms_campaign_actions` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ---------------------------------------------------------------------
-- 4. Permission (fuse) that gates the Campaigns housekeeping UI
-- ---------------------------------------------------------------------
INSERT INTO `fuses` (`fuse`, `user_group`, `description`)
VALUES ('fuse_campaigns', 'CUSTOM', 'Can manage campaigns')
ON DUPLICATE KEY UPDATE `description` = VALUES(`description`);

-- ---------------------------------------------------------------------
-- 5. Grant the fuse to staff ranks.
--    Mirrors base `housekeeping` access, which is held by:
--      rank 1 = Hotel Manager, rank 2 = Community Manager
--    Adjust / remove rows below to match your hotel's rank layout.
--    (rank 3 = Habbo eXpert is intentionally excluded — campaigns
--     change live site-wide settings.)
-- ---------------------------------------------------------------------
INSERT IGNORE INTO `rank_rights` (`rank_id`, `fuse`) VALUES
  (1, 'fuse_campaigns'),
  (2, 'fuse_campaigns');

-- ---------------------------------------------------------------------
-- Verify (optional — run manually after the migration):
--   SHOW TABLES LIKE 'cms_campaign%';
--   SELECT * FROM `fuses` WHERE `fuse` = 'fuse_campaigns';
--   SELECT * FROM `rank_rights` WHERE `fuse` = 'fuse_campaigns';
-- =====================================================================
