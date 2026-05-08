using System;

namespace KeplerCMS.Models
{
    public class SimpleUser
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string LastMachineId { get; set; }
        public string LastIp { get; set; }
        public DateTime? LastAccess { get; set; }
        public int AccessCount { get; set; }
        /// <summary>
        /// Indicates how this account was matched: "IP", "MachineID", or "Both"
        /// </summary>
        public string MatchType { get; set; }
    }
}
