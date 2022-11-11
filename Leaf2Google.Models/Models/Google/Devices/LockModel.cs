// Copyright (c) Nathan Ford. All rights reserved. Lock.cs

using System.Collections.Generic;
using System.Drawing;
using System.Text.Json.Nodes;

namespace Leaf2Google.Models.Google.Devices
{

    public class LockModel : BaseDeviceModel
    {
        public LockModel(string Id, string Name)
            : base(Id, Name)
        {
            type = "LOCK";
            traits = new List<string> { "LockUnlock", "Locator", "EnergyStorage" };
            WillReportState = true;
            Attributes = new JsonObject()
            {
                { "isRechargeable", true },
                { "queryOnlyEnergyStorage", true /* TODO: implement */ },
                { "energyStorageDistanceUnitForUX", "MILES" }
            };

            SupportedCommands = new List<string> { "LockUnlock", "Locate" };
        }

        public bool Locked { get; set; } = true;

        public int CapacityRemaining { get; set; } = 100;

        public int KillometersRemaining { get; set; } = 200;

        public int KillowatCapacity { get; set; } = 40;

        public int MinutesTillFull { get; set; } = 360;

        public bool IsCharging { get; set; }

        public bool IsPluggedIn { get; set; }

        public PointF? Location { get; set; }
    }
}