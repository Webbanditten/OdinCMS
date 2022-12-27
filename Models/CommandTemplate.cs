using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Models
{
    public class CommandTemplate
    {
        public int UserId { get; set; }
        public int Credits { get; set; }
        public int DefinitionId { get; set; }
        public int RoomId { get; set; }
        public string RoomType { get; set; }
        public string Message { get; set; }
        public string MessageUrl { get; set; }
        public string MessageLink { get; set; }
        public int FriendRequestTo { get; set; }
        public bool OnlineOnly { get; set; }
        public string[] Users { get; set; }
    }
}
