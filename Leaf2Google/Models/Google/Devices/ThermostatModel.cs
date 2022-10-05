// Copyright (c) Nathan Ford. All rights reserved. Thermostat.cs

using Newtonsoft.Json.Linq;

namespace Leaf2Google.Models.Google.Devices;

public class ThermostatModel : BaseDeviceModel
{
    public ThermostatModel(string Id, string Name)
        : base(Id, Name)
    {
        type = "THERMOSTAT";
        traits = new List<string> { "TemperatureSetting" };
        WillReportState = true;
        Attributes = new JObject
        {
            {
                "availableThermostatModes", new JArray
                {
                    "off",
                    "heatcool"
                }
            },
            {
                "thermostatTemperatureRange", new JObject
                {
                    { "minThresholdCelsius", 16 },
                    { "maxThresholdCelsius", 26 }
                }
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