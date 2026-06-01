-- =====================================================================
-- Scheduled visibility for page containers, promos and front-end menu items
-- =====================================================================
-- Adds optional start/end date windows so containers, promos and menu
-- items can be shown only during a set period (e.g. seasonal / Christmas
-- content).
--
-- Visibility rules applied by the CMS at render time:
--   * Containers: the existing `hidden` flag is a hard off-switch. When a
--     container is NOT hidden, it shows only if the current time is within
--     its start/end window. An empty bound is open-ended; bounds inclusive.
--   * Promos: same rule as containers (`hidden` wins, then the window).
--   * Menu items: shown only within their start/end window (open-ended if
--     a bound is empty). Existing `state` (auth) behaviour is unchanged.
--
-- Idempotent: uses ADD COLUMN IF NOT EXISTS (MariaDB) — safe to re-run.
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
