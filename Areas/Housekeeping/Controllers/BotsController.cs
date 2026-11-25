using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;

namespace KeplerCMS.Areas.Housekeeping
{
    /// <summary>
    /// Public room bots (rooms_bots, Odin's BotManager): the list with each bot's picture, and an edit page with a live
    /// preview drawn as the client draws the bot in its room (the room's own casts, rooms.ccts, and the room's zoom:
    /// most public rooms are 32 px). The pictures come from the avatar renderer (keplercms:avatarRenderer:url,
    /// shockwave-imager: /avatar with the bot's figure and casts=, /parts for what the room can dress a bot in).
    /// Saving sends reset_bots, so Odin reloads the room's bots.
    /// </summary>
    [Area("Housekeeping")]
    public class BotsController : Controller
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
        private readonly ICommandQueueService _commandQueueService;
        private readonly IBotService _botService;
        private readonly IRoomService _roomService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BotsController> _logger;
        private readonly KeplerCMS.Data.DataContext _context;
        private readonly IUserService _userService;
        private readonly IGameDataService _gameData;

        public BotsController(ICommandQueueService commandQueueService, IBotService botService, IRoomService roomService, IConfiguration configuration, ILogger<BotsController> logger, KeplerCMS.Data.DataContext context, IUserService userService, IGameDataService gameData)
        {
            _gameData = gameData;
            _commandQueueService = commandQueueService;
            _botService = botService;
            _roomService = roomService;
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _userService = userService;
        }

        [HousekeepingFilter(Fuse.fuse_bots)]
        public async Task<IActionResult> Index(string Message = null)
        {
            ViewBag.Message = Message;
            var bots = await _botService.GetAllBots();
            return View(new BotsListView { Bots = bots.OrderBy(b => b.Room?.Name).ThenBy(b => b.Name) });
        }

        [HousekeepingFilter(Fuse.fuse_bots)]
        public async Task<IActionResult> Edit(int id, string message = null)
        {
            var bot = await _botService.Get(id);
            if (bot == null) return RedirectToAction("Index", new { Message = "That bot doesn't exist" });
            return View(new BotEditView { Bot = bot, Rooms = await PublicRooms(), Message = message, HandItemNames = await HandItemNames(), Others = await OtherBots(id) });
        }

