using Leaf2Google.Contexts;
using Leaf2Google.Dependency.Helpers;
using Leaf2Google.Dependency.Managers;
using Leaf2Google.Models;
using Leaf2Google.Models.Car;
using Leaf2Google.Models.Google;
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
                Thermostat? thermostat = (Thermostat?)Google.Devices[session.SessionId].FirstOrDefault(device => device is Thermostat);
                Lock? carlock = (Lock?)Google.Devices[session.SessionId].FirstOrDefault(device => device is Lock);
                PointF? location = await Sessions.VehicleLocation(session.SessionId, session.PrimaryVin);

                return View("IndexUser");
            }
        }
    }
}