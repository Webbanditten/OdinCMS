using System;
using KeplerCMS.Models;

namespace KeplerCMS.Services.Interfaces
{
    /// <summary>
    /// Room photos through the room-photographer service (settings keplercms:photographer:url, token, username). Requests
    /// are worked one at a time in the background; a finished photo is stored in cms_room_photos.
    /// </summary>
    public interface IRoomPhotographer
    {
        /// <summary>Whether the service and the photographer account are configured.</summary>
        bool Enabled { get; }

        /// <summary>Queues a photo of the room, or returns the request already queued or running for it.</summary>
        RoomPhotoRequest Request(int roomId, bool isPublicRoom, int requestedBy);

        RoomPhotoRequest Get(Guid id);
    }
}
