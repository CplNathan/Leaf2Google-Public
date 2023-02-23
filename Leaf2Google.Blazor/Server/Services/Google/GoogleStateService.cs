// Copyright (c) Nathan Ford. All rights reserved. GoogleStateService.cs

using Leaf2Google.Models.Car.Sessions;
using Leaf2Google.Models.Google.Devices;
using Leaf2Google.Services.Google.Devices;

namespace Leaf2Google.Services.Google;

public sealed class GoogleStateService
{
    public GoogleStateService(BaseStorageService storageManager)
    {
        StorageManager = storageManager;
        
        foreach (var session in storageManager.VehicleSessions.Where(session => session.Value.Authenticated))
        {
            GetOrCreateDevices(session.Key);
        }
    }

    private BaseStorageService StorageManager { get; }

    private static Dictionary<Type, BaseDeviceModel> MakeDevices()
    {
        var devices = new Dictionary<Type, BaseDeviceModel>
        {
            { typeof(ThermostatDeviceService), new ThermostatModel("1-leaf-ac", "Leaf") },
            { typeof(LockDeviceService), new LockModel("1-leaf-lock", "Leaf") }
        };

        return devices;
    }

    public Dictionary<Type, BaseDeviceModel> GetOrCreateDevices(Guid sessionId)
    {
        Dictionary<Type, BaseDeviceModel> googleSession;
        var googleSessionFound = StorageManager.GoogleSessions.TryGetValue(sessionId, out googleSession!);

        if (googleSessionFound)
        {
            return googleSession;
        }
        else
        {
            return StorageManager.GoogleSessions[sessionId] = MakeDevices();
        }
    }
}
