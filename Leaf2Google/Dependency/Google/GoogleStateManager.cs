using Leaf2Google.Dependency.Google.Devices;
using Leaf2Google.Models.Car;
using Leaf2Google.Models.Google.Devices;
using NUglify.Helpers;

namespace Leaf2Google.Dependency.Google;

public class GoogleStateManager
{
    public GoogleStateManager(Dictionary<Guid, Dictionary<Type, BaseDeviceModel>> devices,
        ICarSessionManager sessionManager)
    {
        Devices = devices;

        if (sessionManager is BaseSessionManager baseSessionManager)
        {
            baseSessionManager.OnAuthenticationAttempt += BaseSessionManager_OnAuthenticationAttempt;

            if (devices.Count <= 0)
                baseSessionManager.VehicleSessions.Where(session => session.Value.Authenticated)
                    .ForEach(session => GetOrCreateDevices(session.Key));
        }
    }

    public Dictionary<Guid, Dictionary<Type, BaseDeviceModel>> Devices { get; }

    private void BaseSessionManager_OnAuthenticationAttempt(object sender, Guid sessionId, string? authToken)
    {
        if (sender is VehicleSessionBase session) _ = GetOrCreateDevices(sessionId);
    }

    private static Dictionary<Type, BaseDeviceModel> MakeDevices()
    {
        var devices = new Dictionary<Type, BaseDeviceModel>();

        devices.Add(typeof(ThermostatDevice), new ThermostatModel("1-leaf-ac", "Air Conditioner"));
        devices.Add(typeof(LockDevice), new LockModel("1-leaf-lock", "Leaf"));

        return devices;
    }

    public Dictionary<Type, BaseDeviceModel> GetOrCreateDevices(Guid sessionId)
    {
        if (Devices.ContainsKey(sessionId))
            return Devices[sessionId];
        return Devices[sessionId] = MakeDevices();
    }
}