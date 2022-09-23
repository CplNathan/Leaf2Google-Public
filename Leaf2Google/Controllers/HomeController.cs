﻿using Leaf2Google.Dependency.Managers;
using Leaf2Google.Models;
using Leaf2Google.Models.Google;
using Leaf2Google.Models.Car;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;
using Leaf2Google.Models.Google.Devices;
using System.Drawing;
using Leaf2Google.Contexts;
using Leaf2Google.Dependency.Helpers;

namespace Leaf2Google.Controllers
{
    public class HomeController : BaseController
    {
        private readonly LeafContext _leafContext;

        private readonly GoogleStateManager _google;

        private readonly Captcha _captcha;

        public GoogleStateManager Google { get => _google; }

        public HomeController(ILogger<HomeController> logger, LeafSessionManager sessions, LeafContext leafContext, GoogleStateManager google, Captcha captcha, IConfiguration configuration)
            : base(logger, sessions, configuration)
        {
            _leafContext = leafContext;
            _google = google;
            _captcha = captcha;
        }

        public async Task<IActionResult> Index()
        {
            ReloadViewBag();

            var session = Sessions.VehicleSessions.FirstOrDefault(session => session.SessionId == SessionId);

            if (session == null)
            {
                RegisterViewComponentScript("/js/Partials/AuthenticationForm.js");

                return View(new CarInfoModel()
                {
                    car = _leafContext.NissanLeafs.FirstOrDefault(car => car.CarModelId == SessionId) ?? new CarModel()
                });
            }
            else
            {
                Thermostat? thermostat = (Thermostat?)Google.Devices[session.SessionId].FirstOrDefault(device => device is Thermostat);
                Lock? carlock = (Lock?)Google.Devices[session.SessionId].FirstOrDefault(device => device is Lock);
                PointF? location = await Sessions.VehicleLocation(session.SessionId, session.PrimaryVin);

                return View("IndexUser", new CarInfoModel()
                {
                    car = _leafContext.NissanLeafs.FirstOrDefault(car => car.CarModelId == SessionId),
                    carlock = carlock,
                    thermostat = thermostat,
                    location = location
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([FromForm] AuthPostForm authForm)
        {
            Func<VehicleSessionBase, bool> authenticationPredicate = session =>
            {
                return session.Username == authForm.NissanUsername && session.Password == authForm.NissanPassword;
            };

            var captchaStatus = await _captcha.VerifyCaptcha(authForm.Captcha, HttpContext.Request.Host.Host);

            if (Sessions.VehicleSessions.Any(authenticationPredicate) && captchaStatus)
            {
                var session = Sessions.VehicleSessions.First(authenticationPredicate);
                SessionId = session.SessionId;
                ViewBag.SessionId = SessionId;
                SelectedVin = session.PrimaryVin;
                ViewBag.SelectedVin = SelectedVin;

                AddToast(new ToastViewModel() { Title = "Authentication", Message = "Authentication success." });
            }
            else
            {
                if (captchaStatus)
                    AddToast(new ToastViewModel() { Title = "Authentication", Message = "Authentication failed with the given credentials.", Colour = "warning" });
                else
                    AddToast(new ToastViewModel() { Title = "Authentication", Message = "Failed to verify reCaptcha response.", Colour = "warning" });
            }

            ReloadViewBag();
            return await Index();
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
    }
}