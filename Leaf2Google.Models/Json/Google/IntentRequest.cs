// Copyright (c) Nathan Ford. All rights reserved. Intent.cs

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Leaf2Google.Json.Google
{
    public class GoogleIntentRequest
    {
        public string requestId { get; set; }
        public Input[] inputs { get; set; }
    }

    public class Input
    {
        public string intent { get; set; }
        public RequestPayload payload { get; set; }
    }

    public class RequestPayload
    {
        public RequestDevice[] devices { get; set; }

        public Command[] commands { get; set; }
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
        public JsonObject customData { get; set; }
    }
}
