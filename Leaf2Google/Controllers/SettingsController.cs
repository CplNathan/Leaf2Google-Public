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

    public SettingsController(BaseStorageManager storageManager, GoogleStateManager google)
        : base(storageManager)
    {
        Google = google;
    }

    public IActionResult Index()
    {
        ReloadViewBag();

        var session = StorageManager.VehicleSessions.FirstOrDefault(session => session.Key == SessionId).Value;

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

        var thermostat = (ThermostatModel?)(StorageManager.GoogleSessions)[session.SessionId][typeof(ThermostatDevice)];
        var carlock = (LockModel?)(StorageManager.GoogleSessions)[session.SessionId][typeof(LockDevice)];

        return View("IndexUser");
    }
}