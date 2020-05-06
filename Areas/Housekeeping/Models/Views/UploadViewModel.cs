using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.Housekeeping.Models.Views
{
    public class UploadViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string ContentType { get; set; }
        public string Category { get; set; }
    }
    public class UploadAddViewModel
    {
        [Required]
        public List<IFormFile> Files { get; set; }
        [Required]
        public string Category { get; set; }
    }

    public class UploadUpdateViewModel
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Category { get; set; }
    }


}
