-- =====================================================================
-- RELEASE MIGRATION - 2026-11-18
-- The Habbo Help Tool (/iot/go) and its tickets (Housekeeping > Help tickets)
-- =====================================================================
-- A ticket per question (who, which problem, the message, a secret token for the player's link) and the replies on it
-- from staff or the player. Status: 0 new, 1 answered, 2 the player replied, 3 closed.
-- Rollback: DROP TABLE cms_help_ticket_responses; DROP TABLE cms_help_tickets;
-- =====================================================================

CREATE TABLE IF NOT EXISTS `cms_help_tickets` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `user_id` INT NULL,
    `name` VARCHAR(50) NULL,
    `email` VARCHAR(255) NULL,
    `issue` VARCHAR(20) NOT NULL,
    `message` TEXT NOT NULL,
    `token` VARCHAR(64) NOT NULL,
    `status` TINYINT NOT NULL DEFAULT 0,
    `ip` VARCHAR(45) NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `cms_help_tickets_status` (`status`, `updated_at`),
    KEY `cms_help_tickets_user` (`user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `cms_help_ticket_responses` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `ticket_id` INT NOT NULL,
    `staff_id` INT NULL,
    `message` TEXT NOT NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `cms_help_ticket_responses_ticket` (`ticket_id`, `id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- "Kontakt Habbo" in the Hjælp menu (parent 7 = Hjælp), after its pages. Rollback: DELETE FROM cms_menu WHERE href = '/iot/go';
INSERT INTO cms_menu (parent, `order`, page, icon, state, text, href)
SELECT 7, 7, 0, 'spaceexplore_tab', 'display', 'Kontakt Habbo', '/iot/go' FROM DUAL
WHERE EXISTS (SELECT 1 FROM cms_menu WHERE id = 7) AND NOT EXISTS (SELECT 1 FROM cms_menu WHERE href = '/iot/go');

-- Texts (the code has the same Danish fallbacks): the help tool's title and FAQ note, and the e-mail staff's answer sends.
INSERT INTO cms_localizations (ResourceId, Value, LocaleId, ResourceSet, ValueType)
SELECT x.id, x.value, 'da-DK', x.rs, 0
FROM (
    SELECT 'title' AS id, 'Habbo Hjælp' AS value, 'iot' AS rs
    UNION ALL SELECT 'faq', 'Husk, at svarene på de mest almindelige spørgsmål fra Habbo''er og deres forældre allerede står på vores <a href="/help">hjælpesider</a>. Står svaret der, svarer vi ikke på din henvendelse, så vi kan nå de andre.', 'iot'
    UNION ALL SELECT 'help_reply_subject', 'Svar på din henvendelse til Habbo', 'emails'
    UNION ALL SELECT 'help_reply_header', 'Du har fået svar fra Habbo', 'emails'
    UNION ALL SELECT 'help_reply_body', 'Vi har svaret på din henvendelse til Habbo Hjælp. Læs svaret og skriv tilbage her: {link}', 'emails'
) x
WHERE NOT EXISTS (SELECT 1 FROM cms_localizations l WHERE l.ResourceId = x.id AND l.ResourceSet = x.rs AND l.LocaleId = 'da-DK');
