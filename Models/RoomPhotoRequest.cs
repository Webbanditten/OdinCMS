using System;

namespace KeplerCMS.Models
{
    public class RoomPhotoRequest
    {
        public Guid Id { get; set; }
        public int RoomId { get; set; }
        public bool IsPublicRoom { get; set; }
        public int RequestedBy { get; set; }
        /// <summary>queued, running, done or failed.</summary>
        public string Status { get; set; }
        public string Error { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
    }
}
