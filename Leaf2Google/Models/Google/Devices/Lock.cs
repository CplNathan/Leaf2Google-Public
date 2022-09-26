// Copyright (c) Nathan Ford. All rights reserved. Lock.cs

using Leaf2Google.Dependency.Managers;
using Leaf2Google.Models.Car;
using Newtonsoft.Json.Linq;

namespace Leaf2Google.Models.Google.Devices
{
    public class Lock : BaseDevice
    {
        public bool Locked { get; set; } = true;

        public int CapacityRemaining { get; set; } = 100;

        public int KillometersRemaining { get; set; } = 200;

        public int KillowatCapacity { get; set; } = 40;

        public int MinutesTillFull { get; set; } = 360;

        public bool IsCharging { get; set; }

        public bool IsPluggedIn { get; set; }

        public Lock(string Id, string Name)
            : base(Id, Name)
        {
            this.type = "LOCK";
            this.traits = new List<string>() { "LockUnlock", "Locator", "EnergyStorage" };
            this.WillReportState = true;
            this.Attributes = new JObject()
            {
                { "isRechargeable", true },
                { "queryOnlyEnergyStorage", true /* TODO: implement */ },
                { "energyStorageDistanceUnitForUX", "MILES" }
            };

            this.SupportedCommands = new List<string>() { "LockUnlock", "Locate" };
        }

        /*
         Last = {"attributes": {
              "lockStatus": "locked",
              "doorStatusFrontLeft": "closed",
              "doorStatusFrontRight": "closed",
              "doorStatusRearLeft": "closed",
              "doorStatusRearRight": "closed",
              "hatchStatus": "closed",
              "lastUpdateTime": "2022-08-31T02:16:25...
        */

        public async override Task<bool> FetchAsync(LeafSessionManager sessionManager, Guid sessionId, string? vin, bool forceFetch = false)
        {
            bool success = false;

            if (WillFetch || forceFetch)
            {
                var lockStatus = await sessionManager.VehicleLock(sessionId, vin);

                var batteryStatus = await sessionManager.VehicleBattery(sessionId, vin);

                if (lockStatus is not null && lockStatus.Success == true && batteryStatus is not null && batteryStatus.Success == true)
                {
                    success = lockStatus.Success && batteryStatus.Success;
                    Locked = (string?)lockStatus.Data?.data.attributes.lockStatus == "locked";
                    CapacityRemaining = (int?)batteryStatus.Data?.data.attributes.batteryLevel ?? CapacityRemaining;
                    KillometersRemaining = (int?)batteryStatus.Data?.data.attributes.rangeHvacOff ?? KillometersRemaining;
                    KillowatCapacity = (int?)batteryStatus.Data?.data.attributes.batteryCapacity / 1000 ?? KillowatCapacity;
                    MinutesTillFull = (int?)batteryStatus.Data?.data.attributes.timeRequiredToFullFast ?? MinutesTillFull;
                    IsCharging = (bool?)batteryStatus.Data?.data.attributes.chargeStatus ?? false;
                    IsPluggedIn = (bool?)batteryStatus.Data?.data.attributes.plugStatus ?? false;
                    LastUpdated = DateTime.UtcNow;
                }
            } else
            {
                success = true;
            }

            return success;
        }

        public async override Task<JObject> QueryAsync(LeafSessionManager sessionManager, Guid sessionId, string? vin) // Pass in what to query?
        {
            bool success = await FetchAsync(sessionManager, sessionId, vin);

            var descriptiveCapacity = "FULL";

            if (CapacityRemaining < 15)
                descriptiveCapacity = "CRITICALLY_LOW";
            else if (CapacityRemaining < 40)
                descriptiveCapacity = "LOW";
            else if (CapacityRemaining < 60)
                descriptiveCapacity = "MEDIUM";
            else if (CapacityRemaining < 95)
                descriptiveCapacity = "HIGH";
            else
                descriptiveCapacity = "FULL";

            var currentKillowat = (int)(KillowatCapacity * (CapacityRemaining / 100));

            return new JObject()
            {
                { "status", "SUCCESS" },
                { "online", success },
                { "descriptiveCapacityRemaining", descriptiveCapacity },
                { "capacityRemaining", new JArray()
                {
                    new JObject()
                    {
                        { "rawValue", CapacityRemaining },
                        { "unit", "PERCENTAGE" }
                    },
                    new JObject()
                    {
                        { "rawValue", KillometersRemaining },
                        { "unit", "KILOMETERS" }
                    },
                    new JObject()
                    {
                        { "rawValue", KillometersRemaining / 1.609 },
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
                        { "rawValue", MinutesTillFull * 60 },
                        { "unit", "SECONDS" }
                    },
                    new JObject()
                    {
                        { "rawValue", KillowatCapacity - currentKillowat },
                        { "unit", "KILOWATT_HOURS" }
                    }
                } },
                { "isCharging", IsCharging },
                { "isPluggedIn", IsPluggedIn },
                { "isLocked", Locked },
                { "isJammed", false }
            };
        }

        public async override Task<JObject> ExecuteAsync(LeafSessionManager sessionManager, Guid sessionId, string? vin, JObject data)
        {
            if ((string?)data.Root["command"] == "action.devices.commands.Locate" && ((bool?)data["silence"] ?? false) == false)
            {
                var flashStatus = await sessionManager.FlashLights(sessionId, vin, 5);
            }
            else if ((string?)data.Root["command"] == "action.devices.commands.LockUnlock")
            {
                Locked = data.ContainsKey("lock") ? (bool?)data["lock"] ?? Locked : Locked;

                var lockStatus = await sessionManager.SetVehicleLock(sessionId, vin, Locked);

                bool success = false;
                if (lockStatus is not null && lockStatus.Success)
                {
                    success = lockStatus.Success;
                }

                return new JObject()
                {
                    { "online", success },
                    { "isLocked", Locked },
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