-- Room photos (housekeeping → room → Take photo; served at /room-photo/<roomId>.png). One row per room: the latest
-- photo, a PNG with a transparent background taken by the room-photographer service.
CREATE TABLE IF NOT EXISTS `cms_room_photos` (
  `room_id` int(11) NOT NULL,
  `photo` mediumblob NOT NULL,
  `width` int(11) NOT NULL,
  `height` int(11) NOT NULL,
  `taken_at` datetime NOT NULL,
  `taken_by` int(11) DEFAULT NULL,
  PRIMARY KEY (`room_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
