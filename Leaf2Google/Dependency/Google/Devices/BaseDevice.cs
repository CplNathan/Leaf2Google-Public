// Copyright (c) Nathan Ford. All rights reserved. BaseDevice.cs

using Leaf2Google.Dependency.Car;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace Leaf2Google.Dependency.Google.Devices
{
    public interface IDevice
    {
        public abstract Task<JObject> QueryAsync(Guid sessionId, string? vin);
        public abstract Task<JObject> ExecuteAsync(Guid sessionId, string? vin, JObject data);

        public abstract Task<bool> FetchAsync(Guid sessionId, string? vin, bool forceFetch = false);
    }

    public abstract class BaseDevice
    {
        protected readonly HttpClient _client;

        protected HttpClient Client { get => _client; }

        protected readonly GoogleStateManager _googleState;

        protected readonly LeafSessionManager _sessionManager;

        public BaseDevice(HttpClient client, GoogleStateManager googleState, LeafSessionManager sessionManager)
        {
            _client = client;
            _googleState = googleState;
            _sessionManager = sessionManager;
        }

        public abstract Task<JObject> QueryAsync(Guid sessionId, string? vin);

        public abstract Task<JObject> ExecuteAsync(Guid sessionId, string? vin, JObject data);

        public abstract Task<bool> FetchAsync(Guid sessionId, string? vin, bool forceFetch = false);
    }
}