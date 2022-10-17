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
    private readonly IEnumerable<IDevice> _devices;
    private readonly GoogleStateManager _google;

    public CarController(ICarSessionManager sessionManager, GoogleStateManager google, IEnumerable<IDevice> devices)
        : base(sessionManager)
    {
        _google = google;
        _devices = devices;
    }

    [HttpPost]
    public JsonResult Action([FromForm] string action, [FromForm] int? duration)
    {
        var sessionId = SessionId ?? Guid.Empty;
        if (SessionId != null && action != null && duration != null &&
            SessionManager.VehicleSessions[sessionId] != null)
        {
            var clampedDuration = duration.Value > 15 ? 15 : duration.Value < 5 ? 5 : duration.Value;

            if (action == "flash")
                _ = SessionManager.FlashLights(sessionId, SelectedVin, clampedDuration);
            else if (action == "horn")
                _ = SessionManager.BeepHorn(sessionId, SelectedVin, clampedDuration);
        }

        return Json(null);
    }

    [HttpPost]
    public async Task<JsonResult> Status([FromForm] string query, [FromForm] string vin)
    {
        var sessionId = SessionId ?? Guid.Empty;
        if (SessionId != null && SessionManager.VehicleSessions.Any(session => session.Key == sessionId))
        {
            var activevin = vin.IsNullOrWhiteSpace() || vin == "null" ? SelectedVin : vin;

            if (query == "battery")
            {
                var carLock = (LockModel)_google.Devices[sessionId][typeof(LockDevice)];
                var deviceData = _devices.FirstOrDefault(x => x.GetType() == typeof(LockDevice));

                if (deviceData != null)
                    await deviceData.FetchAsync(sessionId, activevin);

                return Json(new
                {
                    percentage = carLock.CapacityRemaining,
                    charging = carLock.IsCharging
                });
            }
            else if (query == "location")
            {
                PointF? carLocation = await SessionManager.VehicleLocation(sessionId, activevin);
                return Json(new
                {
                    lat = carLocation?.X,
                    @long = carLocation?.Y
                });
            }
            else if (query == "climate")
            {
                var carThermostat = (ThermostatModel)_google.Devices[sessionId][typeof(ThermostatDevice)];
                var deviceData = _devices.FirstOrDefault(x => x.GetType() == typeof(ThermostatDevice));

                if (deviceData != null)
                    await deviceData.FetchAsync(sessionId, activevin);

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