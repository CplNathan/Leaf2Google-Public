using Leaf2Google.Dependency.Managers;
using Leaf2Google.Models;
using Leaf2Google.Models.Google;
using Leaf2Google.Models.Nissan;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Leaf2Google.Controllers
{
    public class HomeController : BaseController
    {
        public HomeController(ILogger<HomeController> logger, LeafSessionManager sessions)
            : base(logger, sessions)
        {
        }

        public IActionResult Index()
        {
            ReloadViewBag();

            return View(Sessions.LeafSessions.FirstOrDefault(session => session.SessionId == SessionId));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index([FromForm] AuthPostForm authForm)
        {
            Func<NissanConnectSession, bool> authenticationPredicate = session =>
            {
                return session.Username == authForm.NissanUsername && session.Password == authForm.NissanPassword;
            };

            if (Sessions.LeafSessions.Any(authenticationPredicate))
            {
                var session = Sessions.LeafSessions.First(authenticationPredicate);
                SessionId = session.SessionId;
                ViewBag.SessionId = SessionId;
                SelectedVin = session.PrimaryVin;
                ViewBag.SelectedVin = SelectedVin;

                AddToast(new ToastViewModel() { Title = "Authentication", Message = "Authentication success." });
            }
            else
            {
                AddToast(new ToastViewModel() { Title = "Authentication", Message = "Authentication failed with the given credentials.", Colour = "warning" });
            }

            ReloadViewBag();

            return View(Sessions.LeafSessions.FirstOrDefault(authenticationPredicate));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        /*
        public async Task<IActionResult> FlashAll()
        {
            List<string> result = new List<string>();

            foreach (var session in Sessions.LeafSessions)
            {
                var response = await session.FlashLights(session.PrimaryVin);
                result.Add(JsonConvert.SerializeObject(response!.Data));
            }

            ViewBag.result = result;

            return View("Index");
        }

        public async Task<IActionResult> HvacStatus()
        {
            List<string> result = new List<string>();

            foreach (var session in Sessions.LeafSessions)
            {
                var response = await session.VehicleClimate(session.PrimaryVin);
                result.Add(JsonConvert.SerializeObject(response!.Data));
            }

            ViewBag.result = result;

            return View("Index");
        }
        */

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}