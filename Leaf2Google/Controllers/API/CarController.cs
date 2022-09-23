// Copyright (c) Nathan Ford. All rights reserved. APIController.cs

using Leaf2Google.Contexts;
using Leaf2Google.Dependency.Managers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static System.Net.Mime.MediaTypeNames;

namespace Leaf2Google.Controllers.API
{
    [Route("api/[controller]/[action]/{id?}")]
    [ApiController]
    public class CarController : BaseController
    {
        private readonly LeafContext _googleContext;

        public CarController(ILogger<HomeController> logger, LeafSessionManager sessions, LeafContext googleContext, IConfiguration configuration)
            : base(logger, sessions, configuration)
        {
            _googleContext = googleContext;
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
    }
}
