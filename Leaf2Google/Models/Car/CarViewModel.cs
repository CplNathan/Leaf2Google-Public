// Copyright (c) Nathan Ford. All rights reserved. CarInfoModel.cs

using Leaf2Google.Models.Google.Devices;
using System.Drawing;

namespace Leaf2Google.Models.Car
{
    public class CarViewModel
    {
        public LockModel? carLock { get; set; }

        public ThermostatModel? carThermostat { get; set; }

        public CarModel? car { get; set; }

        public PointF? location { get; set; }
    }
}