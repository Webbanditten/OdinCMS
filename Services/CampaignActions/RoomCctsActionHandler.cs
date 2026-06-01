using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KeplerCMS.Data;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Services.CampaignActions
{
    public class RoomCctsActionHandler : ICampaignActionHandler
    {
        private readonly DataContext _context;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public RoomCctsActionHandler(DataContext context)
        {
            _context = context;
        }

        public async Task<string> TakeSnapshot(string actionData)
        {
            var data = JsonSerializer.Deserialize<RoomCctsActionData>(actionData, _jsonOptions);
            var roomIds = data.Rooms.Select(r => r.RoomId).ToList();

            var currentStates = await _context.Rooms
                .Where(r => roomIds.Contains(r.Id))
                .Select(r => new RoomCctState
                {
                    RoomId = r.Id,
                    Ccts = r.Casts,
                    Description = r.Description,
                    Name = r.Name
                })
                .ToListAsync();

            return JsonSerializer.Serialize(new RoomCctsSnapshotData { Rooms = currentStates });
        }

        public async Task Apply(string actionData)
        {
            var data = JsonSerializer.Deserialize<RoomCctsActionData>(actionData, _jsonOptions);

            foreach (var roomChange in data.Rooms)
            {
                var room = await _context.Rooms.FindAsync(roomChange.RoomId);
                if (room != null)
                {
                    // Null/empty means "leave this field unchanged"
                    if (!string.IsNullOrEmpty(roomChange.NewCcts))
                        room.Casts = roomChange.NewCcts;
                    if (!string.IsNullOrEmpty(roomChange.NewDescription))
                        room.Description = roomChange.NewDescription;
                    if (!string.IsNullOrEmpty(roomChange.NewName))
                        room.Name = roomChange.NewName;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task Revert(string snapshotData)
        {
            var data = JsonSerializer.Deserialize<RoomCctsSnapshotData>(snapshotData, _jsonOptions);

            foreach (var roomState in data.Rooms)
            {
                var room = await _context.Rooms.FindAsync(roomState.RoomId);
                if (room != null)
                {
                    room.Casts = roomState.Ccts;
                    room.Description = roomState.Description;
                    room.Name = roomState.Name;
                }
            }

            await _context.SaveChangesAsync();
        }

        public string GetConflictKey(string actionData)
        {
            var data = JsonSerializer.Deserialize<RoomCctsActionData>(actionData, _jsonOptions);
            var roomIds = string.Join(",", data.Rooms.Select(r => r.RoomId).OrderBy(id => id));
            return $"room_ccts:{roomIds}";
        }
    }

    public class RoomCctsActionData
    {
        public List<RoomCctMapping> Rooms { get; set; } = new List<RoomCctMapping>();
    }

    public class RoomCctMapping
    {
        public int RoomId { get; set; }
        public string NewCcts { get; set; }
        public string NewDescription { get; set; }
        public string NewName { get; set; }
    }

    public class RoomCctsSnapshotData
    {
        public List<RoomCctState> Rooms { get; set; } = new List<RoomCctState>();
    }

    public class RoomCctState
    {
        public int RoomId { get; set; }
        public string Ccts { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
    }
}
