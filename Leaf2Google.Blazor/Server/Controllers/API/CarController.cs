// Copyright (c) Nathan Ford. All rights reserved. CarController.cs

using Leaf2Google.Models.Car;
using Leaf2Google.Models.Google.Devices;
using Leaf2Google.Services.Google;
using Leaf2Google.Services.Google.Devices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [Authorize]
    public async Task<JsonResult> Action([FromBody] ActionRequest actionRequest)
    {
        if (AuthenticatedSession != null)
        {
            var clampedDuration = actionRequest.Duration > 15 ? 15 : actionRequest.Duration < 5 ? 5 : actionRequest.Duration;

            switch (actionRequest.Action)
            {
                case ActionType.Lights:
                    {
                        await SessionManager.FlashLights(AuthenticatedSession, SelectedVin, clampedDuration).ConfigureAwait(false);
                        break;
                    }
                case ActionType.Horn:
                    {
                        await SessionManager.BeepHorn(AuthenticatedSession, SelectedVin, clampedDuration).ConfigureAwait(false);
                        break;
                    }
                case ActionType.Climate:
                    {
                        await SessionManager.SetVehicleClimate(AuthenticatedSession, SelectedVin, actionRequest.Duration > 0 ? actionRequest.Duration : 21, actionRequest.Duration > 0).ConfigureAwait(false);
                        break;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        return Json(null);
    }

    [HttpPost]
    [Authorize]
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
                        var lockModel = (LockModel)StorageManager.GoogleSessions[AuthenticatedSession.SessionId][typeof(LockDeviceService)];
                        var lockDevice = Devices.FirstOrDefault(x => x.GetType() == typeof(LockDeviceService));

                        if (lockDevice != null)
                        {
                            await lockDevice.FetchAsync(AuthenticatedSession, lockModel, activevin);
                        }

                        return Json(new BatteryData()
                        {
                            Charge = lockModel.CapacityRemaining,
                            Charging = lockModel.IsCharging
                        });
                    }
                case QueryType.Lock:
                    {
                        var lockModel = (LockModel)StorageManager.GoogleSessions[AuthenticatedSession.SessionId][typeof(LockDeviceService)];
                        var lockDevice = Devices.FirstOrDefault(x => x.GetType() == typeof(LockDeviceService));

                        if (lockDevice != null)
                        {
                            await lockDevice.FetchAsync(AuthenticatedSession, lockModel, activevin);
                        }

                        return Json(new LockData()
                        {
                            Locked = lockModel.Locked
                        });
                    }
                case QueryType.Location:
                    {
                        var lockModel = (LockModel)StorageManager.GoogleSessions[AuthenticatedSession.SessionId][typeof(LockDeviceService)];
                        var lockDevice = Devices.FirstOrDefault(x => x.GetType() == typeof(LockDeviceService));

                        if (lockDevice != null)
                        {
                            await lockDevice.FetchAsync(AuthenticatedSession, lockModel, activevin);
                        }

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
                        var thermostatModel = (ThermostatModel)StorageManager.GoogleSessions[AuthenticatedSession.SessionId][typeof(ThermostatDeviceService)];
                        var thermostatDevice = Devices.FirstOrDefault(x => x.GetType() == typeof(ThermostatDeviceService));

                        if (thermostatDevice != null)
                        {
                            await thermostatDevice.FetchAsync(AuthenticatedSession, thermostatModel, activevin);
                        }

                        return Json(new ClimateData()
                        {
                            TargetTemperature = ((int?)thermostatModel?.Target) ?? 21,
                            CurrentTemperature = ((int?)thermostatModel?.LastTemperature) ?? 21,
                            ClimateActive = thermostatModel?.Active ?? false
                        });
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        return Json(BadRequest());
    }
}