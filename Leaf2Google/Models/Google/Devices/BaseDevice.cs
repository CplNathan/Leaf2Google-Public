// Copyright (c) Nathan Ford. All rights reserved. BaseDevice.cs

using Leaf2Google.Dependency.Managers;
using Leaf2Google.Models.Car;
using Newtonsoft.Json.Linq;

namespace Leaf2Google.Models.Google.Devices
{
    public abstract class BaseDevice
    {
        public string Id { get; set; }

        protected string type { get; set; } = string.Empty;

        public string Type
        {
            get
            {
                return $"action.devices.types.{type}";
            }
            set
            {
                type = value;
            }
        }

        protected List<string> traits { get; set; } = new List<string>();

        public JArray Traits
        {
            get
            {
                return JArray.FromObject(traits.Select(trait => $"action.devices.traits.{trait}"));
            }
            set
            {
                traits = value.ToObject<List<string>>() ?? traits;
            }
        }

        public string Name { get; set; } = string.Empty;

        public bool WillReportState { get; set; }

        public JObject Attributes { get; set; } = new JObject();

        public JObject DeviceInfo { get; set; } = new JObject();

        public BaseDevice(string Id, string Name)
        {
            this.Id = Id;
            this.Name = Name;
        }

        public virtual JObject Sync()
        {
            return new JObject()
            {
                { "id", Id },
                { "type", Type },
                { "traits", Traits },
                { "name", new JObject() {
                    { "name", Name }
                }},
                { "willReportState", WillReportState },
                { "attributes", Attributes },
                { "deviceInfo", new JObject() {
                    { "manufacturer", "Nathan Leaf2Google" },
                    { "model", "Nissan Leaf" },
                    { "hwVersion", "1.0" },
                    { "swVersion", "1.0" }
                }}
            };
        }

        private List<string> _supportedCommands { get; init; } = new List<string>();

        public List<string> SupportedCommands
        {
            get
            {
                return _supportedCommands.Select(command => $"action.devices.commands.{command}").ToList();
            }
            set
            {
                _supportedCommands.Clear();
                _supportedCommands.AddRange(value);
            }
        }

        public DateTime LastUpdated { get; set; } = DateTime.MinValue;

        public bool WillFetch { get => DateTime.UtcNow - LastUpdated > TimeSpan.FromMinutes(1); }

        public JObject Query(in LeafSessionManager sessionManager, Guid sessionId, string? vin) => QueryAsync(sessionManager, sessionId, vin).Result;
        public abstract Task<JObject> QueryAsync(LeafSessionManager sessionManager, Guid sessionId, string? vin);

        public JObject Execute(in LeafSessionManager sessionManager, Guid sessionId, string? vin, JObject data) => ExecuteAsync(sessionManager, sessionId, vin, data).Result;
        public abstract Task<JObject> ExecuteAsync(LeafSessionManager sessionManager, Guid sessionId, string? vin, JObject data);

        public bool Fetch(in LeafSessionManager sessionManager, Guid sessionId, string? vin, bool forceFetch = false) => FetchAsync(sessionManager, sessionId, vin, forceFetch).Result;
        public abstract Task<bool> FetchAsync(LeafSessionManager sessionManager, Guid sessionId, string? vin, bool forceFetch = false);
    }
}