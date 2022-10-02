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
        private readonly GoogleStateManager _googleState;

        protected GoogleStateManager GoogleState { get => _googleState; }

        private readonly ICarSessionManager _sessionManager;

        protected ICarSessionManager SessionManager { get => _sessionManager; }

        public BaseDevice(GoogleStateManager googleState, ICarSessionManager sessionManager)
        {
            _googleState = googleState;
            _sessionManager = sessionManager;
        }
    }
}