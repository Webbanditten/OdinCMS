-- Ads in the hotel and on the website (Housekeeping > Ads). One row per ad; it rotates with the other active ads of its
-- placement (weighted). Images come from the media library (an upload, /images/ads/..., or a site file /c_images/...).
--   room_billboard   the frame on the wall of 46 public rooms (160x140 or 200x180, slanted; fit 'auto' slants a flat picture)
--   room_loading     the room loading window (max 620x360; at most 5 per session, every ads.loading.every-th room entry)
--   purse            the purse window (200x60)
--   pool_screen      the big screen in Habbo Poolen and on Tagterassen (108x102)
--   website_top      the website's top banner (728x90)
--   website_side     the website's right column (160x600)
--   website_partner  the small box above it (168x120)
-- Views are counted when the image is fetched (/ads/img/<id>), clicks on /ads/click/<id> (which redirects).
CREATE TABLE IF NOT EXISTS `cms_ads` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `placement` varchar(32) NOT NULL,
  `image` varchar(255) NOT NULL,
  `fit` varchar(16) NOT NULL DEFAULT 'auto',
  `click_url` varchar(500) DEFAULT NULL,
  `starts_at` datetime DEFAULT NULL,
  `ends_at` datetime DEFAULT NULL,
  `enabled` tinyint(1) NOT NULL DEFAULT 1,
  `weight` int(11) NOT NULL DEFAULT 1,
  `views` bigint(20) NOT NULL DEFAULT 0,
  `clicks` bigint(20) NOT NULL DEFAULT 0,
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  `updated_at` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  KEY `placement` (`placement`, `enabled`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Billboards only: the rooms an ad is shown in (none = every room with a billboard).
CREATE TABLE IF NOT EXISTS `cms_ad_rooms` (
  `ad_id` int(11) NOT NULL,
  `room_id` int(11) NOT NULL,
  PRIMARY KEY (`ad_id`, `room_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- The billboard frame of each public room that has one, measured from the room casts: the screen is always 160x140 or
-- 200x180, facing left or right. The client centres the picture on a point; in a few rooms that point is 1 px off the
-- screen, and dx/dy is how far the picture's content must move (right/down) to sit on it. Odin only sends a ready-made
-- billboard (fit = its format) to rooms with that format; fit 'auto' is slanted to each room's format by the CMS.
CREATE TABLE IF NOT EXISTS `cms_billboard_frames` (
  `room_id` int(11) NOT NULL,
  `format` varchar(16) NOT NULL,
  `frame` varchar(64) NOT NULL,
  `dx` tinyint(4) NOT NULL DEFAULT 0,
  `dy` tinyint(4) NOT NULL DEFAULT 0,
  PRIMARY KEY (`room_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT IGNORE INTO `cms_billboard_frames` (`room_id`, `format`, `frame`, `dx`, `dy`) VALUES
(1, 'small_left', 'adframe_newbie_lobby_left', 0, 0),
(2, 'small_right', 'adframe_theatre', 1, 1),
(3, 'small_left', 'adframe_library_left', 0, 0),
(4, 'small_left', 'adframe_tv_studio_left', 0, 0),
(5, 'small_left', 'adframe_cinema_left', 0, 0),
(6, 'small_right', 'ad_frame', 1, 1),
(7, 'small_left', 'adframe_ballroom_left', 0, 0),
(8, 'small_left', 'adframe_cr_kitchen_left', 0, 0),
(9, 'small_left', 'adframe_tv_pub_left', 0, 0),
(11, 'small_right', 'adframe_cr_cafe_right', 0, 0),
(12, 'small_right', 'adframe_space_cafe_right', 0, 0),
(13, 'small_left', 'adframe_rooftop_left', 1, 0),
(14, 'small_right', 'adframe_rooftop_right', 0, 0),
(15, 'small_left', 'adframe_pizza_left', 0, 0),
(16, 'large_right', 'adframe_habburgers_right', 0, 0),
(17, 'small_left', 'ad_frame_left', 1, 1),
(18, 'small_left', 'adframe_tearoom_left', 0, 0),
(19, 'large_left', 'adframe_chromide1_left', 0, 0),
(21, 'large_left', 'adframe_chromide1_left', 0, 0),
(22, 'small_right', 'adframe_chromide2_right', 0, 0),
(23, 'large_left', 'adframe_massiva_left', 0, 0),
(24, 'large_left', 'adframe_massiva_left', 0, 0),
(25, 'large_left', 'adframe_sunset_left', 0, 0),
(26, 'small_left', 'adframe_cafe_gold0_left', 0, 0),
(27, 'small_left', 'adframe_chill_left', 0, 0),
(29, 'small_left', 'ad_frame_left', 0, 0),
(30, 'small_left', 'adframe_theatre_left', 0, 0),
(31, 'small_right', 'adframe_sun_terrace_right', 0, -1),
(32, 'small_right', 'adframe_gate_park_right', 0, 0),
(33, 'small_right', 'adframe_gate_park_right', 0, 0),
(36, 'small_left', 'adframe_lido_left', 0, 0),
(38, 'small_right', 'adframe_terrace_right', 0, 0),
(39, 'small_left', 'adframe_lobby_a_left', 0, 0),
(40, 'large_right', 'adframe_floorlobby_right', 0, 0),
(41, 'large_right', 'adframe_floorlobby_right', 0, 0),
(42, 'large_right', 'adframe_floorlobby_right', 0, 0),
(44, 'large_right', 'adframe_net_right', 0, 0),
(45, 'large_left', 'adframe_beauty_salon1_left', 0, 1),
(46, 'small_left', 'adframe_theden_left', 0, 0),
(59, 'large_right', 'adframe_net_right', 0, 0),
(60, 'small_right', 'adframe_library_right', 0, 0),
(66, 'small_right', 'adframe_bb_lobby_right', 0, 0),
(67, 'large_left', 'adframe_chromide1_left', 0, 0),
(68, 'large_right', 'adframe_habburgers_right', 0, 0),
(69, 'large_left', 'adframe_beauty_salon2_left', 0, 1),
(70, 'small_left', 'adframe_ballroom_left', 0, 0);

-- Starting set (only into an empty table): the website's two hard-coded ads (on, as today), and the old hotel ads found
-- on the asset host (off until someone switches them on).
INSERT INTO `cms_ads` (`name`, `placement`, `image`, `fit`, `click_url`, `enabled`, `weight`)
SELECT * FROM (
  SELECT 'Habbowood' AS n, 'website_top' AS p, '/c_images/album728/ads_habbowood.png' AS i, 'as_is' AS f, '/habbowood' AS u, 1 AS e, 1 AS w UNION ALL
  SELECT 'Habbo Club', 'website_top', '/c_images/album728/ads_habbogroup_clubjoin.png', 'as_is', '/credits/habboclub', 0, 1 UNION ALL
  SELECT 'Habbo Home', 'website_side', '/images/ads-scale/Habbohome_phold_160x600.gif', 'as_is', '/hotel/homes', 1, 1 UNION ALL
  SELECT 'BattleBall', 'website_side', '/images/ads-scale/battleball_reddevil_br.gif', 'as_is', '/games', 0, 1 UNION ALL
  SELECT 'Habbo Club (room loading)', 'room_loading', '/c_images/ads/interstitial_hc.gif', 'as_is', '/credits/habboclub', 0, 1 UNION ALL
  SELECT 'Builders (billboard)', 'room_billboard', '/c_images/ads/billboard_builders_small_left.gif', 'small_left', NULL, 0, 1
) seed
WHERE NOT EXISTS (SELECT 1 FROM `cms_ads`);
