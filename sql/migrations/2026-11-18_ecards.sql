-- =====================================================================
-- RELEASE MIGRATION - 2026-11-18
-- The 2006 Christmas e-card (/xmas/ecard, EcardController)
-- =====================================================================
-- One row per card sent: its share code (/xmas/ecard?card=CODE), the sender (when logged in), the Habbo on it, the
-- sender's and the friend's name (and the friend's Habbo name), the headline and message, and the card's own XML (which the card loads again). No e-mail addresses.
-- Rollback: DROP TABLE cms_ecards;
-- =====================================================================

CREATE TABLE IF NOT EXISTS `cms_ecards` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `code` VARCHAR(16) NOT NULL,
    `user_id` INT NULL,
    `habbo_name` VARCHAR(50) NULL,
    `from_name` VARCHAR(100) NULL,
    `to_name` VARCHAR(100) NULL,
    `to_habbo` VARCHAR(50) NULL,
    `headline` VARCHAR(255) NULL,
    `message` TEXT NULL,
    `card_xml` MEDIUMTEXT NOT NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `cms_ecards_code` (`code`),
    KEY `cms_ecards_user` (`user_id`, `created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- The page's texts (Views/Ecard/Index.cshtml; the code has the same Danish fallbacks). The card's own labels are in
-- wwwroot/flash/xmas06/ecard_da.xml.
INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT x.id, x.value, 'da-DK', 'ecard', 0
FROM (
    SELECT 'mine_title' AS id, 'Dine julekort' AS value
    UNION ALL SELECT 'mine_text', 'Når du har sendt et kort, kommer det her. Giv linket til din ven, så kan de se kortet.'
    UNION ALL SELECT 'mine_none', 'Du har ikke sendt nogen kort endnu.'
    UNION ALL SELECT 'to', 'til'
) x
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations l WHERE l.ResourceId = x.id AND l.ResourceSet = 'ecard' AND l.LocaleId = 'da-DK');
