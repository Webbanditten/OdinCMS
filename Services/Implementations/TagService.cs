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
        private readonly IUserService _userService;
        public TagService(DataContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
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
        public async Task<List<TagCloud>> TagCloud() {
            return await (
            from tag in _context.Tags.Take(10)
            group tag by tag.Tag into tagGroup
            select new TagCloud
            {
                Tag = tagGroup.Key,
                Amount = tagGroup.Count(),
            }).ToListAsync();
        }
        public async Task<List<TagSearchResult>> Search(string tag)
        {
            if(tag == null) return null;
            var filteredTags = await _context.Tags.Where(s=>s.Tag.Contains(tag.ToLower())).ToListAsync();

            var result = new List<TagSearchResult>();

            foreach(var dbTag in filteredTags) {
                var tagData = new TagSearchResult { Tag = dbTag.Tag};
                if(dbTag.GroupId != 0) {
                    tagData.Group = await _context.Homes.Where(s=>s.Id == dbTag.GroupId).FirstOrDefaultAsync();
                    tagData.Tags = await this.TagsForGroup(dbTag.GroupId);
                }

                if(dbTag.UserId != 0) {
                    tagData.User = await _userService.GetUserById(dbTag.UserId);
                    tagData.Tags = await this.TagsForUser(dbTag.UserId);
                }

                result.Add(tagData);
            }

            return result;
        }

        public async Task<TagFightViewModel> Battle(string tag1, string tag2)
        {
            var result = new TagFightViewModel { 
                Winner = 0, 
                Tag1 = tag1,
                Tag2 = tag2,
                Tag1Count = (await _context.Tags.Where(s=>s.Tag == tag1.ToLower()).ToListAsync()).Count(), 
                Tag2Count = (await _context.Tags.Where(s=>s.Tag == tag2.ToLower()).ToListAsync()).Count()  
            }; 
            if(result.Tag1Count > result.Tag2Count) {
                result.Winner = 2;
            } else if(result.Tag2Count > result.Tag1Count) {
                result.Winner = 1;
            }
            return result;
        }
    }

}
