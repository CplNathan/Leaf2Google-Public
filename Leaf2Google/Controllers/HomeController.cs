using Leaf2Google.Dependency;
using Leaf2Google.Dependency.Google;
using Leaf2Google.Dependency.Google.Devices;
using Leaf2Google.Dependency.Helpers;
using Leaf2Google.Models.Car;
using Leaf2Google.Models.Generic;
using Leaf2Google.Models.Google;
using Leaf2Google.Models.Google.Devices;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;

namespace Leaf2Google.Controllers
{
    public class HomeController : BaseController
    {
        private readonly LeafContext _leafContext;

        private readonly LoggingManager _logging;

        protected LoggingManager Logging { get => _logging; }

        private readonly GoogleStateManager _google;

        private readonly Captcha _captcha;

        public GoogleStateManager Google { get => _google; }

        public HomeController(ICarSessionManager sessionManager, LeafContext leafContext, LoggingManager logging, GoogleStateManager google, Captcha captcha)
            : base(sessionManager)
        {
            _leafContext = leafContext;
            _logging = logging;
            _google = google;
            _captcha = captcha;
        }

        public async Task<IActionResult> Index()
        {
            ReloadViewBag();

            var session = SessionManager.VehicleSessions.GetValueOrDefault(SessionId ?? Guid.Empty);

            if (session == null)
            {
                return View("Index", new CarViewModel()
                {
                    car = _leafContext.NissanLeafs.FirstOrDefault(car => car.CarModelId == SessionId) ?? new CarModel()
                });
            }
            else
            {
                ThermostatModel? carThermostat = (ThermostatModel?)Google.Devices[session.SessionId][typeof(ThermostatDevice)];
                LockModel? carLock = (LockModel?)Google.Devices[session.SessionId][typeof(LockDevice)];
                PointF? location = await SessionManager.VehicleLocation(session.SessionId, session.PrimaryVin);

                return View("IndexUser", new CarViewModel()
                {
                    car = _leafContext.NissanLeafs.FirstOrDefault(car => car.CarModelId == SessionId),
                    carLock = carLock,
                    carThermostat = carThermostat,
                    carLocation = location
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([FromForm] AuthPostFormModel authForm)
        {
            Func<KeyValuePair<Guid, VehicleSessionBase>, bool> authenticationPredicate = session =>
            {
                return session.Value.Username == authForm.NissanUsername && session.Value.Password == authForm.NissanPassword;
            };

            if (SessionId != null)
                return RedirectToAction("Index", "Home");

            var captchaStatus = await _captcha.VerifyCaptcha(authForm.Captcha, HttpContext.Request.Host.Host);

            if (SessionManager.VehicleSessions.Any(authenticationPredicate) && captchaStatus)
            {
                var session = SessionManager.VehicleSessions.First(authenticationPredicate);
                SessionId = session.Key;
                ViewBag.SessionId = SessionId;
                SelectedVin = session.Value.PrimaryVin;
                ViewBag.SelectedVin = SelectedVin;

                AddToast(new ToastViewModel() { Title = "Authentication", Message = "Authentication success." });
                Console.WriteLine(await Logging.AddLog(Guid.Empty, AuditAction.Access, AuditContext.Account, $"Login success for {authForm.NissanUsername}"));
            }
            else
            {
                if (captchaStatus)
                    AddToast(new ToastViewModel() { Title = "Authentication", Message = "Authentication failed with the given credentials.", Colour = "warning" });
                else
                    AddToast(new ToastViewModel() { Title = "Authentication", Message = "Failed to verify reCaptcha response.", Colour = "warning" });

                Console.WriteLine(await Logging.AddLog(Guid.Empty, AuditAction.Access, AuditContext.Account, $"Login failed for {authForm.NissanUsername}"));
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public Task<IActionResult> Error()
        {
            AddToast(new ToastViewModel() { Title = "System Error", Message = $"There was an error with your last request, please try again ({HttpContext.TraceIdentifier}).", Colour = "error" });

            ReloadViewBag();
            return Index();
        }
    }
}