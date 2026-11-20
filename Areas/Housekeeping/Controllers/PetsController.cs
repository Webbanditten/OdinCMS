using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace KeplerCMS.Areas.Housekeeping
{
    /// <summary>
    /// Hobba tools > Scam detection > Find pets by user (Sulake's "Pets"): a Habbo's pets, or pets by name, with where they
    /// are (items_pets joined with the pet's item). A Habbo's own page lists their pets too.
    /// </summary>
    [Area("Housekeeping")]
    [HousekeepingFilter(Fuse.fuse_kick)]
    public class PetsController : Controller
    {
        private readonly DataContext _context;

        public PetsController(DataContext context)
        {
            _context = context;
        }

        public record PetRow(int ItemId, string Name, string Type, int Race, string Colour, DateTime Born, int OwnerId, string Owner, int RoomId, string RoomName);

        public class PetsPage
        {
            public string Criteria { get; set; }
            public string Query { get; set; }
            public List<PetRow> Pets { get; set; } = new();
        }

        public async Task<IActionResult> Index(string criteria = "owner", string query = null)
        {
            var page = new PetsPage { Criteria = criteria == "name" ? "name" : "owner", Query = string.IsNullOrWhiteSpace(query) ? null : query.Trim() };
            if (page.Query == null) return View(page);

            var sql = new ContextSql(_context);
            var where = page.Criteria == "name" ? "p.name LIKE @q" : "u.username = @q";
            var value = page.Criteria == "name" ? "%" + page.Query.Replace("%", "").Replace("_", "") + "%" : page.Query;
            page.Pets = await sql.QueryOpen(
                "SELECT p.item_id, p.name, p.type, p.race, p.colour, p.born, i.user_id, u.username, i.room_id, r.name FROM items_pets p " +
                "JOIN items i ON i.id = p.item_id LEFT JOIN users u ON u.id = i.user_id LEFT JOIN rooms r ON r.id = i.room_id " +
                "WHERE " + where + " ORDER BY p.born DESC LIMIT 200",
                r => new PetRow((int)r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetInt32(3), r.IsDBNull(4) ? "" : r.GetString(4),
                    DateTimeOffset.FromUnixTimeSeconds(r.GetInt64(5)).LocalDateTime, r.GetInt32(6), r.IsDBNull(7) ? null : r.GetString(7),
                    r.GetInt32(8), r.IsDBNull(9) ? null : r.GetString(9)), ("@q", value));
            return View(page);
        }

        public static string TypeName(string type) => type switch { "0" => "Dog", "1" => "Cat", "2" => "Crocodile", _ => "Type " + type };
    }
}
