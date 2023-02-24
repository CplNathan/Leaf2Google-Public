// Copyright (c) Nathan Ford. All rights reserved. BaseDeviceModel.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Leaf2Google.Models.Google.Devices
{
    public class SyncResponse
    {
        // TODO: Convert these into nice names, decide on a naming convention and enforce it... then assign JsonProperty names for funky ones.
        public string? id { get; set; }
        public string? type { get; set; }
        public string[]? traits { get; set; }
        public Name? name { get; set; }
        public bool willReportState { get; set; }
        public bool notificationSupportedByAgent { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonObject? attributes { get; set; }
        public Deviceinfo? deviceInfo { get; set; }
    }

    public class Name
    {
        public string? name { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? nicknames { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? defaultNames { get; set; }
    }

    public class Deviceinfo
    {
        public string? manufacturer { get; set; }
        public string? model { get; set; }
        public string? hwVersion { get; set; }
        public string? swVersion { get; set; }
    }

    public abstract class BaseDeviceModel
    {
        public BaseDeviceModel(string Id, string Name)
        {
            this.Id = Id;
            this.Name = Name;
        }

        public string Id { get; set; }

        protected string type { get; set; } = string.Empty;

        public string Type
        {
            get => $"action.devices.types.{type}";
            set => type = value;
        }

        protected List<string> traits { get; set; }

        public List<string> Traits
        {
            get { return traits.Select(trait => $"action.devices.traits.{trait}").ToList(); }
        }

        public string Name { get; set; } = string.Empty;

        public bool WillReportState { get; set; }

        public JsonObject Attributes { get; set; }

        public JsonObject DeviceInfo { get; set; }

        private List<string> _supportedCommands { get; } = new List<string>();

        public List<string> SupportedCommands
        {
            get { return _supportedCommands.Select(command => $"action.devices.commands.{command}").ToList(); }
            set
            {
                _supportedCommands.Clear();
                _supportedCommands.AddRange(value);
            }
        }

        public DateTime LastUpdated { get; set; } = DateTime.MinValue;

        public bool WillFetch => DateTime.UtcNow - LastUpdated > TimeSpan.FromSeconds(10);

        public SyncResponse Sync()
        {
            return new()
            {
                id = Id,
                type = Type,
                traits = this.Traits.ToArray(),
                name = new Name()
                {
                    name = Name,
                    defaultNames = new List<string>()
                    {
                        "Leaf", "Nissan", "Car"
                    },
                    nicknames = new List<string>()
                    {
                        "Leaf", "Nissan", "Car"
                    }
                },
                willReportState = WillReportState,
                notificationSupportedByAgent = false,
                attributes = Attributes,
                deviceInfo = new Deviceinfo()
                {
                    manufacturer = "Nathan Leaf2Google",
                    model = "SurePet",
                    hwVersion = "1.0",
                    swVersion = "1.0"
                }
            };
        }
    }
}
