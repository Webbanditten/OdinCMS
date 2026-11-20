-- =====================================================================
-- RELEASE MIGRATION - 2026-11-20
-- Website reports: the Habbo Home's and group page's "Rapporter" mode, Housekeeping > Hobba tools > Content moderation
-- =====================================================================
-- cms_reports: one row per report (ReportController, /mod/add_TYPE_report): what was reported (type + object id), the page
-- it was on, its writer, the text as it was, the reporter; staff archive it with an outcome (ReportsController).
-- Texts in set "report" (Helpers/ReportTexts.cs falls back to the same Danish while a row is missing).
--
-- Fully idempotent. Rollback: DROP TABLE cms_reports; DELETE FROM cms_localizations WHERE ResourceSet = 'report';
-- =====================================================================

CREATE TABLE IF NOT EXISTS `cms_reports` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `type` VARCHAR(20) NOT NULL,
    `object_id` INT NOT NULL,
    `home_id` INT NULL,
    `author_id` INT NULL,
    `content` TEXT NOT NULL,
    `reporter_id` INT NOT NULL,
    `ip` VARCHAR(45) NULL,
    `status` TINYINT NOT NULL DEFAULT 0,
    `outcome` VARCHAR(255) NULL,
    `closed_by` INT NULL,
    `closed_at` DATETIME NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `cms_reports_type_status` (`type`, `status`, `id`),
    KEY `cms_reports_object` (`type`, `object_id`, `status`),
    KEY `cms_reports_reporter` (`reporter_id`, `created_at`),
    KEY `cms_reports_author` (`author_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'button_start', 'Rapporter', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'button_start' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'button_stop', 'Stop rapportering', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'button_stop' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'dialog_report', 'Rapporter', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'dialog_report' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'dialog_cancel', 'Annuller', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'dialog_cancel' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'dialog_ok', 'OK', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'dialog_ok' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'name_title', 'Rapporter Habbo-navn', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'name_title' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'name_message', 'Synes du, at dette Habbo-navn er stødende? Så kan du rapportere det til vores moderatorer.', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'name_message' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'motto_title', 'Rapporter motto', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'motto_title' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'motto_message', 'Synes du, at dette motto er stødende eller chikanerende? Så kan du rapportere det til vores moderatorer.', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'motto_message' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'stickie_title', 'Rapporter note', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'stickie_title' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'stickie_message', 'Synes du, at denne note er stødende eller chikanerende? Så kan du rapportere den til vores moderatorer.', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'stickie_message' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'guestbook_title', 'Rapporter indlæg', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'guestbook_title' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'guestbook_message', 'Synes du, at dette indlæg i gæstebogen er stødende eller chikanerende? Så kan du rapportere det til vores moderatorer.', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'guestbook_message' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'room_title', 'Rapporter rum', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'room_title' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'room_message', 'Synes du, at rummets navn eller beskrivelse er stødende? Så kan du rapportere det til vores moderatorer.', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'room_message' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'groupname_title', 'Rapporter gruppenavn', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'groupname_title' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'groupname_message', 'Synes du, at gruppens navn er stødende? Så kan du rapportere det til vores moderatorer.', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'groupname_message' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'groupdesc_title', 'Rapporter gruppebeskrivelse', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'groupdesc_title' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'groupdesc_message', 'Synes du, at gruppens beskrivelse er stødende eller chikanerende? Så kan du rapportere den til vores moderatorer.', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'groupdesc_message' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'success_title', 'Tak for din rapport', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'success_title' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'success_message', 'Vores moderatorer kigger på det så hurtigt som muligt.', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'success_message' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'spam_title', 'Allerede rapporteret', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'spam_title' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'spam_message', 'Du har allerede rapporteret dette, eller du har sendt mange rapporter på kort tid. Prøv igen senere.', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'spam_message' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'error_title', 'Det gik galt', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'error_title' AND ResourceSet = 'report' AND LocaleId = 'da-DK');

INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT 'error_message', 'Din rapport kunne ikke sendes. Prøv igen senere.', 'da-DK', 'report', 0 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations WHERE ResourceId = 'error_message' AND ResourceSet = 'report' AND LocaleId = 'da-DK');
