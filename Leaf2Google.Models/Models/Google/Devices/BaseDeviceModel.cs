﻿// Copyright (c) Nathan Ford. All rights reserved. BaseDevice.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Leaf2Google.Models.Google.Devices {

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

        protected IEnumerable<string> traits { get; set; }

        public JArray Traits
        {
            get { return JArray.FromObject(traits.Select(trait => $"action.devices.traits.{trait}")); }
            set => traits = value.ToObject<List<string>>() ?? traits;
        }

        public string Name { get; set; } = string.Empty;

        public bool WillReportState { get; set; }

        public JObject Attributes { get; set; }

        public JObject DeviceInfo { get; set; }

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

        public bool WillFetch => DateTime.UtcNow - LastUpdated > TimeSpan.FromMinutes(1);

        public virtual JObject Sync()
        {
            return new JObject
        {
            { "id", Id },
            { "type", Type },
            { "traits", Traits },
            {
                "name", new JObject
                {
                    { "name", Name }
                }
            },
            { "willReportState", WillReportState },
            { "attributes", Attributes },
            {
                "deviceInfo", new JObject
                {
                    { "manufacturer", "Nathan Leaf2Google" },
                    { "model", "Nissan Leaf" },
                    { "hwVersion", "1.0" },
                    { "swVersion", "1.0" }
                }
            }
        };
        }
    }
}