// Copyright (c) Nathan Ford. All rights reserved. CarInfoModel.cs

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

        public CarModel? car { get; set; }
    }
}