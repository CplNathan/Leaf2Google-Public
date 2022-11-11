// Copyright (c) Nathan Ford. All rights reserved. Thermostat.cs

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;

namespace Leaf2Google.Models.Google.Devices
{

    public class ThermostatModel : BaseDeviceModel
    {
        public ThermostatModel(string Id, string Name)
            : base(Id, Name)
        {
            type = "THERMOSTAT";
            traits = new List<string> { "TemperatureSetting" };
            WillReportState = true;
            Attributes = new JsonObject()
        {
            {
                "availableThermostatModes", JsonValue.Create(new List<string>()
                {
                    "off",
                    "heatcool"
                })
            },
            {
                "thermostatTemperatureRange", JsonValue.Create(new Dictionary<string, int>()
                {
                    { "minThresholdCelsius", 16 },
                    { "maxThresholdCelsius", 26 }
                })
            },
            { "thermostatTemperatureUnit", "C" },
            { "bufferRangeCelsius", 0 }
        };

            SupportedCommands = new List<string> { "ThermostatTemperatureSetpoint", "ThermostatSetMode" };
        }

        public decimal Target { get; set; } = 21;
        public decimal LastTemperature { get; set; } = 21;
        public bool Active { get; set; }
    }
}