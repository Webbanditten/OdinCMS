using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using System.Collections.Generic;

namespace KeplerCMS.Services.Interfaces
{
    public interface IPageService
    {
        public Page GetPageBySlug(string slug);
    }
}
