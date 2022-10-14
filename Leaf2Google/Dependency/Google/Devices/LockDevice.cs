// Copyright (c) Nathan Ford. All rights reserved. Lock.cs

using Leaf2Google.Models.Google.Devices;
using Newtonsoft.Json.Linq;

namespace Leaf2Google.Dependency.Google.Devices;

public class LockDevice : BaseDevice, IDevice
{
    public LockDevice(GoogleStateManager googleState, ICarSessionManager sessionManager)
        : base(googleState, sessionManager)
    {
        // Use here, have as per request object not a singleton!
    }

    public async Task<bool> FetchAsync(Guid sessionId, string? vin, bool forceFetch = false)
    {
        var vehicleLock = (LockModel)GoogleState.Devices[sessionId][typeof(LockDevice)];

        var success = false;

        if (vehicleLock.WillFetch || forceFetch)
        {
            var lockStatus = await SessionManager.VehicleLock(sessionId, vin);

            var batteryStatus = await SessionManager.VehicleBattery(sessionId, vin);

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
            }

            GoogleState.Devices[sessionId][typeof(LockDevice)] = vehicleLock;
        }
        else
        {
            success = true;
        }

        return success;
    }

    public async Task<JObject> QueryAsync(Guid sessionId, string? vin)
    {
        if (!SessionManager.AllSessions[sessionId].Authenticated)
        {
            return new JObject
            {
                { "online", false },

                /* Custom Syntax, also need to implement */
                {
                    "errors", new JObject
                    {
                        { "status", "FAILURE" },
                        { "errorCode", "authFailure" }
                    }
                }
            };
        }

        var success = await FetchAsync(sessionId, vin);

        var vehicleLock = (LockModel)GoogleState.Devices[sessionId][typeof(LockDevice)];

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

        return new JObject
        {
            { "status", "SUCCESS" },
            { "online", success },
            { "descriptiveCapacityRemaining", descriptiveCapacity },
            {
                "capacityRemaining", new JArray
                {
                    new JObject
                    {
                        { "rawValue", vehicleLock.CapacityRemaining },
                        { "unit", "PERCENTAGE" }
                    },
                    new JObject
                    {
                        { "rawValue", vehicleLock.KillometersRemaining },
                        { "unit", "KILOMETERS" }
                    },
                    new JObject
                    {
                        { "rawValue", vehicleLock.KillometersRemaining / 1.609 },
                        { "unit", "MILES" }
                    },
                    new JObject
                    {
                        { "rawValue", currentKillowat },
                        { "unit", "KILOWATT_HOURS" }
                    }
                }
            },
            {
                "capacityUntilFull", new JArray
                {
                    new JObject
                    {
                        { "rawValue", vehicleLock.MinutesTillFull * 60 },
                        { "unit", "SECONDS" }
                    },
                    new JObject
                    {
                        { "rawValue", vehicleLock.KillowatCapacity - currentKillowat },
                        { "unit", "KILOWATT_HOURS" }
                    }
                }
            },
            { "isCharging", vehicleLock.IsCharging },
            { "isPluggedIn", vehicleLock.IsPluggedIn },
            { "isLocked", vehicleLock.Locked },
            { "isJammed", false }
        };
    }

    public async Task<JObject> ExecuteAsync(Guid sessionId, string? vin, JObject data)
    {
        var vehicleLock = (LockModel)GoogleState.Devices[sessionId][typeof(LockDevice)];

        if (!SessionManager.AllSessions[sessionId].Authenticated)
        {
            return new JObject
            {
                { "online", false },

                /* Custom Syntax, also need to implement */
                {
                    "errors", new JObject
                    {
                        { "status", "FAILURE" },
                        { "errorCode", "authFailure" }
                    }
                }
            };
        }

        if ((string?)data.Root["command"] == "action.devices.commands.Locate" &&
            ((bool?)data["silence"] ?? false) == false)
        {
            var flashStatus = await SessionManager.FlashLights(sessionId, vin);
        }
        else if ((string?)data.Root["command"] == "action.devices.commands.LockUnlock")
        {
            vehicleLock.Locked = data.ContainsKey("lock")
                ? (bool?)data["lock"] ?? vehicleLock.Locked
                : vehicleLock.Locked;

            var lockStatus = await SessionManager.SetVehicleLock(sessionId, vin, vehicleLock.Locked);

            var success = false;
            if (lockStatus is not null && lockStatus.Success) success = lockStatus.Success;

            GoogleState.Devices[sessionId][typeof(LockDevice)] = vehicleLock;

            return new JObject
            {
                { "online", success },
                { "isLocked", vehicleLock.Locked },
                { "isJammed", true /* Need to investigate Secure Remote Protocol (SRP) */ },

                /* Custom Syntax, also need to implement */
                {
                    "errors", new JObject
                    {
                        { "status", "FAILURE" },
                        { "errorCode", "remoteSetDisabled" },
                        { "errorCodeReason", "remoteUnlockNotAllowed" }
                    }
                }
            };
        }
        else
        {
            throw new NotImplementedException($"Command: {(string?)data.Root["command"]} is not implemented");
        }

        return new JObject();
    }
}