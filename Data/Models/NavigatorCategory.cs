using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    /// <summary>
    /// A navigator category (Odin's NavigatorCategory). Public room categories have public_spaces = 1; "Fællesrum" (3) is the
    /// root the client opens (navigator.public.default). Housekeeping > Public rooms edits the ones under it, Housekeeping >
    /// Room categories the guest room categories under "Gæsterum" (4, navigator.private.default).
    /// </summary>
    [Table("rooms_categories")]
    public class NavigatorCategory
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("order_id")]
        public int OrderId { get; set; }
        [Column("parent_id")]
        public int ParentId { get; set; }
        [Column("isnode")]
        public int IsNode { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("public_spaces")]
        public int PublicSpaces { get; set; }
        [Column("allow_trading")]
        public int AllowTrading { get; set; }
        [Column("minrole_access")]
        public int MinroleAccess { get; set; }
        [Column("minrole_setflatcat")]
        public int MinroleSetFlatCat { get; set; }
        [Column("fuse_access")]
        public string FuseAccess { get; set; }
        [Column("fuse_setflatcat")]
        public string FuseSetFlatCat { get; set; }
        /// <summary>daily, weekly or alltime: lists the best rated guest rooms instead of its own (Odin's RoomRatings, v1.32.0).</summary>
        [Column("ranking")]
        public string Ranking { get; set; } = "";
        /// <summary>The navigator doesn't list or open it and players can't put rooms in it (Odin v1.33.0).</summary>
        [Column("is_hidden")]
        public bool IsHidden { get; set; }
    }

    /// <summary>
    /// A walkway between two public rooms (Odin's WalkwaysManager). A hidden room joined to a shown one this way is part
    /// of it: the navigator adds its visitors to that room's, and following someone there forwards to it.
    /// </summary>
    [Table("public_roomwalkways")]
    public class PublicRoomWalkway
    {
        [Column("room_id")]
        public int? RoomId { get; set; }
        [Column("to_id")]
        public int? ToId { get; set; }
    }
}
