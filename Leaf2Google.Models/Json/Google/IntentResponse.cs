// Copyright (c) Nathan Ford. All rights reserved. IntentResponse.cs

using Leaf2Google.Json.Google;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Leaf2Google.Json.Google
{

    public class GoogleIntentResponse
    {
        public GoogleIntentResponse(GoogleIntentRequest request) {
            this.requestId = request.requestId;
        }

        public string requestId { get; set; }

        public ResponsePayload payload { get; set; }
    }

    // Important for polymorphism
    [JsonDerivedType(typeof(SyncPayload))]
    [JsonDerivedType(typeof(QueryPayload))]
    [JsonDerivedType(typeof(ExecutePayload))]
    public class ResponsePayload
    {
        public string agentUserId { get; set; }
    }

    public class SyncPayload : ResponsePayload
    {
        public List<JsonObject> devices { get; set; }
    }

    public class QueryPayload : ResponsePayload
    {
        public JsonObject devices { get; set; }
    }

    public class ExecutePayload : ResponsePayload
    {
        public List<ExecuteDeviceData> commands { get; set; }
    }

    // Important for polymorphism
    [JsonDerivedType(typeof(LockDeviceData))]
    [JsonDerivedType(typeof(ThermostatDeviceData))]
    public class QueryDeviceData
    {
        public string status { get; set; }
        public bool online { get; set; }
    }

    public class LockDeviceData : QueryDeviceData
    {
        public string descriptiveCapacityRemaining { get; set; }
        public List<ValueUnit> capacityRemaining { get; set; }
        public List<ValueUnit> capacityUntilFull { get; set; }
        public bool isCharging { get; set; }
        public bool isPluggedIn { get; set; }
        public bool isLocked { get; set; }
        public bool isJammed { get; set; }
    }

    public class ThermostatDeviceData : QueryDeviceData
    {
        public string thermostatMode { get; set; }
        public decimal thermostatTemperatureSetpoint { get; set; }
        public decimal thermostatTemperatureAmbient { get; set; }
    }

    public class ValueUnit
    {
        public int rawValue { get; set; }
        public string unit { get; set; }
    }

    public class ExecuteDeviceData
    {
        public List<string> ids { get; set; }
        public string status { get; set; }
        public JsonObject states { get; set; }

    }

    public class ExecuteDeviceDataSuccess : ExecuteDeviceData
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonObject states { get; set; }
    }

    public class ExecuteDeviceDataError : ExecuteDeviceData
    {
        public string errorCode { get; set; }
        public string errorCodeReason { get; set; }
    }

    public class Color
    {
        public string name { get; set; }
        public int spectrumRGB { get; set; }
    }

}
