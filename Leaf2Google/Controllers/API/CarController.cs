// Copyright (c) Nathan Ford. All rights reserved. APIController.cs

using Leaf2Google.Contexts;
using Leaf2Google.Dependency.Managers;
using Leaf2Google.Models.Google.Devices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using static System.Net.Mime.MediaTypeNames;

namespace Leaf2Google.Controllers.API
{
    [Route("api/[controller]/[action]/{id?}")]
    [ApiController]
    public class CarController : BaseAPIController
    {
        private readonly LeafContext _leafContext;

        private readonly GoogleStateManager _google;

        public CarController(ILogger<HomeController> logger, LeafSessionManager sessions, LeafContext leafContext, GoogleStateManager google, IConfiguration configuration)
            : base(logger, sessions, configuration)
        {
            _leafContext = leafContext;
            _google = google;
        }

        [HttpPost]
        public async Task<ViewComponentResult> Action([FromQuery] string? action, [FromQuery] int? duration)
        {
            if (SessionId != null && action != null && duration != null && Sessions.VehicleSessions.Any(session => session.SessionId == SessionId))
            {
                var session = Sessions.VehicleSessions.First(session => session.SessionId == SessionId);
                var clampedDuration = duration.Value > 15 ? 15 : duration.Value < 5 ? 5 : duration.Value;

                if (action == "flash")
                    await Sessions.FlashLights(session.SessionId, SelectedVin, clampedDuration);
                else if (action == "horn")
                    await Sessions.BeepHorn(session.SessionId, SelectedVin, clampedDuration);
            }

            return ViewComponent("SessionInfo", new
            {
                sessionId = ViewBag?.SessionId ?? null
            });
        }

        [HttpPost]
        public async Task<ActionResult> Status([FromBody] JObject body)
        {
            if (SessionId != null && body["query"] != null && Sessions.VehicleSessions.Any(session => session.SessionId == SessionId))
            {
                var session = Sessions.VehicleSessions.First(session => session.SessionId == SessionId);
                string? vin = body["vin"]?.ToString() ?? session.PrimaryVin;

                if (body["query"]?.ToString() == "battery")
                {
                    Lock? carlock = (Lock?)_google.Devices[session.SessionId].FirstOrDefault(device => device is Lock);
                    if (carlock != null)
                    {
                        await carlock.QueryAsync(Sessions, session, vin);
                        return Json(new
                        {
                            percentage = carlock.CapacityRemaining,
                            charging = carlock.IsCharging
                        });
                    }
                }
            };

            return BadRequest();
        }
    }
}
