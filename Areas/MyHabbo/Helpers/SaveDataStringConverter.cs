using KeplerCMS.Data.Models;
using System.Collections.Generic;

namespace KeplerCMS.Areas.MyHabbo.Helpers
{
    /// <summary>
    /// Parses the positions the home editor saves: "id:x,y,z/id:x,y,z/", e.g. "31276:217,60,237/31277:247,74,235/".
    /// Malformed entries are skipped.
    /// </summary>
    public static class SaveDataStringConverter
    {
        public static List<HomesItems> GetItemsFromString(string data)
        {
            var items = new List<HomesItems>();
            if (string.IsNullOrEmpty(data)) return items;

            foreach (var entry in data.Split('/'))
            {
                var idAndPos = entry.Split(':');
                if (idAndPos.Length != 2) continue;
                var pos = idAndPos[1].Split(',');
                if (pos.Length != 3) continue;

                if (int.TryParse(idAndPos[0], out var id)
                    && int.TryParse(pos[0], out var x)
                    && int.TryParse(pos[1], out var y)
                    && int.TryParse(pos[2], out var z))
                {
                    items.Add(new HomesItems { Id = id, X = x, Y = y, Z = z });
                }
            }
            return items;
        }
    }
}
