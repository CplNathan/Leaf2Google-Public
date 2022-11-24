// Copyright (c) Nathan Ford. All rights reserved. CarInfoModel.cs

using System;
using System.Drawing;
using Leaf2Google.Entities.Car;
using Leaf2Google.Models.Google.Devices;

namespace Leaf2Google.Models.Car
{

    public class CarViewModel
    {
        public LockModel? carLock { get; set; }

        public ThermostatModel? carThermostat { get; set; }

        public PointF? carLocation { get; set; }

        public string? carPicture { get; set; }

        public CarEntity? car { get; set; }
    }

    public class MapData
    {
        public System.Drawing.PointF? CarLocation { get; set; }
        public string CarPhoto { get; set; }
    }

    public class BatteryData
    {
        public bool Charging { get; set; }

        public int Charge { get; set; }

        public int RemainingCharge => Charge;

        public int UsageCharge => 100 - Charge - Math.Min(100 - Charge, 20);

        public int OptimalCharge => Math.Min(100 - Charge, 20);
    }
}