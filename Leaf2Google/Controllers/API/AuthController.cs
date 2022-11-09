// Copyright (c) Nathan Ford. All rights reserved. APIController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Leaf2Google.Controllers.API;

[Route("api/[controller]/[action]/{id?}")]
[ApiController]
public class AuthController : BaseAPIController
{
    private readonly LeafContext _googleContext;

    public AuthController(BaseStorageManager storageManager, ICarSessionManager sessionManager, LeafContext googleContext)
        : base(storageManager, sessionManager)
    {
        _googleContext = googleContext;
    }

    [HttpPost]
    public async Task<ViewComponentResult> Delete([FromForm] Guid? authId)
    {
        if (SessionId != null && authId != null && await _googleContext.GoogleAuths.Include(auth => auth.Owner).AnyAsync(auth =>
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