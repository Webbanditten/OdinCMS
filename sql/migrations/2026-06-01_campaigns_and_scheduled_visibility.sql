-- =====================================================================
-- RELEASE MIGRATION — 2026-06-01
-- Campaign management system + scheduled visibility (containers/promos/menu)
-- =====================================================================
-- Run this ONCE on a production database that is on the version prior to
-- these features. It is the single, self-contained script for this release
-- and supersedes running sql/campaigns.sql and sql/scheduled_visibility.sql
-- individually (those remain as per-feature references).
--
-- Fully idempotent: every statement uses IF NOT EXISTS / INSERT IGNORE /
-- ON DUPLICATE KEY, so it is safe to re-run.
--
-- Sections:
--   1. Campaign system tables (cms_campaigns, _actions, _snapshots)
--   2. fuse_campaigns permission + staff rank grants
--   3. Scheduled-visibility columns on containers, promos and menu items
--
-- NOT covered here (deploy separately):
--   * CMS application binaries (compiled C#).
--   * Kepler Java server rebuild — the campaign reload_catalogue /
--     reload_navigator commands and room-data refresh are server code.
-- =====================================================================


-- =====================================================================
-- 1. Campaign system tables
-- =====================================================================
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


-- =====================================================================
-- 2. Permission (fuse) for the Campaigns housekeeping UI + rank grants
--    Granted to the same staff ranks that hold base `housekeeping`:
--      rank 1 = Hotel Manager, rank 2 = Community Manager
--    Adjust the rows below to match your hotel's rank layout.
-- =====================================================================
INSERT INTO `fuses` (`fuse`, `user_group`, `description`)
VALUES ('fuse_campaigns', 'CUSTOM', 'Can manage campaigns')
ON DUPLICATE KEY UPDATE `description` = VALUES(`description`);

INSERT IGNORE INTO `rank_rights` (`rank_id`, `fuse`) VALUES
  (1, 'fuse_campaigns'),
  (2, 'fuse_campaigns');


-- =====================================================================
-- 3. Scheduled visibility — optional start/end windows.
--    Containers/promos: existing `hidden` flag is a hard off-switch; when
--    not hidden, the item shows only within its window. Menu items show
--    only within their window. Empty bound = open-ended; bounds inclusive.
-- =====================================================================
ALTER TABLE `cms_containers`
  ADD COLUMN IF NOT EXISTS `start_date` datetime DEFAULT NULL,
  ADD COLUMN IF NOT EXISTS `end_date` datetime DEFAULT NULL;

ALTER TABLE `cms_promos`
  ADD COLUMN IF NOT EXISTS `start_date` datetime DEFAULT NULL,
  ADD COLUMN IF NOT EXISTS `end_date` datetime DEFAULT NULL;

ALTER TABLE `cms_menu`
  ADD COLUMN IF NOT EXISTS `start_date` datetime DEFAULT NULL,
  ADD COLUMN IF NOT EXISTS `end_date` datetime DEFAULT NULL;

-- =====================================================================
-- Verify (optional):
--   SHOW TABLES LIKE 'cms_campaign%';
--   SELECT * FROM `rank_rights` WHERE `fuse` = 'fuse_campaigns';
--   SHOW COLUMNS FROM `cms_containers` LIKE '%_date';
--   SHOW COLUMNS FROM `cms_promos`     LIKE '%_date';
--   SHOW COLUMNS FROM `cms_menu`       LIKE '%_date';
-- =====================================================================
