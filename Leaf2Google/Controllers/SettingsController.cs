using System.Drawing;
using Leaf2Google.Dependency.Google;
using Leaf2Google.Dependency.Google.Devices;
using Leaf2Google.Models.Generic;
using Leaf2Google.Models.Google.Devices;
using Microsoft.AspNetCore.Mvc;

namespace Leaf2Google.Controllers;

public class SettingsController : BaseController
{
    public GoogleStateManager Google { get; }

    public SettingsController(ICarSessionManager sessionManager, GoogleStateManager google)
        : base(sessionManager)
    {
        Google = google;
    }

    public async Task<IActionResult> Index()
    {
        ReloadViewBag();

        var session = SessionManager.VehicleSessions.FirstOrDefault(session => session.Key == SessionId).Value;

        if (session == null)
        {
            AddToast(new ToastViewModel
            {
                Colour = "warning",
                Message = "Please login first before accessing this page.",
                Title = "Authentication"
            });
            return RedirectToAction("Index", "Home");
        }

        var thermostat = (ThermostatModel?)Google.Devices[session.SessionId][typeof(ThermostatDevice)];
        var carlock = (LockModel?)Google.Devices[session.SessionId][typeof(LockDevice)];
        //PointF? location = await SessionManager.VehicleLocation(session.SessionId, session.PrimaryVin);

        return View("IndexUser");
    }
}