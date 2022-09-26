﻿// Copyright (c) Nathan Ford. All rights reserved. CarSelectorViewComponent.cs

using Leaf2Google.Dependency.Managers;
using Microsoft.AspNetCore.Mvc;

namespace Leaf2Google.ViewComponents.Car
{
    public class CarSelectorViewComponent : ViewComponent
    {
        private readonly LeafSessionManager _sessions;

        private readonly GoogleStateManager _google;

        protected LeafSessionManager Sessions { get => _sessions; }

        protected GoogleStateManager Google { get => _google; }

        public CarSelectorViewComponent(LeafSessionManager sessions, GoogleStateManager google)
        {
            _sessions = sessions;
            _google = google;
        }

        public IViewComponentResult Invoke(Guid? sessionId)
        {
            var session = Sessions.VehicleSessions.GetValueOrDefault(sessionId ?? Guid.Empty);

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