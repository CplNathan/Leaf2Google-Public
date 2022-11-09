// Copyright (c) Nathan Ford. All rights reserved. BaseDevice.cs

using Leaf2Google.Models.Car;
using Newtonsoft.Json.Linq;

namespace Leaf2Google.Dependency.Google.Devices;

public interface IDevice
{
    public Task<JObject> QueryAsync(VehicleSessionBase session, string? vin);
    public Task<JObject> ExecuteAsync(VehicleSessionBase session, string? vin, JObject data);

    public Task<bool> FetchAsync(VehicleSessionBase session, string? vin, bool forceFetch = false);
}

public abstract class BaseDevice
{
    public BaseDevice(GoogleStateManager googleState, ICarSessionManager sessionManager, BaseStorageManager storageManager)
    {
        GoogleState = googleState;
        SessionManager = sessionManager;
        StorageManager = storageManager;
    }

    protected GoogleStateManager GoogleState { get; }

    protected ICarSessionManager SessionManager { get; }

    protected BaseStorageManager StorageManager { get; }
}