-- =====================================================================
-- RELEASE MIGRATION - 2026-11-20
-- Housekeeping staff tools (after Sulake's housekeeping): staff notes on a Habbo
-- =====================================================================
-- cms_user_notes: Housekeeping > a Habbo's page > Staff notes (UserToolsController). The other new pages read existing
-- tables (credit_log, items_pets, tags, cms_homes, users_room_votes, calls_for_help) and Odin's banned_login_attempts
-- (Odin/tools/migrations/update.1.49.sql, run that one first).
--
-- Fully idempotent. Rollback: DROP TABLE cms_user_notes;
-- =====================================================================

CREATE TABLE IF NOT EXISTS `cms_user_notes` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `user_id` INT NOT NULL,
    `staff_id` INT NOT NULL,
    `note` TEXT NOT NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `cms_user_notes_user` (`user_id`, `id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