        [HousekeepingFilter(Fuse.fuse_bots)]
        public async Task<IActionResult> New(int roomId = 0)
        {
            var rooms = await PublicRooms();
            var room = rooms.FirstOrDefault(r => r.Id == roomId) ?? rooms.FirstOrDefault();
            var bot = new Bots
            {
                Name = "", Mission = "", X = 0, Y = 0, StartLook = "2,2", RoomId = room?.Id ?? 0, Walkspace = "",
                // a plain Habbo to start from (the Lobby's Albert's base)
                figure = "sd=001/0&hr=010/224,186,120&hd=001/255,204,153&ey=001/0&fc=001/255,204,153&bd=001/255,204,153&lh=001/255,204,153&rh=001/255,204,153&ch=005/204,204,204&ls=002/204,204,204&rs=002/204,204,204&lg=006/51,51,51&sh=003/41,41,41",
                Speech = "", Response = "", UnrecognisedResponse = "", HandItems = "",
            };
            return View("Edit", new BotEditView { Bot = bot, Rooms = rooms, HandItemNames = await HandItemNames(), Others = await OtherBots(0) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HousekeepingFilter(Fuse.fuse_bots)]
        public async Task<IActionResult> Save(Bots bot)
        {
            bot.Name = (bot.Name ?? "").Trim();
            bot.Mission = (bot.Mission ?? "").Trim();
            bot.figure = (bot.figure ?? "").Trim();
            bot.Walkspace = Regex.Replace((bot.Walkspace ?? "").Trim(), @"\s+", " ");
            bot.Speech ??= ""; bot.Response ??= ""; bot.UnrecognisedResponse ??= "";
            // drinks are names: Odin looks for them in what Habbos say and hands over the hand item whose text
            // (handitemN) has that name, else a drink by that name
            bot.HandItems = string.Join(",", (bot.HandItems ?? "").Split(',').Select(d => Regex.Replace(d, @"\s+", " ").Trim()).Where(d => d.Length > 0));
            var error =
                bot.Name.Length == 0 || bot.Name.Length > 25 ? "The name needs 1 to 25 characters" :
                bot.Mission.Length > 255 ? "The mission is at most 255 characters" :
                bot.figure.Length == 0 || bot.figure.Length > 255 || !bot.figure.Contains('=') ? "The look (figure) is missing or longer than 255 characters" :
                !Regex.IsMatch(bot.StartLook ?? "", @"^[0-7],[0-7]$") ? "The start look is \"head,body\" directions, 0 to 7" :
                bot.Walkspace.Length > 0 && !Regex.IsMatch(bot.Walkspace, @"^\d+,\d+( \d+,\d+)*$") ? "The walk area is tiles as \"x,y x,y ...\"" :
                bot.HandItems.Length > 50 ? "The drinks are at most 50 characters together" :
                bot.HandItems.IndexOfAny(new[] { '|', '#' }) >= 0 ? "Drink names can't contain | or #" :
                LinesError("What it says now and then", bot.Speech) ?? LinesError("What it says when it serves a drink", bot.Response) ??
                LinesError("What it says when someone says its name", bot.UnrecognisedResponse) ??
                ((await PublicRooms()).All(r => r.Id != bot.RoomId) ? "Pick a public room" : null);
            if (error != null)
                return View("Edit", new BotEditView { Bot = bot, Rooms = await PublicRooms(), Error = error, HandItemNames = await HandItemNames(), Others = await OtherBots(bot.Id) });
            // Odin reads the walk area tile by tile; an empty one would stop it loading the bot: the start tile at least.
            if (bot.Walkspace.Length == 0) bot.Walkspace = $"{bot.X},{bot.Y}";

            if (bot.Id == 0)
            {
                await _botService.AddBot(bot);
                _commandQueueService.QueueCommand(CommandQueueType.reset_bots, new CommandTemplate { RoomId = bot.RoomId });
                return RedirectToAction("Edit", new { id = bot.Id, message = $"{bot.Name} is in the room now" });
            }
            if (!await _botService.UpdateBot(bot))
                return View("Edit", new BotEditView { Bot = bot, Rooms = await PublicRooms(), Error = "Couldn't save the bot", HandItemNames = await HandItemNames(), Others = await OtherBots(bot.Id) });
            return RedirectToAction("Edit", new { id = bot.Id, message = "Saved: the room's bots were reloaded" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HousekeepingFilter(Fuse.fuse_bots)]
        public async Task<IActionResult> Delete(int id)
        {
            var bot = await _botService.Get(id);
            if (bot == null) return RedirectToAction("Index");
            await _botService.DeleteBot(id);
            _commandQueueService.QueueCommand(CommandQueueType.reset_bots, new CommandTemplate { RoomId = bot.RoomId });
            return RedirectToAction("Index", new { Message = $"Deleted {bot.Name}" });
        }

        /// <summary>A bot's picture as the client draws it in its room: the saved bot (id), or a look being edited
        /// (figure + roomId), turned and posed as asked.</summary>
        [HousekeepingFilter(Fuse.fuse_bots)]
        public async Task<IActionResult> Picture(int id = 0, string figure = null, int roomId = 0, int? direction = null, int? headDirection = null, string action = null, int drink = 0, string size = null, bool head = false)
        {
            var renderer = _configuration["keplercms:avatarRenderer:url"];
            if (string.IsNullOrEmpty(renderer)) return StatusCode(503, "No avatar renderer (keplercms:avatarRenderer:url)");
            if (id > 0)
            {
                var bot = await _botService.Get(id);
                if (bot == null) return NotFound();
                figure ??= bot.figure;
                if (roomId == 0) roomId = bot.RoomId;
                var look = (bot.StartLook ?? "2,2").Split(',');
                headDirection ??= int.TryParse(look[0], out var h) ? h : 2;
                direction ??= look.Length > 1 && int.TryParse(look[1], out var b) ? b : headDirection;
            }
            if (string.IsNullOrEmpty(figure) || figure.Length > 500) return BadRequest();
            var room = roomId > 0 ? await _roomService.GetRoom(roomId) : null;
            // head=1: the head alone, cropped (the look editor's hair and face pictures); else the client's whole canvas
            var query = $"figure={Uri.EscapeDataString(figure)}&casts={Uri.EscapeDataString(room?.Casts ?? "")}" + (head ? "&head=1" : "&raw=1") +
                        $"&direction={Math.Clamp(direction ?? 2, 0, 7)}&head_direction={Math.Clamp(headDirection ?? direction ?? 2, 0, 7)}";
            if (action is "wav" or "spk" or "wlk" or "sit" or "lay") query += "&action=" + action;
            if (drink > 0) query += $"&action=crr&crr={drink}";
            if (size is "s" or "b") query += "&size=" + size;
            try
            {
                using var response = await Http.GetAsync(renderer.TrimEnd('/') + "/avatar?" + query);
                if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
                Response.Headers["Cache-Control"] = "private, max-age=60";
                return File(await response.Content.ReadAsByteArrayAsync(), response.Content.Headers.ContentType?.MediaType ?? "image/png");
            }
            catch (Exception e)
            {
                _logger.LogWarning("Avatar renderer unreachable for a bot picture: {Message}", e.Message);
                return StatusCode(502, "The avatar renderer can't be reached");
            }
        }

        /// <summary>What the room can dress a bot in (the renderer's /parts for the room's casts).</summary>
        [HousekeepingFilter(Fuse.fuse_bots)]
        public async Task<IActionResult> Parts(int roomId)
        {
            var renderer = _configuration["keplercms:avatarRenderer:url"];
            if (string.IsNullOrEmpty(renderer)) return StatusCode(503, "No avatar renderer (keplercms:avatarRenderer:url)");
            var room = await _roomService.GetRoom(roomId);
            try
            {
                using var response = await Http.GetAsync(renderer.TrimEnd('/') + "/parts?casts=" + Uri.EscapeDataString(room?.Casts ?? ""));
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode, body);
                return Content(body, "application/json");
            }
            catch (Exception e)
            {
                _logger.LogWarning("Avatar renderer unreachable for bot parts: {Message}", e.Message);
                return StatusCode(502, "The avatar renderer can't be reached");
            }
        }

        /// <summary>Odin reads a line's chat type from what follows its first '#' (Enum.valueOf): anything but one '#' and
        /// SHOUT / WHISPER / CHAT would stop the room loading its bots.</summary>
        private static string LinesError(string what, string lines)
        {
            if (lines.Contains("_AI_")) return null; // Odin uses none of them
            foreach (var line in lines.Split('|'))
            {
                var parts = line.Split('#');
                if (parts.Length == 1) continue;
                if (parts.Length > 2 || !new[] { "SHOUT", "WHISPER", "CHAT" }.Contains(parts[1]))
                    return $"{what}: \"{line}\" has a # the hotel can't read (only #SHOUT, #WHISPER or #CHAT at the end)";
            }
            return null;
        }

        /// <summary>The room for the Place tab: its floor plan (rooms_models), its own furniture (public_items) and the
        /// other bots standing and walking in it.</summary>
        [HousekeepingFilter(Fuse.fuse_bots)]
        public async Task<IActionResult> Map(int roomId, int botId = 0)
        {
            var room = await _roomService.GetRoom(roomId);
            if (room == null) return NotFound();
            var model = _context.RoomModels.FirstOrDefault(m => m.ModelId == room.Model);
            if (model == null) return NotFound("No floor plan for " + room.Model);
            var furni = _context.PublicItems.Where(p => p.RoomModel == room.Model)
                .Select(p => new { p.X, p.Y, p.Sprite, p.Behaviour, p.Length, p.Width, p.Rotation }).ToList();
            var bots = (await _botService.GetForRoom(roomId)).Where(b => b.Id != botId)
                .Select(b => new { b.Id, b.Name, b.X, b.Y, walk = b.Walkspace }).ToList();
            var rows = (model.Heightmap ?? "").Replace("\r", "").Replace("\n", "|").Split('|', StringSplitOptions.RemoveEmptyEntries);
            return Json(new { model = room.Model, rows, door = new { x = model.DoorX, y = model.DoorY, z = model.DoorZ }, furni, bots });
        }

        /// <summary>A Habbo's look in the bot format (the renderer's /convert), to start a bot from it.</summary>
        [HousekeepingFilter(Fuse.fuse_bots)]
        public async Task<IActionResult> LookOf(string username)
        {
            var renderer = _configuration["keplercms:avatarRenderer:url"];
            if (string.IsNullOrEmpty(renderer)) return StatusCode(503, "No avatar renderer (keplercms:avatarRenderer:url)");
            var user = string.IsNullOrWhiteSpace(username) ? null : await _userService.GetUserByUsername(username.Trim());
            if (user == null) return NotFound("No Habbo called " + username);
            try
            {
                using var response = await Http.GetAsync(renderer.TrimEnd('/') + "/convert?figure=" + Uri.EscapeDataString(user.Figure ?? "") + "&sex=" + Uri.EscapeDataString(user.Gender ?? ""));
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode, user.Username + "'s look can't be read (" + body + ")");
                return Content(body, "application/json");
            }
            catch (Exception e)
            {
                _logger.LogWarning("Avatar renderer unreachable for a look: {Message}", e.Message);
                return StatusCode(502, "The avatar renderer can't be reached");
            }
        }

        /// <summary>The hotel's hand items by number (handitem1..25 in the external texts): a drink with one of these names
        /// is that hand item.</summary>
        private async Task<System.Collections.Generic.Dictionary<int, string>> HandItemNames()
        {
            try
            {
                var texts = await _gameData.GetTexts(Enumerable.Range(1, 25).Select(i => "handitem" + i));
                return Enumerable.Range(1, 25).Where(i => texts.TryGetValue("handitem" + i, out var t) && !string.IsNullOrWhiteSpace(t))
                    .ToDictionary(i => i, i => texts["handitem" + i].Trim());
            }
            catch (Exception e)
            {
                _logger.LogWarning("No hand item names for the bot editor: {Message}", e.Message);
                return new();
            }
        }

        private async Task<System.Collections.Generic.List<BotSummary>> OtherBots(int exceptId) =>
            (await _botService.GetAllBots()).Where(b => b.Id != exceptId)
                .Select(b => new BotSummary { Id = b.Id, Name = b.Name, Room = b.Room?.Name, Figure = b.figure }).OrderBy(b => b.Name).ToList();

        private async Task<System.Collections.Generic.List<Rooms>> PublicRooms() =>
            (await _roomService.GetPublicRooms()).OrderBy(r => r.Name).ToList();
    }
}
