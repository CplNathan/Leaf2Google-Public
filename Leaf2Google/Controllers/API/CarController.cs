// Copyright (c) Nathan Ford. All rights reserved. APIController.cs

using Leaf2Google.Contexts;
using Leaf2Google.Dependency.Car;
using Leaf2Google.Dependency.Google;
using Leaf2Google.Dependency.Google.Devices;
using Leaf2Google.Models.Google.Devices;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using NUglify.Helpers;

namespace Leaf2Google.Controllers.API
{
    [Route("api/[controller]/[action]/{id?}")]
    [ApiController]
    public class CarController : BaseAPIController
    {
        private readonly LeafContext _leafContext;

        private readonly GoogleStateManager _google;

        private readonly IEnumerable<IDevice> _devices;

        public CarController(ILogger<HomeController> logger, LeafSessionManager sessions, LeafContext leafContext, GoogleStateManager google, IEnumerable<IDevice> devices, IConfiguration configuration)
            : base(logger, sessions, configuration)
        {
            _leafContext = leafContext;
            _google = google;
            _devices = devices;
        }

        [HttpPost]
        public async Task<ViewComponentResult> Action([FromQuery] string? action, [FromQuery] int? duration)
        {
            var sessionId = SessionId ?? Guid.Empty;
            if (SessionId != null && action != null && duration != null && Sessions.VehicleSessions[sessionId] != null)
            {
                var clampedDuration = duration.Value > 15 ? 15 : duration.Value < 5 ? 5 : duration.Value;

                if (action == "flash")
                    await Sessions.FlashLights(sessionId, SelectedVin, clampedDuration);
                else if (action == "horn")
                    await Sessions.BeepHorn(sessionId, SelectedVin, clampedDuration);
            }

            return ViewComponent("SessionInfo", new
            {
                sessionId = sessionId
            });
        }

        [HttpPost]
        public async Task<ActionResult> Status([FromBody] JObject body)
        {
            var sessionId = SessionId ?? Guid.Empty;
            if (SessionId != null && body["query"] != null && Sessions.VehicleSessions[sessionId] != null)
            {
                string? vin = body["vin"]?.ToString().IsNullOrWhiteSpace() ?? true ? SelectedVin : body["vin"]?.ToString();

                if (body["query"]?.ToString() == "battery")
                {
                    LockModel carLock = (LockModel)_google.Devices[sessionId][typeof(LockDevice)];
                    if (carLock != null)
                    {
                        var deviceData = _devices.FirstOrDefault(x => x.GetType() == typeof(LockDevice));

                        if (deviceData != null)
                            await deviceData.FetchAsync(sessionId, vin);

                        return Json(new
                        {
                            percentage = carLock.CapacityRemaining,
                            charging = carLock.IsCharging
                        });
                    }
                }
            };

            return BadRequest();
        }
    }
}