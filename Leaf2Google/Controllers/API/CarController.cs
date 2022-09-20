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

        public CarController(ILogger<HomeController> logger, LeafSessionManager sessions, LeafContext googleContext)
        : base(logger, sessions)
        {
            _googleContext = googleContext;
        }

        [HttpPost]
        public async Task<ViewComponentResult> Action([FromQuery] string? action, [FromQuery] int? duration)
        {
            if (SessionId != null && action != null && duration != null && Sessions.LeafSessions.Any(session => session.SessionId == SessionId))
            {
                var session = Sessions.LeafSessions.First(session => session.SessionId == SessionId);
                var clampedDuration = duration.Value > 15 ? 15 : duration.Value < 5 ? 5 : duration.Value;

                if (action == "flash")
                    await session.FlashLights(SelectedVin, clampedDuration);
                else if (action == "horn")
                    await session.BeepHorn(SelectedVin, clampedDuration);
            }

            return ViewComponent("SessionInfo", new
            {
                sessionId = ViewBag?.SessionId ?? null
            });
        }
    }
}
