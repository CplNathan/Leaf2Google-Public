// Copyright (c) Nathan Ford. All rights reserved. SessionInfoViewModel.cs

using Leaf2Google.Contexts;
using Leaf2Google.Models.Google;
using Microsoft.AspNetCore.Mvc;

namespace Leaf2Google.ViewComponents
{
    public class SessionInfoViewComponent : BaseViewComponent
    {
        private readonly LeafContext _googleContext;

        public SessionInfoViewComponent(LeafContext googleContext)
        {
            _googleContext = googleContext;
        }

        public IViewComponentResult Invoke(string viewName, Guid? sessionId)
        {
            //RegisterViewComponentScript("/js/Components/SessionInfo.js");
            //RegisterViewComponentScript("/js/Components/CarMap.js");

            var auths = _googleContext.GoogleAuths.Where(auth => auth.Owner != null && auth.Owner.CarModelId == sessionId && sessionId != null);

            if (auths.Any())
            {
                SessionInfo sessionInfo = new SessionInfo()
                {
                    auths = auths.ToList()
                };

                return View(viewName, sessionInfo);
            }
            else
            {
                return View(null);
            }
        }
    }
}