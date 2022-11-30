// Copyright (c) Nathan Ford. All rights reserved. IntentRequest.cs

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Leaf2Google.Json.Google
{
    public class GoogleIntentRequest
    {
        public string requestId { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Input[]? inputs { get; set; }
    }

    public class Input
    {
        public string intent { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public RequestPayload? payload { get; set; }
    }

    public class RequestPayload
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public RequestDevice[]? devices { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Command[]? commands { get; set; }
    }

    public class Command
    {
        public RequestDevice[] devices { get; set; }
        public Execution[] execution { get; set; }
    }

    public class Execution
    {
        public string command { get; set; }

        [JsonPropertyName("params")]
        public JsonObject _params { get; set; }
    }

    public class RequestDevice
    {
        public string id { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public JsonObject? customData { get; set; }
    }
}
