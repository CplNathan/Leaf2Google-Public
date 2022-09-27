using Leaf2Google.Dependency.Google.Devices;
using Leaf2Google.Models.Google.Devices;

namespace Leaf2Google.Dependency.Google
{
    public class GoogleStateManager
    {
        public Dictionary<Guid, Dictionary<Type, BaseDeviceModel>> Devices { get; set; } = new Dictionary<Guid, Dictionary<Type, BaseDeviceModel>>();

        public GoogleStateManager()
        {
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
            else
                return Devices[sessionId] = MakeDevices();
        }
    }
}