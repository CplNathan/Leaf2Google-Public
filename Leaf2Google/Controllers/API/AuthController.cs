﻿// Copyright (c) Nathan Ford. All rights reserved. APIController.cs

using Microsoft.AspNetCore.Mvc;

namespace Leaf2Google.Controllers.API;

[Route("api/[controller]/[action]/{id?}")]
[ApiController]
public class AuthController : BaseAPIController
{
    private readonly LeafContext _googleContext;

    public AuthController(ICarSessionManager sessionManager, LeafContext googleContext)
        : base(sessionManager)
    {
        _googleContext = googleContext;
    }

    [HttpPost]
    public async Task<ViewComponentResult> Delete([FromForm] Guid? authId)
    {
        if (SessionId != null && authId != null && _googleContext.GoogleAuths.Any(auth =>
                auth.Owner != null && auth.Owner.CarModelId == SessionId && auth.AuthId == authId &&
                auth.Deleted == null))
        {
            var auth = _googleContext.GoogleAuths.First(auth => auth.AuthId == authId);
            auth.Deleted = DateTime.UtcNow;

            await _googleContext.SaveChangesAsync();
        }

        return ViewComponent("SessionInfo", new
        {
            viewName = "Auths",
            sessionId = ViewBag?.SessionId ?? null
        });
    }
}