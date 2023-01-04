// Copyright (c) Nathan Ford. All rights reserved. GoogleStateService.cs

using Leaf2Google.Models.Car.Sessions;
using Leaf2Google.Models.Google.Devices;
using Leaf2Google.Services.Google.Devices;

namespace Leaf2Google.Services.Google;

public sealed class GoogleStateService : IDisposable
{
    public GoogleStateService(BaseStorageService storageManager,
        ICarSessionManager sessionManager)
    {
        StorageManager = storageManager;

        if (storageManager != null)
        {
            if (sessionManager is BaseSessionService baseSessionManager)
            {
                if (storageManager.GoogleSessions.Count <= 0)
                {
                    foreach (var session in storageManager.VehicleSessions.Where(session => session.Value.Authenticated))
                    {
                        GetOrCreateDevices(session.Key);
                    }
                }

                BaseSessionService.OnAuthenticationAttempt += BaseSessionManager_OnAuthenticationAttempt;
            }
        }
    }

    public void Dispose()
    {
        BaseSessionService.OnAuthenticationAttempt -= BaseSessionManager_OnAuthenticationAttempt;
    }

    private BaseStorageService StorageManager { get; }

    private void BaseSessionManager_OnAuthenticationAttempt(object sender, string? authToken)
    {
        if (sender is VehicleSessionBase session)
        {
            GetOrCreateDevices(session.SessionId);
        }
    }

    private static Dictionary<Type, BaseDeviceModel> MakeDevices()
    {
        var devices = new Dictionary<Type, BaseDeviceModel>
        {
            { typeof(ThermostatDeviceService), new ThermostatModel("1-leaf-ac", "Air Conditioner") },
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