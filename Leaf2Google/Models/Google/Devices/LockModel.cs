// Copyright (c) Nathan Ford. All rights reserved. Lock.cs

using Leaf2Google.Dependency.Car;
using Newtonsoft.Json.Linq;

namespace Leaf2Google.Models.Google.Devices
{
    public class LockModel : BaseDeviceModel
    {
        public bool Locked { get; set; } = true;

        public int CapacityRemaining { get; set; } = 100;

        public int KillometersRemaining { get; set; } = 200;

        public int KillowatCapacity { get; set; } = 40;

        public int MinutesTillFull { get; set; } = 360;

        public bool IsCharging { get; set; }

        public bool IsPluggedIn { get; set; }

        public LockModel(string Id, string Name)
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
    }
}