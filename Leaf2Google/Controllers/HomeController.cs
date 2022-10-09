using System.Drawing;
using Leaf2Google.Dependency;
using Leaf2Google.Dependency.Google;
using Leaf2Google.Dependency.Google.Devices;
using Leaf2Google.Dependency.Helpers;
using Leaf2Google.Models.Car;
using Leaf2Google.Models.Generic;
using Leaf2Google.Models.Google;
using Leaf2Google.Models.Google.Devices;
using Microsoft.AspNetCore.Mvc;

namespace Leaf2Google.Controllers;

public class HomeController : BaseController
{
    protected Captcha Captcha { get; }

    protected LeafContext LeafContext { get; }

    protected BaseStorageManager StorageManager { get; }

    public HomeController(ICarSessionManager sessionManager, BaseStorageManager storageManager, LeafContext leafContext, LoggingManager logging,
        GoogleStateManager google, Captcha captcha)
        : base(sessionManager)
    {
        LeafContext = leafContext;
        StorageManager = storageManager;
        Logging = logging;
        Google = google;
        Captcha = captcha;
    }

    protected LoggingManager Logging { get; }

    public GoogleStateManager Google { get; }

    public async Task<IActionResult> Index()
    {
        ReloadViewBag();

        var session = SessionManager.VehicleSessions.GetValueOrDefault(SessionId ?? Guid.Empty);

        if (session == null)
            return View("Index", new CarViewModel
            {
                car = LeafContext.NissanLeafs.FirstOrDefault(car => car.CarModelId == SessionId) ?? new CarModel()
            });

        var carThermostat = (ThermostatModel?)Google.Devices[session.SessionId][typeof(ThermostatDevice)];
        var carLock = (LockModel?)Google.Devices[session.SessionId][typeof(LockDevice)];
        PointF? location = await SessionManager.VehicleLocation(session.SessionId, session.PrimaryVin);

        return View("IndexUser", new CarViewModel
        {
            car = LeafContext.NissanLeafs.FirstOrDefault(car => car.CarModelId == SessionId),
            carLock = carLock,
            carThermostat = carThermostat,
            carLocation = location
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index([FromForm] AuthPostFormModel authForm)
    {
        if (SessionId != null)
            return RedirectToAction("Index", "Home");

        var captchaStatus = await Captcha.VerifyCaptcha(authForm.Captcha, HttpContext.Request.Host.Host);

        var sessionId = await StorageManager.UserStorage.LoginUser(authForm.NissanUsername, authForm.NissanPassword);
        if (captchaStatus)
        {
            if (sessionId != Guid.Empty)
            {
                SessionId = sessionId;
                ViewBag.SessionId = SessionId;
                SelectedVin = SessionManager.AllSessions[sessionId].PrimaryVin;
                ViewBag.SelectedVin = SelectedVin;

                AddToast(new ToastViewModel { Title = "Authentication", Message = "Authentication success." });
                Console.WriteLine(await Logging.AddLog(Guid.Empty, AuditAction.Access, AuditContext.Account,
                    $"Login success for {authForm.NissanUsername}"));
            }
            else
            {
                AddToast(new ToastViewModel
                {
                    Title = "Authentication",
                    Message = "Authentication failed with the given credentials.",
                    Colour = "warning"
                });

                Console.WriteLine(await Logging.AddLog(Guid.Empty, AuditAction.Access, AuditContext.Account,
                    $"Login failed for {authForm.NissanUsername}"));
            }
        }
        else
        {
            AddToast(new ToastViewModel
            { Title = "Authentication", Message = "Failed to verify reCaptcha response.", Colour = "warning" });
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
        AddToast(new ToastViewModel
        {
            Title = "System Error",
            Message = $"There was an error with your last request, please try again ({HttpContext.TraceIdentifier}).",
            Colour = "error"
        });

        ReloadViewBag();
        return Index();
    }
}