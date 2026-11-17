using System.Text.RegularExpressions;

namespace KeplerCMS.Models
{
    /// <summary>
    /// A room placed in page or news text: {room_123} (123 = rooms.id). It shows the room photo (housekeeping → room →
    /// Take photo, /room-photo/ID.png), name, owner, description, who is in it and a "go to room" link.
    /// </summary>
    public static class RoomWidget
    {
        public static readonly Regex Code = new Regex(@"\{room_(\d{1,9})\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }
}
