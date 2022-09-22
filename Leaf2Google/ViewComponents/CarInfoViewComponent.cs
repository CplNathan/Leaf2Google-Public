// Copyright (c) Nathan Ford. All rights reserved. CarInfoViewComponent.cs

using Castle.Core.Internal;
using Leaf2Google.Dependency.Managers;
using Leaf2Google.Models.Google.Devices;
using Leaf2Google.Models.Car;
using Microsoft.AspNetCore.Mvc;
using Lock = Leaf2Google.Models.Google.Devices.Lock;

namespace Leaf2Google.ViewComponents
{
    public class CarInfoViewComponent : ViewComponent
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

        public async Task<IViewComponentResult> InvokeAsync(Guid? sessionId, string? defaultVin)
        {
            var session = Sessions.VehicleSessions.FirstOrDefault(session => session.SessionId == sessionId && sessionId != null);

            if (session != null && defaultVin != null)
            {
                Thermostat? thermostat = (Thermostat?)Google.Devices[session.SessionId].FirstOrDefault(device => device is Thermostat);
                Lock? carlock = (Lock?)Google.Devices[session.SessionId].FirstOrDefault(device => device is Lock);

                CarInfoModel carInfo = new CarInfoModel()
                {
                    thermostat = thermostat, //new Models.Google.Devices.Thermostat("1-leaf-ac", "Air Conditioner"),
                    carlock = carlock //new Models.Google.Devices.Charger("1-leaf-lock", "Leaf")
                };

                if (carInfo?.thermostat != null)
                    await carInfo.thermostat.QueryAsync(Sessions, session, defaultVin);

                if (carInfo?.carlock != null)
                    await carInfo.carlock.QueryAsync(Sessions, session, defaultVin);

                return View(carInfo);
            }
            else
            {
                return View(null);
            }
        }
    }
}