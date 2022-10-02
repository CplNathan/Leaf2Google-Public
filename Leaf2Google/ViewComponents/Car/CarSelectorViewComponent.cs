// Copyright (c) Nathan Ford. All rights reserved. CarSelectorViewComponent.cs

using Leaf2Google.Dependency;
using Leaf2Google.Dependency.Car;
using Leaf2Google.Dependency.Google;
using Microsoft.AspNetCore.Mvc;

namespace Leaf2Google.ViewComponents.Car
{
    public class CarSelectorViewComponent : BaseViewComponent
    {
        public CarSelectorViewComponent(ICarSessionManager sessionManager)
            : base (sessionManager)
        {
        }

        public IViewComponentResult Invoke(Guid? sessionId)
        {
            var session = SessionManager.VehicleSessions.GetValueOrDefault(sessionId ?? Guid.Empty);

            if (session != null)
            {
                return View(session.VINs);
            }
            else
            {
                return View(new List<string?>());
            }
        }
    }
}