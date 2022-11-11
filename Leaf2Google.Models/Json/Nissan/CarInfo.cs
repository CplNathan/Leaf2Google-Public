// Copyright (c) Nathan Ford. All rights reserved. CarInfo.cs

using System;
using System.Collections.Generic;
using System.Text;

namespace Leaf2Google.Models.Json.Nissan
{

    public class Nissan
    {
        public object data { get; set; }
    }

    public class CarInfo
    {
        public string vin { get; set; }
        public object color { get; set; }
        public string modelName { get; set; }
        public object nickname { get; set; }
        public string energy { get; set; }
        public string pictureURL { get; set; }
        public object registrationNumber { get; set; }
        public object firstRegistrationDate { get; set; }
        public object batteryCode { get; set; }
        public string engineType { get; set; }
        public object syncStatus { get; set; }
        public string carGateway { get; set; }
        public int phase { get; set; }
        public string privacyMode { get; set; }
        public Service[] services { get; set; }
        public string iceEvFlag { get; set; }
        public string canGeneration { get; set; }
        public bool stolenVehicleFlag { get; set; }
        public Svt svt { get; set; }
        public string modelCode { get; set; }
        public string vinHash { get; set; }
        public string vinCrypt { get; set; }
        public string uuid { get; set; }
        public int vidInt { get; set; }
        public Ivc ivc { get; set; }
        public Ivi ivi { get; set; }
        public DateTime vehicleOwnedSince { get; set; }
        public string modelYear { get; set; }
        public string vehicleNickName { get; set; }
    }

    public class Svt
    {
        public string state { get; set; }
        public object lastUpdatedTime { get; set; }
    }

    public class Ivc
    {
        public bool present { get; set; }
        public string imei { get; set; }
        public string lastInstalledDate { get; set; }
        public string msisdn { get; set; }
        public string simId { get; set; }
        public string swVersion { get; set; }
        public string version { get; set; }
        public string ivcId { get; set; }
    }

    public class Ivi
    {
        public bool present { get; set; }
        public string orderPartNumber { get; set; }
        public string supplierSwVersion { get; set; }
        public string type { get; set; }
        public string iviId { get; set; }
        public string version { get; set; }
    }

    public class Service
    {
        public int id { get; set; }
        public string activationState { get; set; }
        public Feature[] features { get; set; }
    }

    public class Feature
    {
        public int id { get; set; }
        public string state { get; set; }
    }

}
