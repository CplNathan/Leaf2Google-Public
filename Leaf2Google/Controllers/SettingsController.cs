using Leaf2Google.Contexts;
using Leaf2Google.Dependency.Car;
using Leaf2Google.Dependency.Google;
using Leaf2Google.Dependency.Google.Devices;
using Leaf2Google.Dependency.Helpers;
using Leaf2Google.Models.Google.Devices;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;

namespace Leaf2Google.Controllers
{
    public class SettingsController : BaseController
    {
        private readonly LeafContext _leafContext;

        private readonly GoogleStateManager _google;

        private readonly Captcha _captcha;

        public GoogleStateManager Google { get => _google; }

        public SettingsController(ILogger<HomeController> logger, LeafSessionManager sessions, LeafContext leafContext, GoogleStateManager google, Captcha captcha, IConfiguration configuration)
            : base(logger, sessions, configuration)
        {
            _leafContext = leafContext;
            _google = google;
            _captcha = captcha;
        }

        public async Task<IActionResult> Index()
        {
            ReloadViewBag();

            var session = Sessions.VehicleSessions[SessionId ?? Guid.Empty];

            if (session == null)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ThermostatModel? thermostat = (ThermostatModel?)Google.Devices[session.SessionId][typeof(ThermostatDevice)];
                LockModel? carlock = (LockModel?)Google.Devices[session.SessionId][typeof(LockDevice)];
                PointF? location = await Sessions.VehicleLocation(session.SessionId, session.PrimaryVin);

                return View("IndexUser");
            }
        }
    }
}