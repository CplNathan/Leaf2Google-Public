// Copyright (c) Nathan Ford. All rights reserved. CarInfoModel.cs

using Leaf2Google.Models.Google.Devices;

namespace Leaf2Google.Models.Leaf
{
    public class CarInfo
    {
        public Lock? carlock { get; set; }

        public Thermostat? thermostat { get; set; }
    }
}