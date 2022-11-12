// Copyright (c) Nathan Ford. All rights reserved. BaseDevice.cs

using Leaf2Google.Json.Google;
using Leaf2Google.Models.Car.Sessions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Leaf2Google.Dependency.Google.Devices;

public interface IDevice
{
    public Task<QueryDeviceData> QueryAsync(VehicleSessionBase session, string? vin);
    public Task<ExecuteDeviceData> ExecuteAsync(VehicleSessionBase session, string? vin, string command, JsonObject data);
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