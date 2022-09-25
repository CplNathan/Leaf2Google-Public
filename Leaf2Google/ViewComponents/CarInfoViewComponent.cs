// Copyright (c) Nathan Ford. All rights reserved. CarInfoViewComponent.cs

using Leaf2Google.Dependency.Managers;
using Leaf2Google.Models.Car;
using Leaf2Google.Models.Google.Devices;
using Microsoft.AspNetCore.Mvc;
using Lock = Leaf2Google.Models.Google.Devices.Lock;

namespace Leaf2Google.ViewComponents
{
    public class CarInfoViewComponent : BaseViewComponent
    {
        private readonly LeafSessionManager _sessions;

        private readonly GoogleStateManager _google;

        protected LeafSessionManager Sessions { get => _sessions; }

        protected GoogleStateManager Google { get => _google; }

        public CarInfoViewComponent(LeafSessionManager sessions, GoogleStateManager google)
        {
            _sessions = sessions;
            _google = google;
        }

        public async Task<IViewComponentResult> InvokeAsync(string viewName, Guid? sessionId, string? defaultVin)
        {
            var session = Sessions.VehicleSessions.FirstOrDefault(session => session.SessionId == sessionId && sessionId != null);

            if (session != null && defaultVin != null)
            {
                Thermostat? thermostat = (Thermostat?)Google.Devices[session.SessionId].FirstOrDefault(device => device is Thermostat);
                Lock? carlock = (Lock?)Google.Devices[session.SessionId].FirstOrDefault(device => device is Lock);

                if (thermostat != null)
                {
                    await thermostat.Fetch(Sessions, session, defaultVin);
                }

                if (carlock != null)
                {
                    await carlock.Fetch(Sessions, session, defaultVin);
                }

                CarInfoModel carInfo = new CarInfoModel()
                {
                    thermostat = thermostat, //new Models.Google.Devices.Thermostat("1-leaf-ac", "Air Conditioner"),
                    carlock = carlock, //new Models.Google.Devices.Charger("1-leaf-lock", "Leaf"),
                    location = await Sessions.VehicleLocation(session.SessionId, defaultVin)
                };

                return View(viewName, carInfo);
            }
            else
            {
                return View(viewName);
            }
        }
    }
}