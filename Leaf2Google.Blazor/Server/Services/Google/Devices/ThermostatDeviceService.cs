// Copyright (c) Nathan Ford. All rights reserved. ThermostatDeviceService.cs

using Leaf2Google.Json.Google;
using Leaf2Google.Models.Car.Sessions;
using Leaf2Google.Models.Google.Devices;
using System.Text.Json.Nodes;

namespace Leaf2Google.Services.Google.Devices;

public class ThermostatDeviceService : BaseDeviceService, IDevice
{
    public ThermostatDeviceService(ICarSessionManager sessionManager)
        : base(sessionManager)
    {
        // Use here, have as per request object not a singleton!
    }

    public Type DeviceModel { get => typeof(ThermostatDeviceData); }

    public async Task<bool> FetchAsync(VehicleSessionBase session, BaseDeviceModel deviceModel, string? vin, bool forceFetch = false)
    {
        var success = false;

        if (deviceModel.WillFetch || forceFetch)
        {
            var vehicleThermostat = (ThermostatModel)deviceModel;

            var climateStatus = await SessionManager.VehicleClimate(session, vin, DateTime.UtcNow - vehicleThermostat.LastUpdated > TimeSpan.FromSeconds(10)).ConfigureAwait(false);

            if (climateStatus is not null && climateStatus.Success)
            {
                success = climateStatus.Success;
                vehicleThermostat.LastTemperature = climateStatus.Data?["data"]?["attributes"]?["internalTemperature"]?.GetValue<decimal?>() ??
                                                    vehicleThermostat.LastTemperature;
                vehicleThermostat.Active = climateStatus.Data?["data"]?["attributes"]?["hvacStatus"]?.GetValue<string>() != "off";
                vehicleThermostat.LastUpdated = DateTime.UtcNow; //climateStatus.Data?.data.attributes.lastUpdateTime;
            }
        }
        else
        {
            success = true;
        }

        return success;
    }

    public async Task<QueryDeviceData> QueryAsync(VehicleSessionBase session, BaseDeviceModel deviceModel, string? vin)
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

        var vehicleThermostat = (ThermostatModel)deviceModel;

        var success = await FetchAsync(session, vehicleThermostat, vin).ConfigureAwait(false);

        return new ThermostatDeviceData
        {
            status = "SUCCESS",
            online = success,
            thermostatMode = vehicleThermostat.Active ? "heatcool" : "off",
            thermostatTemperatureSetpoint = vehicleThermostat.Target,
            thermostatTemperatureAmbient = vehicleThermostat.LastTemperature
        };
    }

    public async Task<ExecuteDeviceData> ExecuteAsync(VehicleSessionBase session, BaseDeviceModel deviceModel, string? vin, string command, JsonObject data)
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

        var vehicleThermostat = (ThermostatModel)deviceModel;

        vehicleThermostat.Target = data.ContainsKey("thermostatTemperatureSetpoint")
            ? (decimal)data["thermostatTemperatureSetpoint"]!
            : vehicleThermostat.Target;
        vehicleThermostat.Active = data.ContainsKey("thermostatMode") ? (string)data["thermostatMode"]! == "heatcool" :
            data.ContainsKey("thermostatTemperatureSetpoint") || vehicleThermostat.Active;
        var climateStatus =
            await SessionManager.SetVehicleClimate(session, vin, vehicleThermostat.Target, vehicleThermostat.Active).ConfigureAwait(false);

        var success = false;
        if (climateStatus is not null && climateStatus.Success)
        {
            success = climateStatus.Success;
        }

        await QueryAsync(session, deviceModel, vin).ConfigureAwait(false);

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