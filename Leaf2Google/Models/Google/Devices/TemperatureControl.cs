// Copyright (c) Nathan Ford. All rights reserved. Thermostat.cs

using Leaf2Google.Dependency.Managers;
using Leaf2Google.Models.Car;
using Newtonsoft.Json.Linq;

namespace Leaf2Google.Models.Google.Devices
{
    public class TemperatureControl : BaseDevice
    {
        public decimal Target { get; set; } = 21;
        public decimal LastTemperature { get; set; } = 21;
        public bool Active { get; set; }

        public TemperatureControl(string Id, string Name)
            : base(Id, Name)
        {
            this.type = "THERMOSTAT";
            this.traits = new List<string>() { "TemperatureControl", "OnOff" };
            this.WillReportState = true;
            this.Attributes = new JObject()
            {
                { "temperatureRange", new JObject()
                {
                    { "minThresholdCelsius", 16 },
                    { "maxThresholdCelsius", 26 }
                }},
                { "temperatureStepCelsius", 0.5 },
                { "temperatureUnitForUX", "C" }
            };

            this.SupportedCommands = new List<string>() { "SetTemperature", "OnOff" };
        }

        public override async Task<JObject> QueryAsync(LeafSessionManager sessionManager, VehicleSessionBase session, string? vin)
        {
            var climateStatus = await sessionManager.VehicleClimate(session.SessionId, vin, DateTime.UtcNow - LastUpdated > TimeSpan.FromSeconds(10));
            LastUpdated = DateTime.UtcNow;

            bool success = false;
            if (climateStatus is not null && climateStatus.Success == true)
            {
                success = climateStatus.Success;
                LastTemperature = climateStatus.Data?.data.attributes.internalTemperature ?? LastTemperature;
                Active = climateStatus.Data?.data.attributes.hvacStatus != "off";
                LastUpdated = climateStatus.Data?.data.attributes.lastUpdateTime;
            }

            return new JObject()
            {
                { "status", "SUCCESS" },
                { "online", success },
                { "on", Active },
                { "temperatureSetpointCelsius", Target },
                { "temperatureAmbientCelsius", LastTemperature }
            };
        }

        public override async Task<bool> Fetch(LeafSessionManager sessionManager, VehicleSessionBase session, string? vin, bool forceFetch = false)
        {
            bool success = false;

            if (WillFetch || forceFetch)
            {
                var climateStatus = await sessionManager.SetVehicleClimate(session.SessionId, vin, Target, Active);

                if (climateStatus is not null && climateStatus.Success == true)
                {
                    success = climateStatus.Success;
                }
            }
            else
            {
                success = true;
            }

            await QueryAsync(sessionManager, session, vin);

            return success;
        }

        public override async Task<JObject> ExecuteAsync(LeafSessionManager sessionManager, VehicleSessionBase session, string? vin, JObject data)
        {
            Target = data.ContainsKey("temperature") ? (decimal?)data["temperature"] ?? Target : Target;
            Active = data.ContainsKey("on") ? (bool?)data["on"] ?? Active : Active;

            bool success = await Fetch(sessionManager, session, vin);

            if ((string?)data.Root["command"] == "action.devices.commands.OnOff")
            {
                return new JObject()
                {
                    { "online", success },
                    { "on", Active }
                };
            }
            else
            {
                return new JObject()
                {
                    { "online", success },
                    { "temperatureSetpointCelsius", Target },
                    { "temperatureAmbientCelsius", LastTemperature }
                };
            }
        }
    }
}