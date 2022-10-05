// Copyright (c) Nathan Ford. All rights reserved. BaseDevice.cs

using Newtonsoft.Json.Linq;

namespace Leaf2Google.Dependency.Google.Devices;

public interface IDevice
{
    public Task<JObject> QueryAsync(Guid sessionId, string? vin);
    public Task<JObject> ExecuteAsync(Guid sessionId, string? vin, JObject data);

    public Task<bool> FetchAsync(Guid sessionId, string? vin, bool forceFetch = false);
}

public abstract class BaseDevice
{
    public BaseDevice(GoogleStateManager googleState, ICarSessionManager sessionManager)
    {
        GoogleState = googleState;
        SessionManager = sessionManager;
    }

    protected GoogleStateManager GoogleState { get; }

    protected ICarSessionManager SessionManager { get; }
}