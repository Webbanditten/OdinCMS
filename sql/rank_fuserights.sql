/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

CREATE TABLE `rank_fuserights` (
  `min_rank` int(11) NOT NULL,
  `fuseright` varchar(255) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `rank_fuserights` (`min_rank`, `fuseright`) VALUES
(1, 'default');
INSERT INTO `rank_fuserights` (`min_rank`, `fuseright`) VALUES
(1, 'fuse_login');
INSERT INTO `rank_fuserights` (`min_rank`, `fuseright`) VALUES
(1, 'fuse_buy_credits');
INSERT INTO `rank_fuserights` (`min_rank`, `fuseright`) VALUES
(1, 'fuse_trade'),
(1, 'fuse_room_queue_default'),
(2, 'fuse_enter_full_rooms'),
(3, 'fuse_enter_locked_rooms'),
(3, 'fuse_kick'),
(3, 'fuse_mute'),
(4, 'fuse_ban'),
(4, 'fuse_room_mute'),
(4, 'fuse_room_kick'),
(4, 'fuse_receive_calls_for_help'),
(4, 'fuse_remove_stickies'),
(5, 'fuse_mod'),
(5, 'fuse_superban'),
(5, 'fuse_pick_up_any_furni'),
(5, 'fuse_ignore_room_owner'),
(5, 'fuse_any_room_controller'),
(2, 'fuse_room_alert'),
(5, 'fuse_moderator_access'),
(6, 'fuse_administrator_access'),
(6, 'fuse_see_flat_ids'),
(5, 'fuse_credits'),
(6, 'fuse_debug_window'),
(6, 'fuse_performance_panel'),
(6, 'fuse_catalog_editor'),
(6, 'fuse_use_special_room_layouts'),
(6, 'housekeeping_localization'),
(6, 'housekeeping_campaign'),
(6, 'housekeeping_menu'),
(3, 'housekeeping'),
(6, 'housekeeping_website'),
(6, 'housekeeping_pages'),
(6, 'housekeeping_upload'),
(6, 'housekeeping_news');

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;