// Copyright (c) Nathan Ford. All rights reserved. Lock.cs

using Leaf2Google.Models.Google.Devices;
using Leaf2Google.Models.Car.Sessions;
using Leaf2Google.Json.Google;
using System.Text.Json.Nodes;

namespace Leaf2Google.Dependency.Google.Devices;

public class LockDevice : BaseDevice, IDevice
{
    public LockDevice(GoogleStateManager googleState, ICarSessionManager sessionManager, BaseStorageManager storageManager)
        : base(googleState, sessionManager, storageManager)
    {
        // Use here, have as per request object not a singleton!
    }

    public async Task<bool> FetchAsync(VehicleSessionBase session, string? vin, bool forceFetch = false)
    {
        var vehicleLock = (LockModel)(StorageManager.GoogleSessions)[session.SessionId][typeof(LockDevice)];

        var success = false;

        if (vehicleLock.WillFetch || forceFetch)
        {
            var lockStatusTask = SessionManager.VehicleLock(session, vin);
            var batteryStatusTask = SessionManager.VehicleBattery(session, vin);
            var locationFetchTask = SessionManager.VehicleLocation(session, vin);

            var lockStatus = await lockStatusTask;
            var batteryStatus = await batteryStatusTask;
            var location = await locationFetchTask;

            if (lockStatus is not null && lockStatus.Success && batteryStatus is not null && batteryStatus.Success)
            {
                success = lockStatus.Success && batteryStatus.Success;
                vehicleLock.Locked = (string?)lockStatus.Data?.data.attributes.lockStatus == "locked";
                vehicleLock.CapacityRemaining = (int?)batteryStatus.Data?.data.attributes.batteryLevel ??
                                                vehicleLock.CapacityRemaining;
                vehicleLock.KillometersRemaining = (int?)batteryStatus.Data?.data.attributes.rangeHvacOff ??
                                                   vehicleLock.KillometersRemaining;
                vehicleLock.KillowatCapacity = (int?)batteryStatus.Data?.data.attributes.batteryCapacity / 1000 ??
                                               vehicleLock.KillowatCapacity;
                vehicleLock.MinutesTillFull = (int?)batteryStatus.Data?.data.attributes.timeRequiredToFullFast ??
                                              vehicleLock.MinutesTillFull;
                vehicleLock.IsCharging = (bool?)batteryStatus.Data?.data.attributes.chargeStatus ?? false;
                vehicleLock.IsPluggedIn = (bool?)batteryStatus.Data?.data.attributes.plugStatus ?? false;
                vehicleLock.LastUpdated = DateTime.UtcNow;
                vehicleLock.Location = location.IsEmpty ? null : location;
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

        var vehicleLock = (LockModel)(StorageManager.GoogleSessions)[session.SessionId][typeof(LockDevice)];

        var descriptiveCapacity = "FULL";

        if (vehicleLock.CapacityRemaining < 15)
            descriptiveCapacity = "CRITICALLY_LOW";
        else if (vehicleLock.CapacityRemaining < 40)
            descriptiveCapacity = "LOW";
        else if (vehicleLock.CapacityRemaining < 60)
            descriptiveCapacity = "MEDIUM";
        else if (vehicleLock.CapacityRemaining < 95)
            descriptiveCapacity = "HIGH";
        else
            descriptiveCapacity = "FULL";

        var currentKillowat = vehicleLock.KillowatCapacity * (vehicleLock.CapacityRemaining / 100);

        return new LockDeviceData
        {

            status = "SUCCESS",
            online = success,
            descriptiveCapacityRemaining = descriptiveCapacity,
            capacityRemaining = new List<ValueUnit>()
            {
                new ValueUnit() { rawValue = vehicleLock.CapacityRemaining, unit = "PERCENTAGE" },
                new ValueUnit() { rawValue = vehicleLock.KillometersRemaining, unit = "KILOMETERS" },
                new ValueUnit() { rawValue = (int)(vehicleLock.KillometersRemaining / 1.609), unit = "MILES" }
                //new ValueUnit() { rawValue = currentKillowat, unit = "KILOWATT_HOURS" }
            },
            capacityUntilFull = new List<ValueUnit>()
            {
                new ValueUnit() { rawValue = (vehicleLock.MinutesTillFull * 60), unit = "SECONDS" }
                //new ValueUnit() { rawValue = (vehicleLock.KillowatCapacity - currentKillowat), unit = "KILOWATT_HOURS" }
            },
            isCharging = vehicleLock.IsCharging,
            isPluggedIn = vehicleLock.IsPluggedIn,
            isLocked = vehicleLock.Locked,
            isJammed = false
        };
    }

    public async Task<ExecuteDeviceData> ExecuteAsync(VehicleSessionBase session, string? vin, JsonObject data)
    {
        var vehicleLock = (LockModel)(StorageManager.GoogleSessions)[session.SessionId][typeof(LockDevice)];

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

        if ((string?)data.Root["command"] == "action.devices.commands.Locate" &&
            ((bool?)data["silence"] ?? false) == false)
        {
            await SessionManager.FlashLights(session, vin).ConfigureAwait(false);
        }
        else if ((string?)data.Root["command"] == "action.devices.commands.LockUnlock")
        {
            vehicleLock.Locked = data.ContainsKey("lock")
                ? (bool?)data["lock"] ?? vehicleLock.Locked
                : vehicleLock.Locked;

            var lockStatus = await SessionManager.SetVehicleLock(session, vin, vehicleLock.Locked);

            var success = false;
            if (lockStatus is not null && lockStatus.Success) success = lockStatus.Success;

            return new ExecuteDeviceDataError
            {
                status = "ERROR",
                errorCode = "remoteSetDisabled",
                errorCodeReason = "remoteUnlockNotAllowed",
                states = new JsonObject()
                {
                    { "online", success },
                    { "isLocked", vehicleLock.Locked },
                    { "isJammed", true }
                }
            };
        }
        else
        {
            throw new NotImplementedException($"Command: {(string?)data.Root["command"]} is not implemented");
        }

        return null;
    }
}