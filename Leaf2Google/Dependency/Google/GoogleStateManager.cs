using Leaf2Google.Dependency.Google.Devices;
using Leaf2Google.Models.Car;
using Leaf2Google.Models.Google.Devices;
using NUglify.Helpers;

namespace Leaf2Google.Dependency.Google;

public class GoogleStateManager
{
    public GoogleStateManager(BaseStorageManager storageManager,
        ICarSessionManager sessionManager)
    {
        StorageManager = storageManager;

        if (sessionManager is BaseSessionManager baseSessionManager)
        {
            BaseSessionManager.OnAuthenticationAttempt += BaseSessionManager_OnAuthenticationAttempt;

            if (storageManager.GoogleSessions.Count <= 0)
                storageManager.VehicleSessions.Where(session => session.Value.Authenticated)
                    .ForEach(session => GetOrCreateDevices(session.Key));
        }
    }

    protected BaseStorageManager StorageManager { get; }

    private void BaseSessionManager_OnAuthenticationAttempt(object sender, string? authToken)
    {
        if (sender is VehicleSessionBase session) GetOrCreateDevices(session.SessionId);
    }

    private static Dictionary<Type, BaseDeviceModel> MakeDevices()
    {
        var devices = new Dictionary<Type, BaseDeviceModel>
        {
            { typeof(ThermostatDevice), new ThermostatModel("1-leaf-ac", "Air Conditioner") },
            { typeof(LockDevice), new LockModel("1-leaf-lock", "Leaf") }
        };

        return devices;
    }

    public Dictionary<Type, BaseDeviceModel> GetOrCreateDevices(Guid sessionId)
    {
        if (StorageManager.GoogleSessions.ContainsKey(sessionId))
            return StorageManager.GoogleSessions[sessionId];

        StorageManager.GoogleSessions.TryAdd(sessionId, MakeDevices());
        return StorageManager.GoogleSessions[sessionId];
    }
}