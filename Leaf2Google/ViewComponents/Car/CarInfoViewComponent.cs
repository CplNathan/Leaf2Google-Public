// Copyright (c) Nathan Ford. All rights reserved. CarInfoViewComponent.cs

using Leaf2Google.Dependency;
using Leaf2Google.Dependency.Car;
using Leaf2Google.Dependency.Google;
using Leaf2Google.Dependency.Google.Devices;
using Leaf2Google.Models.Car;
using Leaf2Google.Models.Google.Devices;
using Microsoft.AspNetCore.Mvc;
using LockModel = Leaf2Google.Models.Google.Devices.LockModel;

namespace Leaf2Google.ViewComponents.Car
{
    public class CarInfoViewComponent : BaseViewComponent
    {
        private readonly GoogleStateManager _google;

        protected GoogleStateManager Google { get => _google; }

        private readonly IEnumerable<IDevice> _devices;

        public CarInfoViewComponent(ICarSessionManager sessionManager, GoogleStateManager google, IEnumerable<IDevice> devices)
            : base(sessionManager)
        {
            _google = google;
            _devices = devices;
        }

        public async Task<IViewComponentResult> InvokeAsync(string viewName, Guid? sessionId, string? defaultVin)
        {
            var session = SessionManager.VehicleSessions[sessionId ?? Guid.Empty];

            if (session != null && defaultVin != null)
            {
                ThermostatModel? carThermostat = (ThermostatModel?)Google.Devices[session.SessionId][typeof(ThermostatDevice)];
                LockModel? carLock = (LockModel?)Google.Devices[session.SessionId][typeof(LockDevice)];

                var thermostatDeviceData = _devices.FirstOrDefault(x => x.GetType() == typeof(ThermostatDevice));
                if (thermostatDeviceData != null)
                {
                    await thermostatDeviceData.FetchAsync(session.SessionId, defaultVin);
                }

                var lockDeviceData = _devices.FirstOrDefault(x => x.GetType() == typeof(LockDevice));
                if (lockDeviceData != null)
                {
                    await lockDeviceData.FetchAsync(session.SessionId, defaultVin);
                }

                carThermostat = (ThermostatModel?)Google.Devices[session.SessionId][typeof(ThermostatDevice)];
                carLock = (LockModel?)Google.Devices[session.SessionId][typeof(LockDevice)];

                CarViewModel carInfo = new CarViewModel()
                {
                    carThermostat = carThermostat, //new Models.Google.Devices.Thermostat("1-leaf-ac", "Air Conditioner"),
                    carLock = carLock, //new Models.Google.Devices.Charger("1-leaf-lock", "Leaf"),
                    carLocation = await SessionManager.VehicleLocation(session.SessionId, defaultVin),
                    carPicture = SessionManager.AllSessions[session.SessionId].CarPictureUrl
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