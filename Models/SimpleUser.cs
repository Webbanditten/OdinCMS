namespace KeplerCMS.Models
{
    public class SimpleUser
    {
        public int Id { get; set; }
        public string Username { get; set; }
        /// <summary>
        /// Indicates how this account was matched: "IP", "MachineID", or "Both"
        /// </summary>
        public string MatchType { get; set; }
    }
}
