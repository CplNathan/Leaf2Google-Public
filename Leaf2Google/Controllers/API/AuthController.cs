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
    public class AuthController : BaseController
    {
        private readonly LeafContext _googleContext;

        public AuthController(ILogger<HomeController> logger, LeafSessionManager sessions, LeafContext googleContext, IConfiguration configuration)
        : base(logger, sessions, configuration)
        {
            _googleContext = googleContext;
        }

        [HttpPost]
        public async Task<ViewComponentResult> Delete([FromQuery] Guid? authId)
        {
            if (SessionId != null && authId != null && _googleContext.GoogleAuths.Any(auth => auth.Owner != null && auth.Owner.CarModelId == SessionId && auth.AuthId == authId && auth.Deleted == null))
            {
                var auth = _googleContext.GoogleAuths.First(auth => auth.AuthId == authId);
                auth.Deleted = DateTime.UtcNow;

                await _googleContext.SaveChangesAsync();
            }

            return ViewComponent("SessionInfo", new
            {
                sessionId = ViewBag?.SessionId ?? null
            });
        }
    }
}
