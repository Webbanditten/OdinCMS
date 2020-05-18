using KeplerCMS.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.MyHabbo.Helpers
{
    public class SkinIdToString
    {

        public static string Convert(int skinId)
        {
            string skin = skinId switch
            {
                1 => "w_skin_defaultskin",
                2 => "w_skin_speechbubbleskin",
                3 => "w_skin_metalskin",
                4 => "w_skin_noteitskin",
                5 => "w_skin_notepadskin",
                6 => "w_skin_goldenskin",
                7 => "w_skin_hc_machineskin",
                8 => "w_skin_hc_pillowskin",
                _ => "w_skin_defaultskin",
            };
            return skin;
        }

        public static string ConvertNote(int skinId)
        {
            string skin = skinId switch
            {
                1 => "n_skin_defaultskin",
                2 => "n_skin_speechbubbleskin",
                3 => "n_skin_metalskin",
                4 => "n_skin_noteitskin",
                5 => "n_skin_notepadskin",
                6 => "n_skin_goldenskin",
                7 => "n_skin_hc_machineskin",
                8 => "n_skin_hc_pillowskin",
                _ => "n_skin_defaultskin",
            };
            return skin;
        }
    }
}
