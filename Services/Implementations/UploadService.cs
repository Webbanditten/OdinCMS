using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Implementations
{
    public class UploadService : IUploadService
    {
        private readonly DataContext _context;

        public UploadService(DataContext context)
        {
            _context = context;
        }

        public async Task<bool> Add(UploadAddViewModel model)
        {
            foreach(var file in model.Files)
            {
                if (file.Length > 0)
                {
                    using var ms = new MemoryStream();
                    file.CopyTo(ms);
                    var fileBytes = ms.ToArray();
                    var upload = new Upload
                    {
                        Category = model.Category,
                        ContentType = file.ContentType,
                        Name = file.FileName,
                        Blob = fileBytes
                    };
                    await _context.Uploads.AddAsync(upload);
                }

            }
            await _context.SaveChangesAsync();
            return true;
        }

        public List<UploadViewModel> GetAll()
        {
            var uploads = _context.Uploads.Select(x => new UploadViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Category = x.Category,
                ContentType = x.ContentType
            }).ToList();
            return uploads;
        }

        public async Task<Upload> GetByCategoryAndName(string category, string name)
        {
            return await _context.Uploads.Where(s => s.Category.ToLower() == category && s.Name.ToLower() == name.ToLower()).FirstOrDefaultAsync();
        }

        public async Task<bool> Remove(int id)
        {
            _context.Uploads.Remove(new Upload { Id = id });
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Upload> Update(UploadUpdateViewModel model)
        {
            var file = await _context.Uploads.Where(x => x.Id == model.Id).FirstOrDefaultAsync();
            file.Category = model.Category;
            _context.Update(file);
            await _context.SaveChangesAsync();
            return file;
        }

        public async Task<UploadUpdateViewModel> GetWithoutBlob(int id)
        {
            var file = await _context.Uploads.Select(x => new UploadUpdateViewModel
            {
                Id = x.Id,
                Category = x.Category
            }).FirstOrDefaultAsync();
            return file;
        }
    }
}
