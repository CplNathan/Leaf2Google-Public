// Copyright (c) Nathan Ford. All rights reserved. SessionInfoViewModel.cs

using Leaf2Google.Dependency.Managers;
using Leaf2Google.Models.Google.Devices;
using Leaf2Google.Models.Google;
using Microsoft.AspNetCore.Mvc;
using Leaf2Google.Contexts;
using Leaf2Google.Controllers;

namespace Leaf2Google.ViewComponents
{
    public class SessionInfoViewComponent : ViewComponent
    {
        private readonly LeafContext _googleContext;

        public SessionInfoViewComponent(LeafContext googleContext)
        {
            _googleContext = googleContext;
        }

        public IViewComponentResult Invoke(Guid? sessionId)
        {
            var auths = _googleContext.GoogleAuths.Where(auth => auth.Owner != null && auth.Owner.CarModelId == sessionId && sessionId != null);

            if (auths.Any())
            {
                SessionInfo sessionInfo = new SessionInfo()
                {
                    auths = auths.ToList()
                };

                return View(sessionInfo);
            }
            else
            {
                return View(null);
            }
        }
    }
}