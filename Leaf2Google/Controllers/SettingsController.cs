using Leaf2Google.Dependency;
using Leaf2Google.Dependency.Google;
using Leaf2Google.Dependency.Google.Devices;
using Leaf2Google.Models.Google.Devices;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;

namespace Leaf2Google.Controllers
{
    public class SettingsController : BaseController
    {
        private readonly LeafContext _leafContext;

        private readonly GoogleStateManager _google;

        public GoogleStateManager Google { get => _google; }

        public SettingsController(ICarSessionManager sessionManager, LeafContext leafContext, GoogleStateManager google)
            : base(sessionManager)
        {
            _leafContext = leafContext;
            _google = google;
        }

        public async Task<IActionResult> Index()
        {
            ReloadViewBag();

            var session = SessionManager.VehicleSessions.FirstOrDefault(session => session.Key == SessionId).Value;

            if (session == null)
            {
                AddToast(new Models.Generic.ToastViewModel()
                {
                    Colour = "warning",
                    Message = "Please login first before accessing this page.",
                    Title = "Authentication"
                });
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ThermostatModel? thermostat = (ThermostatModel?)Google.Devices[session.SessionId][typeof(ThermostatDevice)];
                LockModel? carlock = (LockModel?)Google.Devices[session.SessionId][typeof(LockDevice)];
                PointF? location = await SessionManager.VehicleLocation(session.SessionId, session.PrimaryVin);

                return View("IndexUser");
            }
        }
    }
}