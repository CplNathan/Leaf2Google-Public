// Copyright (c) Nathan Ford. All rights reserved. Lock.cs

using Leaf2Google.Dependency.Car;
using Leaf2Google.Models.Google.Devices;
using Newtonsoft.Json.Linq;
using System.Net.Sockets;

namespace Leaf2Google.Dependency.Google.Devices
{
    public class LockDevice : BaseDevice, IDevice
    {
        public LockDevice(HttpClient client, GoogleStateManager googleState, LeafSessionManager sessionManager)
            : base(client, googleState, sessionManager)
        {
            // Use here, have as per request object not a singleton!
        }

        public override async Task<bool> FetchAsync(Guid sessionId, string? vin, bool forceFetch = false)
        {
            LockModel vehicleLock = (LockModel)_googleState.Devices[sessionId][typeof(LockDevice)];

            bool success = false;

            if (vehicleLock.WillFetch || forceFetch)
            {
                var lockStatus = await _sessionManager.VehicleLock(sessionId, vin);

                var batteryStatus = await _sessionManager.VehicleBattery(sessionId, vin);

                if (lockStatus is not null && lockStatus.Success == true && batteryStatus is not null && batteryStatus.Success == true)
                {
                    success = lockStatus.Success && batteryStatus.Success;
                    vehicleLock.Locked = (string?)lockStatus.Data?.data.attributes.lockStatus == "locked";
                    vehicleLock.CapacityRemaining = (int?)batteryStatus.Data?.data.attributes.batteryLevel ?? vehicleLock.CapacityRemaining;
                    vehicleLock.KillometersRemaining = (int?)batteryStatus.Data?.data.attributes.rangeHvacOff ?? vehicleLock.KillometersRemaining;
                    vehicleLock.KillowatCapacity = (int?)batteryStatus.Data?.data.attributes.batteryCapacity / 1000 ?? vehicleLock.KillowatCapacity;
                    vehicleLock.MinutesTillFull = (int?)batteryStatus.Data?.data.attributes.timeRequiredToFullFast ?? vehicleLock.MinutesTillFull;
                    vehicleLock.IsCharging = (bool?)batteryStatus.Data?.data.attributes.chargeStatus ?? false;
                    vehicleLock.IsPluggedIn = (bool?)batteryStatus.Data?.data.attributes.plugStatus ?? false;
                    vehicleLock.LastUpdated = DateTime.UtcNow;
                }

                _googleState.Devices[sessionId][typeof(LockDevice)] = vehicleLock;
            }
            else
            {
                success = true;
            }

            return success;
        }

        public override async Task<JObject> QueryAsync(Guid sessionId, string? vin)
        {
            bool success = await FetchAsync(sessionId, vin);

            LockModel vehicleLock = (LockModel)_googleState.Devices[sessionId][typeof(LockDevice)];

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

            var currentKillowat = (int)(vehicleLock.KillowatCapacity * (vehicleLock.CapacityRemaining / 100));

            return new JObject()
            {
                { "status", "SUCCESS" },
                { "online", success },
                { "descriptiveCapacityRemaining", descriptiveCapacity },
                { "capacityRemaining", new JArray()
                {
                    new JObject()
                    {
                        { "rawValue", vehicleLock.CapacityRemaining },
                        { "unit", "PERCENTAGE" }
                    },
                    new JObject()
                    {
                        { "rawValue", vehicleLock.KillometersRemaining },
                        { "unit", "KILOMETERS" }
                    },
                    new JObject()
                    {
                        { "rawValue", vehicleLock.KillometersRemaining / 1.609 },
                        { "unit", "MILES" }
                    },
                    new JObject()
                    {
                        { "rawValue", currentKillowat },
                        { "unit", "KILOWATT_HOURS" }
                    }
                } },
                { "capacityUntilFull", new JArray()
                {
                    new JObject()
                    {
                        { "rawValue", vehicleLock.MinutesTillFull * 60 },
                        { "unit", "SECONDS" }
                    },
                    new JObject()
                    {
                        { "rawValue", vehicleLock.KillowatCapacity - currentKillowat },
                        { "unit", "KILOWATT_HOURS" }
                    }
                } },
                { "isCharging", vehicleLock.IsCharging },
                { "isPluggedIn", vehicleLock.IsPluggedIn },
                { "isLocked", vehicleLock.Locked },
                { "isJammed", false }
            };
        }

        public override async Task<JObject> ExecuteAsync(Guid sessionId, string? vin, JObject data)
        {
            LockModel vehicleLock = (LockModel)_googleState.Devices[sessionId][typeof(LockDevice)];

            if ((string?)data.Root["command"] == "action.devices.commands.Locate" && ((bool?)data["silence"] ?? false) == false)
            {
                var flashStatus = await _sessionManager.FlashLights(sessionId, vin, 5);
            }
            else if ((string?)data.Root["command"] == "action.devices.commands.LockUnlock")
            {
                vehicleLock.Locked = data.ContainsKey("lock") ? (bool?)data["lock"] ?? vehicleLock.Locked : vehicleLock.Locked;

                var lockStatus = await _sessionManager.SetVehicleLock(sessionId, vin, vehicleLock.Locked);

                bool success = false;
                if (lockStatus is not null && lockStatus.Success)
                {
                    success = lockStatus.Success;
                }

                _googleState.Devices[sessionId][typeof(LockDevice)] = vehicleLock;

                return new JObject()
                {
                    { "online", success },
                    { "isLocked", vehicleLock.Locked },
                    { "isJammed", true /* Need to investigate Secure Remote Protocol (SRP) */ },

                    /* Custom Syntax, also need to implement */
                    { "errors", new JObject()
                    {
                        { "status", "FAILURE" },
                        { "errorCode", "remoteSetDisabled" },
                        { "errorCodeReason", "remoteUnlockNotAllowed" }
                    }}
                };
            }
            else
            {
                throw new NotImplementedException($"Command: {(string?)data.Root["command"]} is not implemented");
            }

            return new JObject();
        }
    }
}