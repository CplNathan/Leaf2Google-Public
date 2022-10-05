// Copyright (c) Nathan Ford. All rights reserved. CarInfoViewComponent.cs

using Leaf2Google.Dependency.Google;
using Leaf2Google.Dependency.Google.Devices;
using Leaf2Google.Models.Car;
using Leaf2Google.Models.Google.Devices;
using Microsoft.AspNetCore.Mvc;

namespace Leaf2Google.ViewComponents.Car;

public class CarInfoViewComponent : BaseViewComponent
{
    private readonly IEnumerable<IDevice> _devices;

    public CarInfoViewComponent(ICarSessionManager sessionManager, GoogleStateManager google,
        IEnumerable<IDevice> devices)
        : base(sessionManager)
    {
        Google = google;
        _devices = devices;
    }

    protected GoogleStateManager Google { get; }

    public async Task<IViewComponentResult> InvokeAsync(string viewName, Guid? sessionId, string? defaultVin)
    {
        var session = SessionManager.VehicleSessions[sessionId ?? Guid.Empty];

        if (session != null && defaultVin != null)
        {
            var carThermostat = (ThermostatModel?)Google.Devices[session.SessionId][typeof(ThermostatDevice)];
            var carLock = (LockModel?)Google.Devices[session.SessionId][typeof(LockDevice)];

            var thermostatDeviceData = _devices.FirstOrDefault(x => x.GetType() == typeof(ThermostatDevice));
            if (thermostatDeviceData != null) await thermostatDeviceData.FetchAsync(session.SessionId, defaultVin);

            var lockDeviceData = _devices.FirstOrDefault(x => x.GetType() == typeof(LockDevice));
            if (lockDeviceData != null) await lockDeviceData.FetchAsync(session.SessionId, defaultVin);

            carThermostat = (ThermostatModel?)Google.Devices[session.SessionId][typeof(ThermostatDevice)];
            carLock = (LockModel?)Google.Devices[session.SessionId][typeof(LockDevice)];

            var carInfo = new CarViewModel
            {
                carThermostat = carThermostat, //new Models.Google.Devices.Thermostat("1-leaf-ac", "Air Conditioner"),
                carLock = carLock, //new Models.Google.Devices.Charger("1-leaf-lock", "Leaf"),
                carLocation = await SessionManager.VehicleLocation(session.SessionId, defaultVin),
                carPicture = SessionManager.AllSessions[session.SessionId].CarPictureUrl
            };

            return View(viewName, carInfo);
        }

        return View(viewName);
    }
}