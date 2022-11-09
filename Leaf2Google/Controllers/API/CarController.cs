// Copyright (c) Nathan Ford. All rights reserved. APIController.cs

using System.Drawing;
using Leaf2Google.Dependency.Google;
using Leaf2Google.Dependency.Google.Devices;
using Leaf2Google.Models.Google.Devices;
using Microsoft.AspNetCore.Mvc;
using NUglify.Helpers;

namespace Leaf2Google.Controllers.API;

[Route("api/[controller]/[action]/{id?}")]
[ApiController]
public class CarController : BaseAPIController
{
    private IEnumerable<IDevice> Devices { get; }
    private GoogleStateManager Google { get; }

    public CarController(BaseStorageManager storageManager, ICarSessionManager sessionManager, GoogleStateManager google, IEnumerable<IDevice> devices)
        : base(storageManager, sessionManager)
    {
        Google = google;
        Devices = devices;
    }

    [HttpPost]
    public async Task<JsonResult> Action([FromForm] string action, [FromForm] int? duration)
    {
        var sessionId = SessionId ?? Guid.Empty;
        var session = StorageManager.VehicleSessions.GetValueOrDefault(sessionId);

        if (sessionId != Guid.Empty && session != null && action != null && duration != null &&
        StorageManager.VehicleSessions[sessionId] != null)
        {
            var clampedDuration = duration.Value > 15 ? 15 : duration.Value < 5 ? 5 : duration.Value;

            if (action == "flash")
                await SessionManager.FlashLights(session, SelectedVin, clampedDuration).ConfigureAwait(false);
            else if (action == "horn")
                await SessionManager.BeepHorn(session, SelectedVin, clampedDuration).ConfigureAwait(false);
        }

        return Json(null);
    }

    [HttpPost]
    public async Task<JsonResult> Status([FromForm] string query, [FromForm] string vin)
    {
        var sessionId = SessionId ?? Guid.Empty;
        var session = StorageManager.VehicleSessions.GetValueOrDefault(sessionId);

        if (sessionId != Guid.Empty && session != null)
        {
            var activevin = vin.IsNullOrWhiteSpace() || vin == "null" ? SelectedVin : vin;

            if (query == "battery")
            {
                var carLock = (LockModel)(StorageManager.GoogleSessions)[sessionId][typeof(LockDevice)];
                var deviceData = Devices.FirstOrDefault(x => x.GetType() == typeof(LockDevice));

                if (deviceData != null)
                    await deviceData.FetchAsync(session, activevin);

                return Json(new
                {
                    percentage = carLock.CapacityRemaining,
                    charging = carLock.IsCharging
                });
            }
            else if (query == "location")
            {
                var carLock = (LockModel)(StorageManager.GoogleSessions)[sessionId][typeof(LockDevice)];
                var deviceData = Devices.FirstOrDefault(x => x.GetType() == typeof(LockDevice));

                return Json(new
                {
                    lat = carLock.Location?.X,
                    @long = carLock.Location?.Y
                });
            }
            else if (query == "climate")
            {
                var carThermostat = (ThermostatModel)(StorageManager.GoogleSessions)[session.SessionId][typeof(ThermostatDevice)];
                var deviceData = Devices.FirstOrDefault(x => x.GetType() == typeof(ThermostatDevice));

                if (deviceData != null)
                    await deviceData.FetchAsync(session, activevin);

                return Json(new
                {
                    target = carThermostat?.Target,
                    current = carThermostat?.LastTemperature
                });
            }
        }

        ;

        return Json(BadRequest());
    }
}