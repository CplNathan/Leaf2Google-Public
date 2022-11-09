// Copyright (c) Nathan Ford. All rights reserved. CarInfoViewComponent.cs

using Leaf2Google.Dependency.Google;
using Leaf2Google.Dependency.Google.Devices;
using Leaf2Google.Models.Car;
using Leaf2Google.Models.Google.Devices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Leaf2Google.ViewComponents.Car;

public class CarInfoViewComponent : BaseViewComponent
{
    protected IEnumerable<IDevice> Devices { get; }

    public CarInfoViewComponent(BaseStorageManager storageManager, GoogleStateManager google,
        IEnumerable<IDevice> devices)
        : base(storageManager)
    {
        Google = google;
        Devices = devices;
    }

    protected GoogleStateManager Google { get; }

    public async Task<IViewComponentResult> InvokeAsync(string viewName, Guid? sessionId, string? defaultVin)
    {
        var session = StorageManager.VehicleSessions.GetValueOrDefault(sessionId ?? Guid.Empty);

        if (session != null && defaultVin != null)
        {
            var thermostatDeviceData = Devices.FirstOrDefault(x => x.GetType() == typeof(ThermostatDevice));
            if (thermostatDeviceData != null) await thermostatDeviceData.FetchAsync(session, defaultVin);

            var lockDeviceData = Devices.FirstOrDefault(x => x.GetType() == typeof(LockDevice));
            if (lockDeviceData != null) await lockDeviceData.FetchAsync(session, defaultVin);

            var carThermostat = (ThermostatModel?)(StorageManager.GoogleSessions)[session.SessionId][typeof(ThermostatDevice)];
            var carLock = (LockModel?)(StorageManager.GoogleSessions)[session.SessionId][typeof(LockDevice)];

            var carInfo = new CarViewModel
            {
                carThermostat = carThermostat, //new Models.Google.Devices.Thermostat("1-leaf-ac", "Air Conditioner"),
                carLock = carLock, //new Models.Google.Devices.Charger("1-leaf-lock", "Leaf"),
                carLocation = carLock?.Location,
                carPicture = session.CarPictureUrl
            };

            return View(viewName, carInfo);
        }

        return View(viewName);
    }
}