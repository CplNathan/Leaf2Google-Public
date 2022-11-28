// Copyright (c) Nathan Ford. All rights reserved. APIController.cs

using System.Drawing;
using Leaf2Google.Services.Google;
using Leaf2Google.Services.Google.Devices;
using Leaf2Google.Models.Google.Devices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Leaf2Google.Models.Car;

namespace Leaf2Google.Controllers.API;

[ApiController]
[Route("api/[controller]/[action]/{id?}")]
[Authorize]
public class CarController : BaseAPIController
{
    private IEnumerable<IDevice> Devices { get; }
    private GoogleStateService Google { get; }

    public CarController(BaseStorageService storageManager, ICarSessionManager sessionManager, GoogleStateService google, IEnumerable<IDevice> devices)
        : base(storageManager, sessionManager)
    {
        Google = google;
        Devices = devices;
    }

    [HttpPost]
    public async Task<JsonResult> Action([FromForm] string action, [FromForm] int? duration)
    {
        if (AuthenticatedSession != null && action != null && duration != null &&
        StorageManager.VehicleSessions[AuthenticatedSession.SessionId] != null)
        {
            var clampedDuration = duration.Value > 15 ? 15 : duration.Value < 5 ? 5 : duration.Value;

            if (action == "flash")
                await SessionManager.FlashLights(AuthenticatedSession, SelectedVin, clampedDuration).ConfigureAwait(false);
            else if (action == "horn")
                await SessionManager.BeepHorn(AuthenticatedSession, SelectedVin, clampedDuration).ConfigureAwait(false);
        }

        return Json(null);
    }

    [HttpPost]
    public async Task<JsonResult> Query([FromBody] QueryRequest queryRequest)
    {
        if (AuthenticatedSession != null)
        {
            var activevin = queryRequest.ActiveVin ?? AuthenticatedSession.PrimaryVin;

            switch (queryRequest.QueryType)
            {
                case QueryType.PrimaryVin:
                    {
                        return Json(AuthenticatedSession.PrimaryVin);
                    }
                case QueryType.Battery:
                    {
                        var lockModel = (LockModel)(StorageManager.GoogleSessions)[AuthenticatedSession.SessionId][typeof(LockDeviceService)];
                        var lockDevice = Devices.FirstOrDefault(x => x.GetType() == typeof(LockDeviceService));

                        if (lockDevice != null)
                            await lockDevice.FetchAsync(AuthenticatedSession, lockModel, activevin);

                        return Json(new BatteryData()
                        {
                            Charge = lockModel.CapacityRemaining,
                            Charging = lockModel.IsCharging
                        });
                    }
                case QueryType.Location:
                    {
                        var lockModel = (LockModel)(StorageManager.GoogleSessions)[AuthenticatedSession.SessionId][typeof(LockDeviceService)];
                        var lockDevice = Devices.FirstOrDefault(x => x.GetType() == typeof(LockDeviceService));

                        if (lockDevice != null)
                            await lockDevice.FetchAsync(AuthenticatedSession, lockModel, activevin);

                        return Json(new
                        {
                            lat = lockModel.Location?.X,
                            @long = lockModel.Location?.Y
                        });
                    }
                case QueryType.Photo:
                    {
                        return Json(AuthenticatedSession.CarPictureUrl);
                    }
                case QueryType.Climate:
                    {
                        var thermostatModel = (ThermostatModel)(StorageManager.GoogleSessions)[AuthenticatedSession.SessionId][typeof(ThermostatDeviceService)];
                        var thermostatDevice = Devices.FirstOrDefault(x => x.GetType() == typeof(ThermostatDeviceService));

                        if (thermostatDevice != null)
                            await thermostatDevice.FetchAsync(AuthenticatedSession, thermostatModel, activevin);

                        return Json(new ClimateData()
                        {
                            TargetTemperature = ((int?)thermostatModel?.Target) ?? 21,
                            CurrentTemperature = ((int?)thermostatModel?.LastTemperature) ?? 21,
                            ClimateActive = thermostatModel.Active
                        });
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        return Json(BadRequest());
    }
}