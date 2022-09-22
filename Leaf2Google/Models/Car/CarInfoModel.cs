// Copyright (c) Nathan Ford. All rights reserved. CarInfoModel.cs

using Leaf2Google.Models.Google.Devices;
using System.Drawing;

namespace Leaf2Google.Models.Car
{
    public class CarInfoModel
    {
        public Lock? carlock { get; set; }

        public Thermostat? thermostat { get; set; }

        public CarModel? car { get; set; }

        public PointF? location { get; set; }
    }
}