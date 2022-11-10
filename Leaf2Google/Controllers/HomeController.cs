using System.Drawing;
using Leaf2Google.Dependency.Google;
using Leaf2Google.Dependency.Google.Devices;
using Leaf2Google.Dependency.Helpers;
using Leaf2Google.Entities.Generic;
using Leaf2Google.Entities.Car;
using Leaf2Google.Models.Car;
using Leaf2Google.Models.Generic;
using Leaf2Google.Models.Google;
using Leaf2Google.Models.Google.Devices;
using Microsoft.AspNetCore.Mvc;

namespace Leaf2Google.Controllers;

public class HomeController : BaseController
{
    protected ICarSessionManager SessionManager { get; }

    protected IEnumerable<IDevice> Devices { get; }

    protected Captcha Captcha { get; }

    public HomeController(BaseStorageManager storageManager, ICarSessionManager sessionManager, IEnumerable<IDevice> devices, LoggingManager logging,
        GoogleStateManager google, Captcha captcha)
        : base(storageManager)
    {
        SessionManager = sessionManager;
        Devices = devices;
        Logging = logging;
        Google = google;
        Captcha = captcha;
    }

    protected LoggingManager Logging { get; }

    public GoogleStateManager Google { get; }

    public async Task<IActionResult> Index()
    {
        ReloadViewBag();

        var session = StorageManager.VehicleSessions.GetValueOrDefault(SessionId ?? Guid.Empty);

        if (session == null || !StorageManager.GoogleSessions.ContainsKey(session.SessionId))
        {
            return View("Index", new CarViewModel
            {
                car = await StorageManager.UserStorage.GetUser(SessionId ?? Guid.Empty) ?? new CarModel()
            });
        }

        if (!session?.Authenticated ?? false)
            await SessionManager.Login(session);


        foreach (var device in Devices)
        {
            await device.FetchAsync(session, session.PrimaryVin);
        }

        var carThermostat = (ThermostatModel?)(StorageManager.GoogleSessions)[session.SessionId][typeof(ThermostatDevice)];
        var carLock = (LockModel?)(StorageManager.GoogleSessions)[session.SessionId][typeof(LockDevice)];
        PointF? location = StorageManager.VehicleSessions[session.SessionId].LastLocation.Item2;

        return View("IndexUser", new CarViewModel
        {
            car = await StorageManager.UserStorage.GetUser(SessionId ?? Guid.Empty) ?? new CarModel(),
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
        var sessionIdTask = StorageManager.UserStorage.LoginUser(authForm.NissanUsername, authForm.NissanPassword);

        if (captchaStatus)
        {
            var sessionId = await sessionIdTask;

            if (sessionId != Guid.Empty)
            {
                SessionId = sessionId;
                ViewBag.SessionId = sessionId;
                SelectedVin = StorageManager.VehicleSessions.GetValueOrDefault(sessionId)?.PrimaryVin;
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