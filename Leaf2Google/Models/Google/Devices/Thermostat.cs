// Copyright (c) Nathan Ford. All rights reserved. Thermostat.cs

using Leaf2Google.Models.Nissan;
using Newtonsoft.Json.Linq;

namespace Leaf2Google.Models.Google.Devices
{
    public class Thermostat : BaseDevice
    {
        public decimal Target { get; set; } = 21;
        public decimal LastTemperature { get; set; } = 21;
        public DateTime LastUpdated { get; set; } = DateTime.MinValue;
        public bool Active { get; set; }

        public Thermostat(string Id, string Name)
            : base(Id, Name)
        {
            this.type = "THERMOSTAT";
            this.traits = new List<string>() { "TemperatureSetting" };
            this.WillReportState = true;
            this.Attributes = new JObject()
            {
                { "availableThermostatModes", new JArray() {
                    "off",
                    "heatcool"
                }},
                { "thermostatTemperatureRange", new JObject()
                {
                    { "minThresholdCelsius", 16 },
                    { "maxThresholdCelsius", 26 }
                }},
                { "thermostatTemperatureUnit", "C" },
                { "bufferRangeCelsius", 0 }
            };

            this.SupportedCommands = new List<string>() { "ThermostatTemperatureSetpoint", "ThermostatSetMode" };
        }

        public override async Task<JObject> QueryAsync(NissanConnectSession session, string vin)
        {
            var climateStatus = await session.VehicleClimate(vin, DateTime.UtcNow - LastUpdated > TimeSpan.FromSeconds(10));

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
                { "thermostatMode", Active ? "heatcool" : "off" },
                { "thermostatTemperatureSetpoint", Target },
                { "thermostatTemperatureAmbient", LastTemperature }
            };
        }

        public override async Task<JObject> ExecuteAsync(NissanConnectSession session, string vin, JObject data)
        {
            Target = data.ContainsKey("thermostatTemperatureSetpoint") ? (decimal)data["thermostatTemperatureSetpoint"]! : Target;
            Active = data.ContainsKey("thermostatMode") ? ((string)data["thermostatMode"]! == "heatcool") : data.ContainsKey("thermostatTemperatureSetpoint") ? true : Active;

            var climateStatus = await session.SetVehicleClimate(vin, Target, Active);

            bool success = false;
            if (climateStatus is not null && climateStatus.Success == true)
            {
                success = climateStatus.Success;
            }

            await QueryAsync(session, vin);

            return new JObject()
            {
                { "online", success },
                { "thermostatMode", Active ? "heatcool" : "off" },
                { "thermostatTemperatureSetpoint", Target },
                { "thermostatTemperatureAmbient", LastTemperature }
            };
        }
    }
}