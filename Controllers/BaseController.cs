using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;

namespace KeplerCMS.Controllers
{
    public class BaseController : Controller
    {

        [MaintenanceFilter]
        public BaseController()
        {
        }

    }
}
