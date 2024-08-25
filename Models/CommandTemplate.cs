using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Models
{
    public class CommandTemplate
    {
        public string Type { get; set; }
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
        
        public int BanLength { get; set; }
        public string ExtraInfo { get; set; }
        public bool BanIp { get; set; }
        public bool BanMachine { get; set; }
        public string RoomDescription { get; set; }
        public string RoomName { get; set; }
        public int RoomAccessType { get; set; }
        public bool RoomShowOwnerName { get; set; }
    }
}
