using KeplerCMS.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.MyHabbo.Helpers
{
    public class SaveDataStringConverter
    {

        public static List<HomesItems> GetItemsFromString(string data)
        {
            var changedItems = new List<HomesItems>();
            if (data != null)
            {
                var items = data.Split("/");
                foreach (var item in items)
                {
                    if(item != "")
                    {
                        var idAndPos = item.Split(":");
                        var id = int.Parse(idAndPos[0]);
                        var pos = idAndPos[1].Split(",");
                        var x = int.Parse(pos[0]);
                        var y = int.Parse(pos[1]);
                        var z = int.Parse(pos[2]);
                        changedItems.Add(new HomesItems { Id = id, X = x, Y = y, Z = z });
                    }
                    
                }
            }
            return changedItems;
        }
    }
}
