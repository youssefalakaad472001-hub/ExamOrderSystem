using ExamRFID.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ExamRFID.Controllers
{
    // The home page, the privacy page and the two error pages.
    public class HomeController : Controller
    {
        // GET / - shows the dashboard with links to Customers, Orders and Performance.
        // Used by: the app name and "Dashboard" in the sidebar.
        public IActionResult Index()
        {
            return View();
        }

        // GET /Home/Privacy - shows the privacy page.
        // Used by: the "Privacy" link at the bottom of the sidebar.
        public IActionResult Privacy()
        {
            return View();
        }

        // Shows the friendly "Not found" page for 404 and other error codes.
        // Called by UseStatusCodePagesWithReExecute in Program.cs (not by a link).
        public IActionResult HttpError(int code)
        {
            ViewBag.Code = code;

            return View();
        }

        // Shows a general error page without technical details.
        // Called by UseExceptionHandler in Program.cs when the app is not in Development.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
