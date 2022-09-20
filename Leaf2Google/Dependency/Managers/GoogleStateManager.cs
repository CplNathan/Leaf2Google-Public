using Leaf2Google.Models.Google.Devices;

namespace Leaf2Google.Dependency.Managers
{
    public class GoogleStateManager
    {
        public Dictionary<Guid, List<BaseDevice>> Devices { get; set; } = new Dictionary<Guid, List<BaseDevice>>();

        public GoogleStateManager()
        {
        }

        private static List<BaseDevice> MakeDevices()
        {
            var devices = new List<BaseDevice>();

            devices.Add(new Thermostat("1-leaf-ac", "Air Conditioner"));
            devices.Add(new Lock("1-leaf-lock", "Leaf"));
            //devices.Add(new Charger("3", "Leaf"));
            //devices.Add(new TemperatureControl("3", "Air Conditioner"));

            return devices;
        }

        public List<BaseDevice> GetOrCreateDevices(Guid sessionId)
        {
            if (Devices.ContainsKey(sessionId))
                return Devices[sessionId];
            else
                return Devices[sessionId] = MakeDevices();
        }
    }
}