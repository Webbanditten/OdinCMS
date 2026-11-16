using System.Diagnostics;
using KeplerCMS.Models;
using Microsoft.AspNetCore.Mvc;

namespace KeplerCMS.Controllers
{
    /// <summary>
    /// Target of UseExceptionHandler("/error") outside Development. Deliberately has no filters or
    /// service dependencies, so it still renders when the failure was in the DB or a filter.
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorController : Controller
    {
        [Route("/error")]
        public IActionResult Index()
        {
            Response.StatusCode = 500;
            return View("~/Views/Shared/Error.cshtml", new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
