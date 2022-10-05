// Copyright (c) Nathan Ford. All rights reserved. SessionInfoViewModel.cs

using Leaf2Google.Models.Google;
using Microsoft.AspNetCore.Mvc;

namespace Leaf2Google.ViewComponents.Google;

public class SessionInfoViewComponent : BaseViewComponent
{
    private readonly LeafContext _googleContext;

    public SessionInfoViewComponent(ICarSessionManager sessionManager, LeafContext googleContext)
        : base(sessionManager)
    {
        _googleContext = googleContext;
    }

    public IViewComponentResult Invoke(string viewName, Guid? sessionId)
    {
        //RegisterViewComponentScript("/js/Components/SessionInfo.js");
        //RegisterViewComponentScript("/js/Components/CarMap.js");

        var auths = _googleContext.GoogleAuths.Where(auth =>
            auth.Owner != null && auth.Owner.CarModelId == sessionId && sessionId != null);

        var sessionInfo = new SessionInfoViewModel();

        if (auths.Any())
        {
            sessionInfo.auths = auths.ToList();

            return View(viewName, sessionInfo);
        }

        return View(viewName, sessionInfo);
    }
}