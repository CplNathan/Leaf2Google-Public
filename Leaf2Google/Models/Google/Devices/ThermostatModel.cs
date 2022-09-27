// Copyright (c) Nathan Ford. All rights reserved. Thermostat.cs

using Leaf2Google.Dependency.Car;
using Newtonsoft.Json.Linq;

namespace Leaf2Google.Models.Google.Devices
{
    public class ThermostatModel : BaseDeviceModel
    {
        public decimal Target { get; set; } = 21;
        public decimal LastTemperature { get; set; } = 21;
        public bool Active { get; set; }

        public ThermostatModel(string Id, string Name)
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
    }
}