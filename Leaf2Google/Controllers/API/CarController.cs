// Copyright (c) Nathan Ford. All rights reserved. APIController.cs

using Leaf2Google.Dependency;
using Leaf2Google.Dependency.Google;
using Leaf2Google.Dependency.Google.Devices;
using Leaf2Google.Models.Google.Devices;
using Microsoft.AspNetCore.Mvc;
using NUglify.Helpers;
using System.Drawing;

namespace Leaf2Google.Controllers.API
{
    [Route("api/[controller]/[action]/{id?}")]
    [ApiController]
    public class CarController : BaseAPIController
    {
        private readonly GoogleStateManager _google;

        private readonly IEnumerable<IDevice> _devices;

        public CarController(ICarSessionManager sessionManager, GoogleStateManager google, IEnumerable<IDevice> devices)
            : base(sessionManager)
        {
            _google = google;
            _devices = devices;
        }

        [HttpPost]
        public async Task<JsonResult> Action([FromForm] string action, [FromForm] int? duration)
        {
            var sessionId = SessionId ?? Guid.Empty;
            if (SessionId != null && action != null && duration != null && SessionManager.VehicleSessions[sessionId] != null)
            {
                var clampedDuration = duration.Value > 15 ? 15 : duration.Value < 5 ? 5 : duration.Value;

                if (action == "flash")
                    await SessionManager.FlashLights(sessionId, SelectedVin, clampedDuration);
                else if (action == "horn")
                    await SessionManager.BeepHorn(sessionId, SelectedVin, clampedDuration);
            }

            return Json(null);
        }

        [HttpPost]
        public async Task<JsonResult> Status([FromForm] string query, [FromForm] string vin)
        {
            var sessionId = SessionId ?? Guid.Empty;
            if (SessionId != null && query != null && SessionManager.VehicleSessions[sessionId] != null)
            {
                string? activevin = vin.IsNullOrWhiteSpace() || vin == "null" ? SelectedVin : vin;

                if (query == "battery")
                {
                    LockModel carLock = (LockModel)_google.Devices[sessionId][typeof(LockDevice)];
                    if (carLock != null)
                    {
                        var deviceData = _devices.FirstOrDefault(x => x.GetType() == typeof(LockDevice));

                        if (deviceData != null)
                            await deviceData.FetchAsync(sessionId, activevin);

                        return Json(new
                        {
                            percentage = carLock.CapacityRemaining,
                            charging = carLock.IsCharging
                        });
                    }
                }
                else if (query == "location")
                {
                    PointF? carLocation = await SessionManager.VehicleLocation(sessionId, activevin);
                    if (carLocation != null)
                    {
                        return Json(new
                        {
                            lat = carLocation?.X,
                            @long = carLocation?.Y
                        });
                    }
                }
                else if (query == "climate")
                {
                    ThermostatModel carThermostat = (ThermostatModel)_google.Devices[sessionId][typeof(ThermostatDevice)];
                    if (carThermostat != null)
                    {
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
            };

            return Json(BadRequest());
        }
    }
}