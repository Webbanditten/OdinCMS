using Isopoh.Cryptography.Argon2;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Helpers;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Implementations
{
    public class TagService : ITagService
    {
        private readonly DataContext _context;

        public TagService(DataContext context)
        {
            _context = context;
        }
        public async Task<List<Tags>> TagsForUser(int userId, bool editMode = false)
        {
            var tags = await _context.Tags.Where(s => s.UserId == userId).ToListAsync();
            var canEdit = editMode;

            tags.ForEach(t=>t.CanEdit = canEdit);
            
            return tags;
        }

        public async Task<List<Tags>> TagsForGroup(int groupId, bool editMode = false)
        {
            var tags = await _context.Tags.Where(s => s.GroupId == groupId).ToListAsync();
            tags.ForEach(t=>t.CanEdit = editMode);

            return tags;
        }

        public async Task<Tags> AddTag(Tags tag)
        {
            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();
            return tag;
        }


        public void RemoveTag(Tags tag)
        {
            var dbTag = _context.Tags.Where(s=>s.Tag.ToLower() == tag.Tag.ToLower() && s.UserId == tag.UserId).FirstOrDefault();
            if (dbTag != null)
                _context.Remove(dbTag);

            _context.SaveChanges();
        }

    }

}
