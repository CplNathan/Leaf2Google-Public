// Copyright (c) Nathan Ford. All rights reserved. BaseDeviceService.cs

using Leaf2Google.Json.Google;
using Leaf2Google.Models.Car.Sessions;
using Leaf2Google.Models.Google.Devices;
using System.Text.Json.Nodes;

namespace Leaf2Google.Services.Google.Devices;

public interface IDevice
{
    public Task<QueryDeviceData> QueryAsync(VehicleSessionBase session, BaseDeviceModel deviceModel, string? vin);
    public Task<ExecuteDeviceData> ExecuteAsync(VehicleSessionBase session, BaseDeviceModel deviceModel, string? vin, string command, JsonObject data);
    public Task<bool> FetchAsync(VehicleSessionBase session, BaseDeviceModel deviceModel, string? vin, bool forceFetch = false);

    public Type DeviceModel { get; }
}

public class BaseDeviceService
{
    public BaseDeviceService(ICarSessionManager sessionManager)
    {
        SessionManager = sessionManager;
    }

    protected ICarSessionManager SessionManager { get; }

    protected BaseStorageService StorageManager { get; }
}