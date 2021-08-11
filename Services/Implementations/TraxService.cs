using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Helpers;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Implementations
{
    public class TraxService : ITraxService
    {
        private readonly DataContext _context;

        public TraxService(DataContext context)
        {
            _context = context;
        }

        public async Task<SoundMachineSongs> GetSingleSongById(int id)
        {
            return await _context.SoundMachineSongs.Where(s => s.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<SoundMachineSongs>> GetSongsByOwner(int ownerId)
        {
            return await _context.SoundMachineSongs.Where(s => s.UserId == ownerId).ToListAsync();
        }

        public string GetTrack(string data, int track)
        {
            var trackNum = ":" + track + ":";
            if (track == 1)
            {
                trackNum = track + ":";
            } else if(track == 4) {
                try
                {
                    return data.Split(new string[] { trackNum }, StringSplitOptions.None)[1].Trim(':');
                }
                catch (Exception)
                {
                    return "";
                }
            }
            try
            {
                return data.Split(new string[] { trackNum }, StringSplitOptions.None)[1].Split(":")[0].Trim(':');
            }
            catch (Exception)
            {
                return "";
            }
            
        }
    }

}
