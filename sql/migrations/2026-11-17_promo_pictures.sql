-- Promo pictures built from room photos (housekeeping → Promo picture; served at /promo-picture/<id>.png). The crop
-- settings are kept so a picture can be edited again.
CREATE TABLE IF NOT EXISTS `cms_promo_pictures` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `room_id` int(11) NOT NULL,
  `zoom` int(11) NOT NULL DEFAULT 1,
  `offset_x` int(11) NOT NULL DEFAULT 0,
  `offset_y` int(11) NOT NULL DEFAULT 0,
  `background` varchar(7) NOT NULL DEFAULT '#000000',
  `banner` varchar(32) NOT NULL DEFAULT '',
  `photo` mediumblob NOT NULL,
  `updated_at` datetime NOT NULL,
  `updated_by` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `room_id` (`room_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
