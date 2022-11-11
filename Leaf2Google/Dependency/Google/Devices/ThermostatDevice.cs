// Copyright (c) Nathan Ford. All rights reserved. Lock.cs

using Leaf2Google.Models.Google.Devices;
using Leaf2Google.Models.Car.Sessions;
using Leaf2Google.Json.Google;
using System.Text.Json.Nodes;

namespace Leaf2Google.Dependency.Google.Devices;

public class ThermostatDevice : BaseDevice, IDevice
{
    public ThermostatDevice(GoogleStateManager googleState, ICarSessionManager sessionManager, BaseStorageManager storageManager)
        : base(googleState, sessionManager, storageManager)
    {
        // Use here, have as per request object not a singleton!
    }

    public async Task<bool> FetchAsync(VehicleSessionBase session, string? vin, bool forceFetch = false)
    {
        var vehicleThermostat = (ThermostatModel)(StorageManager.GoogleSessions)[session.SessionId][typeof(ThermostatDevice)];

        var success = false;

        if (vehicleThermostat.WillFetch || forceFetch)
        {
            var climateStatus = await SessionManager.VehicleClimate(session, vin,
                DateTime.UtcNow - vehicleThermostat.LastUpdated > TimeSpan.FromSeconds(10));

            if (climateStatus is not null && climateStatus.Success)
            {
                success = climateStatus.Success;
                vehicleThermostat.LastTemperature = climateStatus.Data?.data.attributes.internalTemperature ??
                                                    vehicleThermostat.LastTemperature;
                vehicleThermostat.Active = climateStatus.Data?.data.attributes.hvacStatus != "off";
                vehicleThermostat.LastUpdated = DateTime.UtcNow; //climateStatus.Data?.data.attributes.lastUpdateTime;
            }
        }
        else
        {
            success = true;
        }

        return success;
    }

    public async Task<QueryDeviceData> QueryAsync(VehicleSessionBase session, string? vin)
    {
        if (!session.Authenticated)
        {
            return new QueryDeviceData
            {
                online = false,
                status = "ERROR"

                /* Custom Syntax, also need to implement 
                {
                    "errors", new JObject
                    {
                        { "status", "FAILURE" },
                        { "errorCode", "authFailure" }
                    }
                }
                */
            };
        }

        var success = await FetchAsync(session, vin);

        var vehicleThermostat = (ThermostatModel)(StorageManager.GoogleSessions)[session.SessionId][typeof(ThermostatDevice)];

        return new ThermostatDeviceData
        {
            status = "SUCCESS",
            online = success,
            thermostatMode = vehicleThermostat.Active ? "heatcool" : "off",
            thermostatTemperatureSetpoint = vehicleThermostat.Target,
            thermostatTemperatureAmbient = vehicleThermostat.LastTemperature
        };
    }

    public async Task<ExecuteDeviceData> ExecuteAsync(VehicleSessionBase session, string? vin, JsonObject data)
    {
        if (!session.Authenticated)
        {
            return new ExecuteDeviceDataError
            {
                status = "ERROR",
                errorCode = "authFailure",
                states = new JsonObject()
                {
                    { "online", false },
                }
            };
        }

        var vehicleThermostat = (ThermostatModel)(StorageManager.GoogleSessions)[session.SessionId][typeof(ThermostatDevice)];

        vehicleThermostat.Target = data.ContainsKey("thermostatTemperatureSetpoint")
            ? (decimal)data["thermostatTemperatureSetpoint"]!
            : vehicleThermostat.Target;
        vehicleThermostat.Active = data.ContainsKey("thermostatMode") ? (string)data["thermostatMode"]! == "heatcool" :
            data.ContainsKey("thermostatTemperatureSetpoint") ? true : vehicleThermostat.Active;
        var climateStatus =
            await SessionManager.SetVehicleClimate(session, vin, vehicleThermostat.Target, vehicleThermostat.Active);

        var success = false;
        if (climateStatus is not null && climateStatus.Success) success = climateStatus.Success;

        await QueryAsync(session, vin).ConfigureAwait(false);

        return new ExecuteDeviceDataSuccess()
        {
            status = "SUCCESS",
            states = new JsonObject()
            {
                { "online", success },
                { "thermostatMode", vehicleThermostat.Active ? "heatcool" : "off" },
                { "thermostatTemperatureSetpoint", vehicleThermostat.Target },
                { "thermostatTemperatureAmbient", vehicleThermostat.LastTemperature }
            }
        };
    }
}