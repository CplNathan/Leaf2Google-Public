// Copyright (c) Nathan Ford. All rights reserved. Lock.cs

using Leaf2Google.Dependency.Car;
using Leaf2Google.Models.Google.Devices;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Sockets;

namespace Leaf2Google.Dependency.Google.Devices
{
    public class ThermostatDevice : BaseDevice, IDevice
    {
        public ThermostatDevice(GoogleStateManager googleState, LeafSessionManager sessionManager)
            : base(googleState, sessionManager)
        {
            // Use here, have as per request object not a singleton!
        }

        public async Task<bool> FetchAsync(Guid sessionId, string? vin, bool forceFetch = false)
        {
            ThermostatModel vehicleThermostat = (ThermostatModel)_googleState.Devices[sessionId][typeof(ThermostatDevice)];

            bool success = false;

            if (vehicleThermostat.WillFetch || forceFetch)
            {
                var climateStatus = await _sessionManager.VehicleClimate(sessionId, vin, DateTime.UtcNow - vehicleThermostat.LastUpdated > TimeSpan.FromSeconds(10));

                if (climateStatus is not null && climateStatus.Success == true)
                {
                    success = climateStatus.Success;
                    vehicleThermostat.LastTemperature = climateStatus.Data?.data.attributes.internalTemperature ?? vehicleThermostat.LastTemperature;
                    vehicleThermostat.Active = climateStatus.Data?.data.attributes.hvacStatus != "off";
                    vehicleThermostat.LastUpdated = DateTime.UtcNow; //climateStatus.Data?.data.attributes.lastUpdateTime;
                }

                _googleState.Devices[sessionId][typeof(ThermostatDevice)] = vehicleThermostat;
            }
            else
            {
                success = true;
            }

            return success;
        }

        public async Task<JObject> QueryAsync(Guid sessionId, string? vin)
        {
            bool success = await FetchAsync(sessionId, vin);

            ThermostatModel vehicleThermostat = (ThermostatModel)_googleState.Devices[sessionId][typeof(ThermostatDevice)];

            return new JObject()
            {
                { "status", "SUCCESS" },
                { "online", success },
                { "thermostatMode", vehicleThermostat.Active ? "heatcool" : "off" },
                { "thermostatTemperatureSetpoint", vehicleThermostat.Target },
                { "thermostatTemperatureAmbient", vehicleThermostat.LastTemperature }
            };
        }

        public async Task<JObject> ExecuteAsync(Guid sessionId, string? vin, JObject data)
        {
            ThermostatModel vehicleThermostat = (ThermostatModel)_googleState.Devices[sessionId][typeof(ThermostatDevice)];

            vehicleThermostat.Target = data.ContainsKey("thermostatTemperatureSetpoint") ? (decimal)data["thermostatTemperatureSetpoint"]! : vehicleThermostat.Target;
            vehicleThermostat.Active = data.ContainsKey("thermostatMode") ? ((string)data["thermostatMode"]! == "heatcool") : data.ContainsKey("thermostatTemperatureSetpoint") ? true : vehicleThermostat.Active;
            var climateStatus = await _sessionManager.SetVehicleClimate(sessionId, vin, vehicleThermostat.Target, vehicleThermostat.Active);

            bool success = false;
            if (climateStatus is not null && climateStatus.Success == true)
            {
                success = climateStatus.Success;
            }

            await QueryAsync(sessionId, vin);

            return new JObject()
            {
                { "online", success },
                { "thermostatMode", vehicleThermostat.Active ? "heatcool" : "off" },
                { "thermostatTemperatureSetpoint", vehicleThermostat.Target },
                { "thermostatTemperatureAmbient", vehicleThermostat.LastTemperature }
            };
        }
    }
}