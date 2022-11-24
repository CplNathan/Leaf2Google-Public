using Leaf2Google.Services.Google.Devices;
using Leaf2Google.Models.Google.Devices;
using Leaf2Google.Models.Car.Sessions;

namespace Leaf2Google.Services.Google;

public class GoogleStateService
{
    public GoogleStateService(BaseStorageService storageManager,
        ICarSessionManager sessionManager)
    {
        StorageManager = storageManager;

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

    protected BaseStorageService StorageManager { get; }

    private void BaseSessionManager_OnAuthenticationAttempt(object sender, string? authToken)
    {
        if (sender is VehicleSessionBase session) GetOrCreateDevices(session.SessionId);
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
        if (StorageManager.GoogleSessions.ContainsKey(sessionId))
            return StorageManager.GoogleSessions[sessionId];

        return StorageManager.GoogleSessions[sessionId] = MakeDevices();
    }
}