-- =====================================================================
-- RELEASE MIGRATION - 2026-11-17
-- Reward calendars: furni pools, calendar templates and a label on rewards
-- =====================================================================
-- Housekeeping > Reward management builds calendars from pools of furni and saved templates
-- (Areas/Housekeeping/Views/Rewards). The rewards table itself is unchanged except for `label`, which
-- names the calendar a reward came from; Odin keeps reading it as before.
--
-- Seeds:
--   * pools from the rewards made so far (credit furni, daily plants, weekend bonus, the penguin series,
--     the Christmas extras) and the seasonal themes from prod-staging/reward-themes/furni_themes.json
--     (Claude Opus 5.5, 2026-11-17; needs_review = 1 where staff should check);
--   * two templates: the penguin Christmas calendar exactly as in December 2025, and a month of daily gifts.
-- Furni missing from items_definitions are skipped.
--
-- Fully idempotent: IF NOT EXISTS / INSERT IGNORE, safe to re-run.
-- =====================================================================

CREATE TABLE IF NOT EXISTS `cms_reward_pools` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `description` varchar(255) NOT NULL DEFAULT '',
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `cms_reward_pool_items` (
  `pool_id` int(11) NOT NULL,
  `definition_id` int(11) NOT NULL,
  `sort_order` int(11) NOT NULL DEFAULT 0,
  `needs_review` tinyint(1) NOT NULL DEFAULT 0,
  `note` varchar(255) NOT NULL DEFAULT '',
  PRIMARY KEY (`pool_id`, `definition_id`),
  CONSTRAINT `fk_reward_pool_items_pool` FOREIGN KEY (`pool_id`) REFERENCES `cms_reward_pools` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `cms_reward_templates` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `plan` text NOT NULL,
  `updated_at` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

ALTER TABLE `rewards` ADD COLUMN IF NOT EXISTS `label` varchar(100) NULL DEFAULT NULL AFTER `badge`;

-- Pools
INSERT IGNORE INTO `cms_reward_pools` (`name`, `description`) VALUES ('Kreditmøbler', 'Møbler der kan veksles til credits');
INSERT IGNORE INTO `cms_reward_pools` (`name`, `description`) VALUES ('Daglige gaver', 'Træer, hække og planter fra de daglige gaver');
INSERT IGNORE INTO `cms_reward_pools` (`name`, `description`) VALUES ('Weekendbonus', 'Større gaver til lørdage');
INSERT IGNORE INTO `cms_reward_pools` (`name`, `description`) VALUES ('Pingviner', 'Pingvinfigurerne fra julekalenderen, i rækkefølge');
INSERT IGNORE INTO `cms_reward_pools` (`name`, `description`) VALUES ('Julebonus', 'Ekstra gaver på dag 1, 7, 14 og 21 i julekalenderen');
INSERT IGNORE INTO `cms_reward_pools` (`name`, `description`) VALUES ('Jul', 'Julemøbler (forslag fra Claude, gennemgå "Tjek"-møblerne)');
INSERT IGNORE INTO `cms_reward_pools` (`name`, `description`) VALUES ('Vinter', 'Sne, is og vinter (forslag fra Claude, gennemgå "Tjek"-møblerne)');
INSERT IGNORE INTO `cms_reward_pools` (`name`, `description`) VALUES ('Påske', 'Påske (forslag fra Claude, gennemgå "Tjek"-møblerne)');
INSERT IGNORE INTO `cms_reward_pools` (`name`, `description`) VALUES ('Valentin', 'Valentins dag og kærlighed (forslag fra Claude, gennemgå "Tjek"-møblerne)');
INSERT IGNORE INTO `cms_reward_pools` (`name`, `description`) VALUES ('Halloween', 'Halloween (forslag fra Claude, gennemgå "Tjek"-møblerne)');
INSERT IGNORE INTO `cms_reward_pools` (`name`, `description`) VALUES ('Sommer', 'Strand, sol og sommer (forslag fra Claude, gennemgå "Tjek"-møblerne)');
INSERT IGNORE INTO `cms_reward_pool_items` (`pool_id`, `definition_id`, `sort_order`, `needs_review`, `note`)
SELECT p.id, s.definition_id, s.sort_order, s.needs_review, s.note FROM `cms_reward_pools` p
JOIN (
  SELECT 209 AS definition_id, 0 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 213 AS definition_id, 1 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 211 AS definition_id, 2 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 212 AS definition_id, 3 AS sort_order, 0 AS needs_review, '' AS note
) s JOIN `items_definitions` d ON d.id = s.definition_id
WHERE p.name = 'Kreditmøbler';
INSERT IGNORE INTO `cms_reward_pool_items` (`pool_id`, `definition_id`, `sort_order`, `needs_review`, `note`)
SELECT p.id, s.definition_id, s.sort_order, s.needs_review, s.note FROM `cms_reward_pools` p
JOIN (
  SELECT 1621 AS definition_id, 0 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1622 AS definition_id, 1 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1623 AS definition_id, 2 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1620 AS definition_id, 3 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1617 AS definition_id, 4 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1619 AS definition_id, 5 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1618 AS definition_id, 6 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 73 AS definition_id, 7 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 71 AS definition_id, 8 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 74 AS definition_id, 9 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1613 AS definition_id, 10 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1614 AS definition_id, 11 AS sort_order, 0 AS needs_review, '' AS note
) s JOIN `items_definitions` d ON d.id = s.definition_id
WHERE p.name = 'Daglige gaver';
INSERT IGNORE INTO `cms_reward_pool_items` (`pool_id`, `definition_id`, `sort_order`, `needs_review`, `note`)
SELECT p.id, s.definition_id, s.sort_order, s.needs_review, s.note FROM `cms_reward_pools` p
JOIN (
  SELECT 1510 AS definition_id, 0 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1519 AS definition_id, 1 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1731 AS definition_id, 2 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 346 AS definition_id, 3 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 304 AS definition_id, 4 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 2011 AS definition_id, 5 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 340 AS definition_id, 6 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 335 AS definition_id, 7 AS sort_order, 0 AS needs_review, '' AS note
) s JOIN `items_definitions` d ON d.id = s.definition_id
WHERE p.name = 'Weekendbonus';
INSERT IGNORE INTO `cms_reward_pool_items` (`pool_id`, `definition_id`, `sort_order`, `needs_review`, `note`)
SELECT p.id, s.definition_id, s.sort_order, s.needs_review, s.note FROM `cms_reward_pools` p
JOIN (
  SELECT 1966 AS definition_id, 0 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1967 AS definition_id, 1 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1968 AS definition_id, 2 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1969 AS definition_id, 3 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1970 AS definition_id, 4 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1971 AS definition_id, 5 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1972 AS definition_id, 6 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1973 AS definition_id, 7 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1974 AS definition_id, 8 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1975 AS definition_id, 9 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1976 AS definition_id, 10 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1977 AS definition_id, 11 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1978 AS definition_id, 12 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1979 AS definition_id, 13 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1980 AS definition_id, 14 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1981 AS definition_id, 15 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1982 AS definition_id, 16 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1983 AS definition_id, 17 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1984 AS definition_id, 18 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1985 AS definition_id, 19 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1986 AS definition_id, 20 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1987 AS definition_id, 21 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1988 AS definition_id, 22 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1989 AS definition_id, 23 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1990 AS definition_id, 24 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 1965 AS definition_id, 25 AS sort_order, 0 AS needs_review, '' AS note
) s JOIN `items_definitions` d ON d.id = s.definition_id
WHERE p.name = 'Pingviner';
INSERT IGNORE INTO `cms_reward_pool_items` (`pool_id`, `definition_id`, `sort_order`, `needs_review`, `note`)
SELECT p.id, s.definition_id, s.sort_order, s.needs_review, s.note FROM `cms_reward_pools` p
JOIN (
  SELECT 1991 AS definition_id, 0 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 293 AS definition_id, 1 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 331 AS definition_id, 2 AS sort_order, 0 AS needs_review, '' AS note
  UNION ALL SELECT 171 AS definition_id, 3 AS sort_order, 0 AS needs_review, '' AS note
) s JOIN `items_definitions` d ON d.id = s.definition_id
WHERE p.name = 'Julebonus';
INSERT IGNORE INTO `cms_reward_pool_items` (`pool_id`, `definition_id`, `sort_order`, `needs_review`, `note`)
SELECT p.id, s.definition_id, s.sort_order, s.needs_review, s.note FROM `cms_reward_pools` p
JOIN (
  SELECT 103 AS definition_id, 0 AS sort_order, 1 AS needs_review, 'Candelabra released as festive holiday season furni' AS note
  UNION ALL SELECT 104 AS definition_id, 1 AS sort_order, 0 AS needs_review, 'Christmas ham for Christmas lunch' AS note
  UNION ALL SELECT 377 AS definition_id, 2 AS sort_order, 1 AS needs_review, 'Santa-themed trax sound set' AS note
  UNION ALL SELECT 395 AS definition_id, 3 AS sort_order, 1 AS needs_review, 'Santa-themed trax sound set' AS note
  UNION ALL SELECT 402 AS definition_id, 4 AS sort_order, 1 AS needs_review, 'Santa-themed trax sound set' AS note
  UNION ALL SELECT 1479 AS definition_id, 5 AS sort_order, 0 AS needs_review, 'Christmas tree' AS note
  UNION ALL SELECT 1480 AS definition_id, 6 AS sort_order, 0 AS needs_review, 'Christmas tree' AS note
  UNION ALL SELECT 1481 AS definition_id, 7 AS sort_order, 0 AS needs_review, 'Christmas tree' AS note
  UNION ALL SELECT 1482 AS definition_id, 8 AS sort_order, 0 AS needs_review, 'Holly poster, Jul page' AS note
  UNION ALL SELECT 1483 AS definition_id, 9 AS sort_order, 0 AS needs_review, 'Christmas turkey' AS note
  UNION ALL SELECT 1484 AS definition_id, 10 AS sort_order, 0 AS needs_review, 'Electric Christmas candles' AS note
  UNION ALL SELECT 1485 AS definition_id, 11 AS sort_order, 0 AS needs_review, 'Gingerbread house' AS note
  UNION ALL SELECT 1486 AS definition_id, 12 AS sort_order, 0 AS needs_review, 'Christmas pudding' AS note
  UNION ALL SELECT 1487 AS definition_id, 13 AS sort_order, 0 AS needs_review, 'Christmas rubber duck' AS note
  UNION ALL SELECT 1488 AS definition_id, 14 AS sort_order, 0 AS needs_review, 'Christmas angel poster' AS note
  UNION ALL SELECT 1489 AS definition_id, 15 AS sort_order, 0 AS needs_review, 'Silver star, Jul page' AS note
  UNION ALL SELECT 1490 AS definition_id, 16 AS sort_order, 0 AS needs_review, 'Gold star, Jul page' AS note
  UNION ALL SELECT 1491 AS definition_id, 17 AS sort_order, 0 AS needs_review, 'Silver garland, Jul page' AS note
  UNION ALL SELECT 1492 AS definition_id, 18 AS sort_order, 0 AS needs_review, 'Christmas star poster' AS note
  UNION ALL SELECT 1493 AS definition_id, 19 AS sort_order, 0 AS needs_review, 'Christmas star poster' AS note
  UNION ALL SELECT 1494 AS definition_id, 20 AS sort_order, 0 AS needs_review, 'Snowman poster on Jul page' AS note
  UNION ALL SELECT 1495 AS definition_id, 21 AS sort_order, 0 AS needs_review, 'Snow landscape poster on Jul page' AS note
  UNION ALL SELECT 1496 AS definition_id, 22 AS sort_order, 0 AS needs_review, 'Mistletoe poster' AS note
  UNION ALL SELECT 1497 AS definition_id, 23 AS sort_order, 0 AS needs_review, 'Christmas stocking' AS note
  UNION ALL SELECT 1498 AS definition_id, 24 AS sort_order, 0 AS needs_review, 'Santa Claus poster' AS note
  UNION ALL SELECT 1499 AS definition_id, 25 AS sort_order, 0 AS needs_review, 'Three Wise Men poster' AS note
  UNION ALL SELECT 1500 AS definition_id, 26 AS sort_order, 0 AS needs_review, 'Reindeer Rudolph poster' AS note
  UNION ALL SELECT 1501 AS definition_id, 27 AS sort_order, 0 AS needs_review, 'Christmas hyacinth, Jul page' AS note
  UNION ALL SELECT 1502 AS definition_id, 28 AS sort_order, 0 AS needs_review, 'Christmas hyacinth, Jul page' AS note
  UNION ALL SELECT 1503 AS definition_id, 29 AS sort_order, 0 AS needs_review, 'Poinsettia Christmas plant' AS note
  UNION ALL SELECT 1504 AS definition_id, 30 AS sort_order, 0 AS needs_review, 'Red candle on Jul page' AS note
  UNION ALL SELECT 1505 AS definition_id, 31 AS sort_order, 0 AS needs_review, 'White candles on Jul page' AS note
  UNION ALL SELECT 1506 AS definition_id, 32 AS sort_order, 0 AS needs_review, 'Gold garland, Jul page' AS note
  UNION ALL SELECT 1507 AS definition_id, 33 AS sort_order, 0 AS needs_review, 'Christmas screen, Ho Ho Ho' AS note
  UNION ALL SELECT 1518 AS definition_id, 34 AS sort_order, 1 AS needs_review, 'Snow globe with falling snow' AS note
  UNION ALL SELECT 1525 AS definition_id, 35 AS sort_order, 1 AS needs_review, 'Xmas fireplace test item' AS note
  UNION ALL SELECT 1970 AS definition_id, 36 AS sort_order, 1 AS needs_review, 'Elf penguin, Ho Ho' AS note
  UNION ALL SELECT 1991 AS definition_id, 37 AS sort_order, 0 AS needs_review, 'Reindeer, rare Christmas decoration' AS note
) s JOIN `items_definitions` d ON d.id = s.definition_id
WHERE p.name = 'Jul';
INSERT IGNORE INTO `cms_reward_pool_items` (`pool_id`, `definition_id`, `sort_order`, `needs_review`, `note`)
SELECT p.id, s.definition_id, s.sort_order, s.needs_review, s.note FROM `cms_reward_pools` p
JOIN (
  SELECT 726 AS definition_id, 0 AS sort_order, 0 AS needs_review, 'Snowdrift rug, creaking snow underfoot' AS note
  UNION ALL SELECT 1416 AS definition_id, 1 AS sort_order, 1 AS needs_review, 'Polar bear sofa evokes ice and cold' AS note
  UNION ALL SELECT 1494 AS definition_id, 2 AS sort_order, 0 AS needs_review, 'Snowman poster on Jul page' AS note
  UNION ALL SELECT 1495 AS definition_id, 3 AS sort_order, 0 AS needs_review, 'Snow landscape poster on Jul page' AS note
  UNION ALL SELECT 1518 AS definition_id, 4 AS sort_order, 1 AS needs_review, 'Snow globe with falling snow' AS note
  UNION ALL SELECT 1973 AS definition_id, 5 AS sort_order, 1 AS needs_review, 'Ice hockey penguin' AS note
  UNION ALL SELECT 1984 AS definition_id, 6 AS sort_order, 0 AS needs_review, 'Skiing penguin' AS note
  UNION ALL SELECT 1991 AS definition_id, 7 AS sort_order, 0 AS needs_review, 'Reindeer, rare Christmas decoration' AS note
) s JOIN `items_definitions` d ON d.id = s.definition_id
WHERE p.name = 'Vinter';
INSERT IGNORE INTO `cms_reward_pool_items` (`pool_id`, `definition_id`, `sort_order`, `needs_review`, `note`)
SELECT p.id, s.definition_id, s.sort_order, s.needs_review, s.note FROM `cms_reward_pools` p
JOIN (
  SELECT 717 AS definition_id, 0 AS sort_order, 1 AS needs_review, 'Daffodil meadow rug; daffodils are Danish Easter flowers' AS note
  UNION ALL SELECT 1428 AS definition_id, 1 AS sort_order, 0 AS needs_review, 'Bunny on Love and Easter pages' AS note
  UNION ALL SELECT 1432 AS definition_id, 2 AS sort_order, 0 AS needs_review, 'Basket with Easter eggs' AS note
  UNION ALL SELECT 1433 AS definition_id, 3 AS sort_order, 0 AS needs_review, 'Easter egg' AS note
  UNION ALL SELECT 1435 AS definition_id, 4 AS sort_order, 0 AS needs_review, 'Duck dressed as Easter bunny' AS note
  UNION ALL SELECT 1968 AS definition_id, 5 AS sort_order, 1 AS needs_review, 'Penguin in bunny costume' AS note
) s JOIN `items_definitions` d ON d.id = s.definition_id
WHERE p.name = 'Påske';
INSERT IGNORE INTO `cms_reward_pool_items` (`pool_id`, `definition_id`, `sort_order`, `needs_review`, `note`)
SELECT p.id, s.definition_id, s.sort_order, s.needs_review, s.note FROM `cms_reward_pools` p
JOIN (
  SELECT 13 AS definition_id, 0 AS sort_order, 0 AS needs_review, 'Heart doormat, clearly love-themed' AS note
  UNION ALL SELECT 47 AS definition_id, 1 AS sort_order, 1 AS needs_review, 'Spin-the-bottle item on Love catalogue page' AS note
  UNION ALL SELECT 62 AS definition_id, 2 AS sort_order, 1 AS needs_review, 'Pink rug listed on Love catalogue page' AS note
  UNION ALL SELECT 106 AS definition_id, 3 AS sort_order, 1 AS needs_review, 'Red candles on Love page, romantic' AS note
  UNION ALL SELECT 108 AS definition_id, 4 AS sort_order, 1 AS needs_review, 'Flower bouquet gift on Love page' AS note
  UNION ALL SELECT 346 AS definition_id, 5 AS sort_order, 1 AS needs_review, 'SUPERLOVE fan blowing loving breezes' AS note
  UNION ALL SELECT 1424 AS definition_id, 6 AS sort_order, 0 AS needs_review, 'Heart sticky note from Love page' AS note
  UNION ALL SELECT 1425 AS definition_id, 7 AS sort_order, 0 AS needs_review, 'Duck in love, Love page' AS note
  UNION ALL SELECT 1426 AS definition_id, 8 AS sort_order, 0 AS needs_review, 'Red Valentine rose' AS note
  UNION ALL SELECT 1427 AS definition_id, 9 AS sort_order, 1 AS needs_review, 'Gift bouquet of flowers, romantic gift' AS note
  UNION ALL SELECT 1428 AS definition_id, 10 AS sort_order, 0 AS needs_review, 'Bunny on Love and Easter pages' AS note
  UNION ALL SELECT 1429 AS definition_id, 11 AS sort_order, 0 AS needs_review, 'Heart-shaped sofa' AS note
  UNION ALL SELECT 1430 AS definition_id, 12 AS sort_order, 0 AS needs_review, 'Cupid statue with love arrows' AS note
  UNION ALL SELECT 1431 AS definition_id, 13 AS sort_order, 0 AS needs_review, 'Giant heart full of love' AS note
  UNION ALL SELECT 1510 AS definition_id, 14 AS sort_order, 1 AS needs_review, 'Valentine screen sprite, flirting theme' AS note
  UNION ALL SELECT 1519 AS definition_id, 15 AS sort_order, 0 AS needs_review, 'Valentine heart randomizer' AS note
  UNION ALL SELECT 1542 AS definition_id, 16 AS sort_order, 1 AS needs_review, 'Neon heart, romance on wall' AS note
  UNION ALL SELECT 1731 AS definition_id, 17 AS sort_order, 1 AS needs_review, 'Witch cauldron love potion on Love page' AS note
) s JOIN `items_definitions` d ON d.id = s.definition_id
WHERE p.name = 'Valentin';
INSERT IGNORE INTO `cms_reward_pool_items` (`pool_id`, `definition_id`, `sort_order`, `needs_review`, `note`)
SELECT p.id, s.definition_id, s.sort_order, s.needs_review, s.note FROM `cms_reward_pools` p
JOIN (
  SELECT 68 AS definition_id, 0 AS sort_order, 0 AS needs_review, 'Coffin from Halloween 2014 set' AS note
  UNION ALL SELECT 1439 AS definition_id, 1 AS sort_order, 0 AS needs_review, 'Pumpkin lamp' AS note
  UNION ALL SELECT 1440 AS definition_id, 2 AS sort_order, 0 AS needs_review, 'Spiderweb poster, Halloween page' AS note
  UNION ALL SELECT 1441 AS definition_id, 3 AS sort_order, 0 AS needs_review, 'Creaking chains, Halloween page' AS note
  UNION ALL SELECT 1442 AS definition_id, 4 AS sort_order, 0 AS needs_review, 'Mummy poster' AS note
  UNION ALL SELECT 1443 AS definition_id, 5 AS sort_order, 0 AS needs_review, 'Skeleton poster' AS note
  UNION ALL SELECT 1444 AS definition_id, 6 AS sort_order, 0 AS needs_review, 'Skull candlestick' AS note
  UNION ALL SELECT 1445 AS definition_id, 7 AS sort_order, 0 AS needs_review, 'Bat poster' AS note
  UNION ALL SELECT 1446 AS definition_id, 8 AS sort_order, 0 AS needs_review, 'Bloody ghost duck' AS note
  UNION ALL SELECT 1447 AS definition_id, 9 AS sort_order, 0 AS needs_review, 'Dead ghost duck' AS note
  UNION ALL SELECT 1448 AS definition_id, 10 AS sort_order, 0 AS needs_review, 'Ectoplasm ghost duck' AS note
  UNION ALL SELECT 1449 AS definition_id, 11 AS sort_order, 0 AS needs_review, 'Habboween full moon grass' AS note
  UNION ALL SELECT 1450 AS definition_id, 12 AS sort_order, 0 AS needs_review, 'Habboween grave' AS note
  UNION ALL SELECT 1461 AS definition_id, 13 AS sort_order, 0 AS needs_review, 'Habboween window' AS note
  UNION ALL SELECT 1462 AS definition_id, 14 AS sort_order, 0 AS needs_review, 'Habboween window' AS note
  UNION ALL SELECT 1477 AS definition_id, 15 AS sort_order, 0 AS needs_review, 'Habboween crypt' AS note
  UNION ALL SELECT 1478 AS definition_id, 16 AS sort_order, 0 AS needs_review, 'Witch cauldron' AS note
  UNION ALL SELECT 1731 AS definition_id, 17 AS sort_order, 1 AS needs_review, 'Witch cauldron love potion on Love page' AS note
  UNION ALL SELECT 1929 AS definition_id, 18 AS sort_order, 0 AS needs_review, 'Habboween 2008 window' AS note
  UNION ALL SELECT 1930 AS definition_id, 19 AS sort_order, 0 AS needs_review, 'Habboween 2008 window' AS note
  UNION ALL SELECT 1931 AS definition_id, 20 AS sort_order, 1 AS needs_review, 'Part of Habboween 2008 virus set' AS note
  UNION ALL SELECT 1932 AS definition_id, 21 AS sort_order, 1 AS needs_review, 'Part of Habboween 2008 virus set' AS note
  UNION ALL SELECT 1933 AS definition_id, 22 AS sort_order, 0 AS needs_review, 'Blood bath, Habboween 2008' AS note
  UNION ALL SELECT 1934 AS definition_id, 23 AS sort_order, 0 AS needs_review, 'Slime bath, Habboween 2008' AS note
  UNION ALL SELECT 1935 AS definition_id, 24 AS sort_order, 0 AS needs_review, 'Bloody bed, Habboween 2008' AS note
  UNION ALL SELECT 1936 AS definition_id, 25 AS sort_order, 0 AS needs_review, 'Slimy bed, Habboween 2008' AS note
  UNION ALL SELECT 1937 AS definition_id, 26 AS sort_order, 0 AS needs_review, 'Body bag, spooky Habboween item' AS note
  UNION ALL SELECT 1938 AS definition_id, 27 AS sort_order, 0 AS needs_review, 'Bloody curtain, Habboween 2008' AS note
  UNION ALL SELECT 1939 AS definition_id, 28 AS sort_order, 0 AS needs_review, 'Slimy curtain, Habboween 2008' AS note
  UNION ALL SELECT 1940 AS definition_id, 29 AS sort_order, 0 AS needs_review, 'Bloody defibrillator, Habboween 2008' AS note
  UNION ALL SELECT 1941 AS definition_id, 30 AS sort_order, 0 AS needs_review, 'Sticky defibrillator, Habboween 2008' AS note
  UNION ALL SELECT 1942 AS definition_id, 31 AS sort_order, 1 AS needs_review, 'Manhole from Habboween 2008 set' AS note
  UNION ALL SELECT 1943 AS definition_id, 32 AS sort_order, 0 AS needs_review, 'Blood sink, Habboween 2008' AS note
  UNION ALL SELECT 1944 AS definition_id, 33 AS sort_order, 0 AS needs_review, 'Slime sink, Habboween 2008' AS note
  UNION ALL SELECT 1945 AS definition_id, 34 AS sort_order, 1 AS needs_review, 'Surgical tray from Habboween 2008 set' AS note
  UNION ALL SELECT 1946 AS definition_id, 35 AS sort_order, 1 AS needs_review, 'X-ray from Habboween 2008 set' AS note
  UNION ALL SELECT 1947 AS definition_id, 36 AS sort_order, 0 AS needs_review, 'Haunted organ, Habboween 2009' AS note
  UNION ALL SELECT 1948 AS definition_id, 37 AS sort_order, 0 AS needs_review, 'Haunted wall, Habboween 2009' AS note
  UNION ALL SELECT 1949 AS definition_id, 38 AS sort_order, 0 AS needs_review, 'Haunted window, Habboween 2009' AS note
  UNION ALL SELECT 1950 AS definition_id, 39 AS sort_order, 0 AS needs_review, 'Haunted chair, Habboween 2009' AS note
  UNION ALL SELECT 1951 AS definition_id, 40 AS sort_order, 0 AS needs_review, 'Haunted chandelier, Habboween 2009' AS note
  UNION ALL SELECT 1952 AS definition_id, 41 AS sort_order, 0 AS needs_review, 'Haunted corner wall, Habboween 2009' AS note
  UNION ALL SELECT 1953 AS definition_id, 42 AS sort_order, 0 AS needs_review, 'Haunted curtain, Habboween 2009' AS note
  UNION ALL SELECT 1954 AS definition_id, 43 AS sort_order, 0 AS needs_review, 'Haunted floor, Habboween 2009' AS note
  UNION ALL SELECT 1955 AS definition_id, 44 AS sort_order, 0 AS needs_review, 'Ghost' AS note
  UNION ALL SELECT 1956 AS definition_id, 45 AS sort_order, 0 AS needs_review, 'Haunted cellar hatch' AS note
  UNION ALL SELECT 1957 AS definition_id, 46 AS sort_order, 0 AS needs_review, 'Haunted jar, Habboween 2009' AS note
  UNION ALL SELECT 1958 AS definition_id, 47 AS sort_order, 0 AS needs_review, 'Haunted mirror' AS note
  UNION ALL SELECT 1959 AS definition_id, 48 AS sort_order, 0 AS needs_review, 'Haunted portrait' AS note
  UNION ALL SELECT 1960 AS definition_id, 49 AS sort_order, 0 AS needs_review, 'Haunted stone wall' AS note
  UNION ALL SELECT 1961 AS definition_id, 50 AS sort_order, 0 AS needs_review, 'Haunted dining table' AS note
  UNION ALL SELECT 1962 AS definition_id, 51 AS sort_order, 0 AS needs_review, 'Haunted forest wall' AS note
  UNION ALL SELECT 1963 AS definition_id, 52 AS sort_order, 0 AS needs_review, 'Haunted TV' AS note
  UNION ALL SELECT 1974 AS definition_id, 53 AS sort_order, 0 AS needs_review, 'Pingoween infected penguin' AS note
  UNION ALL SELECT 1993 AS definition_id, 54 AS sort_order, 1 AS needs_review, 'Voodoo doll, spooky' AS note
  UNION ALL SELECT 2131 AS definition_id, 55 AS sort_order, 1 AS needs_review, 'Scarecrow, autumn/spooky association' AS note
  UNION ALL SELECT 2159 AS definition_id, 56 AS sort_order, 1 AS needs_review, 'Skull, spooky though jungle line' AS note
) s JOIN `items_definitions` d ON d.id = s.definition_id
WHERE p.name = 'Halloween';
INSERT IGNORE INTO `cms_reward_pool_items` (`pool_id`, `definition_id`, `sort_order`, `needs_review`, `note`)
SELECT p.id, s.definition_id, s.sort_order, s.needs_review, s.note FROM `cms_reward_pools` p
JOIN (
  SELECT 257 AS definition_id, 0 AS sort_order, 0 AS needs_review, 'Parasol, description celebrates sunshine' AS note
  UNION ALL SELECT 258 AS definition_id, 1 AS sort_order, 0 AS needs_review, 'Parasol, description celebrates sunshine' AS note
  UNION ALL SELECT 259 AS definition_id, 2 AS sort_order, 0 AS needs_review, 'Parasol, description celebrates sunshine' AS note
  UNION ALL SELECT 260 AS definition_id, 3 AS sort_order, 0 AS needs_review, 'Parasol, description celebrates sunshine' AS note
  UNION ALL SELECT 304 AS definition_id, 4 AS sort_order, 1 AS needs_review, 'Awning giving shade from burning sun' AS note
  UNION ALL SELECT 305 AS definition_id, 5 AS sort_order, 1 AS needs_review, 'Awning giving shade from burning sun' AS note
  UNION ALL SELECT 306 AS definition_id, 6 AS sort_order, 1 AS needs_review, 'Awning giving shade from burning sun' AS note
  UNION ALL SELECT 308 AS definition_id, 7 AS sort_order, 1 AS needs_review, 'Awning giving shade from burning sun' AS note
  UNION ALL SELECT 309 AS definition_id, 8 AS sort_order, 1 AS needs_review, 'Awning giving shade from burning sun' AS note
  UNION ALL SELECT 310 AS definition_id, 9 AS sort_order, 1 AS needs_review, 'Awning giving shade from burning sun' AS note
  UNION ALL SELECT 311 AS definition_id, 10 AS sort_order, 1 AS needs_review, 'Awning giving shade from burning sun' AS note
  UNION ALL SELECT 312 AS definition_id, 11 AS sort_order, 1 AS needs_review, 'Awning giving shade from burning sun' AS note
  UNION ALL SELECT 313 AS definition_id, 12 AS sort_order, 1 AS needs_review, 'Awning giving shade from burning sun' AS note
  UNION ALL SELECT 324 AS definition_id, 13 AS sort_order, 1 AS needs_review, 'Ice cream machine, summer treat' AS note
  UNION ALL SELECT 325 AS definition_id, 14 AS sort_order, 1 AS needs_review, 'Ice cream machine, summer treat' AS note
  UNION ALL SELECT 326 AS definition_id, 15 AS sort_order, 1 AS needs_review, 'Ice cream machine, summer treat' AS note
  UNION ALL SELECT 327 AS definition_id, 16 AS sort_order, 1 AS needs_review, 'Ice cream machine, summer treat' AS note
  UNION ALL SELECT 328 AS definition_id, 17 AS sort_order, 1 AS needs_review, 'Ice cream machine, summer treat' AS note
  UNION ALL SELECT 329 AS definition_id, 18 AS sort_order, 1 AS needs_review, 'Ice cream machine, summer treat' AS note
  UNION ALL SELECT 330 AS definition_id, 19 AS sort_order, 1 AS needs_review, 'Ice cream machine, summer treat' AS note
  UNION ALL SELECT 331 AS definition_id, 20 AS sort_order, 1 AS needs_review, 'Ice cream machine, summer treat' AS note
  UNION ALL SELECT 332 AS definition_id, 21 AS sort_order, 1 AS needs_review, 'Ice cream machine, summer treat' AS note
  UNION ALL SELECT 333 AS definition_id, 22 AS sort_order, 1 AS needs_review, 'Ice cream machine, summer treat' AS note
  UNION ALL SELECT 341 AS definition_id, 23 AS sort_order, 1 AS needs_review, 'Fan for cool breeze in heat' AS note
  UNION ALL SELECT 342 AS definition_id, 24 AS sort_order, 1 AS needs_review, 'Fan for cool breeze in heat' AS note
  UNION ALL SELECT 343 AS definition_id, 25 AS sort_order, 1 AS needs_review, 'Fan for cool breeze in heat' AS note
  UNION ALL SELECT 344 AS definition_id, 26 AS sort_order, 1 AS needs_review, 'Fan for cool breeze in heat' AS note
  UNION ALL SELECT 345 AS definition_id, 27 AS sort_order, 1 AS needs_review, 'Fan for cool breeze in heat' AS note
  UNION ALL SELECT 347 AS definition_id, 28 AS sort_order, 1 AS needs_review, 'Fan for cool breeze in heat' AS note
  UNION ALL SELECT 348 AS definition_id, 29 AS sort_order, 1 AS needs_review, 'Fan, fresh breeze on a hot day' AS note
  UNION ALL SELECT 349 AS definition_id, 30 AS sort_order, 1 AS needs_review, 'Fan for cool breeze in heat' AS note
  UNION ALL SELECT 350 AS definition_id, 31 AS sort_order, 1 AS needs_review, 'Fan for cool breeze in heat' AS note
  UNION ALL SELECT 368 AS definition_id, 32 AS sort_order, 1 AS needs_review, 'Beach-themed trax sound set' AS note
  UNION ALL SELECT 1414 AS definition_id, 33 AS sort_order, 1 AS needs_review, 'Palm chair with coconuts, tropical beach feel' AS note
  UNION ALL SELECT 1420 AS definition_id, 34 AS sort_order, 0 AS needs_review, 'Jamaica sand beach rug' AS note
  UNION ALL SELECT 1437 AS definition_id, 35 AS sort_order, 1 AS needs_review, 'Large palm tree, desert shade' AS note
  UNION ALL SELECT 1438 AS definition_id, 36 AS sort_order, 1 AS needs_review, 'Sand tile, beach feel' AS note
  UNION ALL SELECT 1555 AS definition_id, 37 AS sort_order, 1 AS needs_review, 'Tiki tropical beach luau food' AS note
  UNION ALL SELECT 1556 AS definition_id, 38 AS sort_order, 1 AS needs_review, 'Tiki tropical beach luau food' AS note
  UNION ALL SELECT 1557 AS definition_id, 39 AS sort_order, 1 AS needs_review, 'Tiki pineapple bowl, tropical beach' AS note
  UNION ALL SELECT 1558 AS definition_id, 40 AS sort_order, 1 AS needs_review, 'Tiki sun-ripened fruit bowl' AS note
  UNION ALL SELECT 1559 AS definition_id, 41 AS sort_order, 1 AS needs_review, 'Tiki bowl for seafood, beach line' AS note
  UNION ALL SELECT 1560 AS definition_id, 42 AS sort_order, 1 AS needs_review, 'Tropical toucan from Tiki beach line' AS note
  UNION ALL SELECT 1561 AS definition_id, 43 AS sort_order, 0 AS needs_review, 'Tiki torch lighting the beach' AS note
  UNION ALL SELECT 1562 AS definition_id, 44 AS sort_order, 0 AS needs_review, 'Tiki beach bar with umbrella drinks' AS note
  UNION ALL SELECT 1563 AS definition_id, 45 AS sort_order, 0 AS needs_review, 'Surfboard' AS note
  UNION ALL SELECT 1564 AS definition_id, 46 AS sort_order, 1 AS needs_review, 'Tiki statue, tropical beach line' AS note
  UNION ALL SELECT 1565 AS definition_id, 47 AS sort_order, 0 AS needs_review, 'Warm beach sand' AS note
  UNION ALL SELECT 1566 AS definition_id, 48 AS sort_order, 0 AS needs_review, 'Parasol giving shade from sun' AS note
  UNION ALL SELECT 1567 AS definition_id, 49 AS sort_order, 1 AS needs_review, 'Tropical jungle grass, Tiki line' AS note
  UNION ALL SELECT 1568 AS definition_id, 50 AS sort_order, 0 AS needs_review, 'Tiki beach bar corner' AS note
  UNION ALL SELECT 1569 AS definition_id, 51 AS sort_order, 1 AS needs_review, 'Butterflies in tropical Tiki line' AS note
  UNION ALL SELECT 1570 AS definition_id, 52 AS sort_order, 0 AS needs_review, 'Beach bar stool, brush off sand' AS note
  UNION ALL SELECT 1752 AS definition_id, 53 AS sort_order, 0 AS needs_review, 'Barbecue grill from Sommer page' AS note
  UNION ALL SELECT 1753 AS definition_id, 54 AS sort_order, 0 AS needs_review, 'Barbecue grill from Sommer page' AS note
  UNION ALL SELECT 1754 AS definition_id, 55 AS sort_order, 0 AS needs_review, 'Barbecue grill from Sommer page' AS note
  UNION ALL SELECT 1755 AS definition_id, 56 AS sort_order, 0 AS needs_review, 'Barbecue grill from Sommer page' AS note
  UNION ALL SELECT 1756 AS definition_id, 57 AS sort_order, 0 AS needs_review, 'Beach deck chair from Sommer page' AS note
  UNION ALL SELECT 1757 AS definition_id, 58 AS sort_order, 0 AS needs_review, 'Beach deck chair from Sommer page' AS note
  UNION ALL SELECT 1758 AS definition_id, 59 AS sort_order, 0 AS needs_review, 'Beach deck chair from Sommer page' AS note
  UNION ALL SELECT 1759 AS definition_id, 60 AS sort_order, 0 AS needs_review, 'Beach deck chair from Sommer page' AS note
  UNION ALL SELECT 1760 AS definition_id, 61 AS sort_order, 0 AS needs_review, 'Beach deck chair from Sommer page' AS note
  UNION ALL SELECT 1761 AS definition_id, 62 AS sort_order, 0 AS needs_review, 'Beach deck chair from Sommer page' AS note
  UNION ALL SELECT 1762 AS definition_id, 63 AS sort_order, 0 AS needs_review, 'Beach deck chair from Sommer page' AS note
  UNION ALL SELECT 1763 AS definition_id, 64 AS sort_order, 0 AS needs_review, 'Beach deck chair from Sommer page' AS note
  UNION ALL SELECT 1764 AS definition_id, 65 AS sort_order, 0 AS needs_review, 'Beach deck chair from Sommer page' AS note
  UNION ALL SELECT 1765 AS definition_id, 66 AS sort_order, 0 AS needs_review, 'Paddling pool from Sommer page' AS note
  UNION ALL SELECT 1766 AS definition_id, 67 AS sort_order, 0 AS needs_review, 'Paddling pool from Sommer page' AS note
  UNION ALL SELECT 1767 AS definition_id, 68 AS sort_order, 0 AS needs_review, 'Paddling pool from Sommer page' AS note
  UNION ALL SELECT 1768 AS definition_id, 69 AS sort_order, 0 AS needs_review, 'Paddling pool from Sommer page' AS note
  UNION ALL SELECT 1769 AS definition_id, 70 AS sort_order, 0 AS needs_review, 'Ice cream freezer for hot days' AS note
  UNION ALL SELECT 1770 AS definition_id, 71 AS sort_order, 0 AS needs_review, 'Swim ring float from Sommer page' AS note
  UNION ALL SELECT 1771 AS definition_id, 72 AS sort_order, 0 AS needs_review, 'Swim ring float from Sommer page' AS note
  UNION ALL SELECT 1988 AS definition_id, 73 AS sort_order, 1 AS needs_review, 'Swimming penguin' AS note
  UNION ALL SELECT 1995 AS definition_id, 74 AS sort_order, 1 AS needs_review, 'Hammock nap in the shade' AS note
  UNION ALL SELECT 2079 AS definition_id, 75 AS sort_order, 1 AS needs_review, 'Calippo summer campaign chair' AS note
  UNION ALL SELECT 2081 AS definition_id, 76 AS sort_order, 1 AS needs_review, 'Calippo campaign fan, cooling summer item' AS note
  UNION ALL SELECT 2084 AS definition_id, 77 AS sort_order, 0 AS needs_review, 'Calippo campaign parasol, beach item' AS note
  UNION ALL SELECT 2085 AS definition_id, 78 AS sort_order, 1 AS needs_review, 'Calippo campaign parasol variant' AS note
  UNION ALL SELECT 2086 AS definition_id, 79 AS sort_order, 0 AS needs_review, 'Calippo campaign pool' AS note
  UNION ALL SELECT 2087 AS definition_id, 80 AS sort_order, 1 AS needs_review, 'Calippo campaign pool variant' AS note
  UNION ALL SELECT 2088 AS definition_id, 81 AS sort_order, 0 AS needs_review, 'Ice cream machine' AS note
  UNION ALL SELECT 2089 AS definition_id, 82 AS sort_order, 1 AS needs_review, 'Calippo ice cream machine variant' AS note
  UNION ALL SELECT 2101 AS definition_id, 83 AS sort_order, 1 AS needs_review, 'Mall window likely beach shop' AS note
  UNION ALL SELECT 2177 AS definition_id, 84 AS sort_order, 0 AS needs_review, 'Sandcastle gate on Sommer page' AS note
  UNION ALL SELECT 2178 AS definition_id, 85 AS sort_order, 0 AS needs_review, 'Sandcastle tower on Sommer page' AS note
  UNION ALL SELECT 2179 AS definition_id, 86 AS sort_order, 0 AS needs_review, 'Sandcastle wall on Sommer page' AS note
  UNION ALL SELECT 2180 AS definition_id, 87 AS sort_order, 0 AS needs_review, 'Inflatable bath crocodile, Sommer page' AS note
  UNION ALL SELECT 2181 AS definition_id, 88 AS sort_order, 0 AS needs_review, 'Shallow paddling water, Sommer page' AS note
  UNION ALL SELECT 2182 AS definition_id, 89 AS sort_order, 0 AS needs_review, 'Deep swimming water, Sommer page' AS note
) s JOIN `items_definitions` d ON d.id = s.definition_id
WHERE p.name = 'Sommer';

-- Templates. `plan` is the JSON the calendar builder saves (Models/RewardPlan.cs); pools by id.
INSERT IGNORE INTO `cms_reward_templates` (`name`, `plan`) VALUES ('Julekalender (pingviner)', CONCAT('{"days":25,"note":"Gave med Reggie vibes\\n\\nFra Rasta.Santa","label":"Julekalender","slots":[{"poolId":', (SELECT id FROM `cms_reward_pools` WHERE name = 'Pingviner'), ',"pick":"in_order","when":"every_day"},{"items":[211,212],"pick":"cycle","when":"every_day"},{"poolId":', (SELECT id FROM `cms_reward_pools` WHERE name = 'Julebonus'), ',"pick":"in_order","when":"days","days":[1,7,14,21]}]}'));
INSERT IGNORE INTO `cms_reward_templates` (`name`, `plan`) VALUES ('Daglige gaver (måned)', CONCAT('{"days":30,"note":"Daglig gave for at besøge Habbo Hotel.","label":"Daglige gaver","slots":[{"items":[212,209],"pick":"random","when":"weekdays","weekdays":[1,2,3,4,5,7]},{"poolId":', (SELECT id FROM `cms_reward_pools` WHERE name = 'Daglige gaver'), ',"pick":"random","when":"weekdays","weekdays":[1,2,3,4,5,7]},{"poolId":', (SELECT id FROM `cms_reward_pools` WHERE name = 'Weekendbonus'), ',"pick":"random","when":"weekdays","weekdays":[6]}]}'));
