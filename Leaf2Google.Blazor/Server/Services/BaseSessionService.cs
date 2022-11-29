// Copyright (c) Nathan Ford. All rights reserved. SessionManagerBase.cs

using System.Drawing;
using System.Net;
using Leaf2Google.Entities.Car;
using Leaf2Google.Entities.Generic;
using Leaf2Google.Models.Car.Sessions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Leaf2Google.Services;

public interface ICarSessionManager
{
    Task StartAsync();

    Task<bool> AddAsync(CarEntity NewLeaf);

    Task<PointF> VehicleLocation(VehicleSessionBase session, string? vin);
    Task<Response<JsonObject>?> VehicleClimate(VehicleSessionBase session, string? vin, bool forceUpdate = true);
    Task<Response<JsonObject>?> VehicleLock(VehicleSessionBase session, string? vin);
    Task<Response<JsonObject>?> VehicleBattery(VehicleSessionBase session, string? vin);
    Task<Response<JsonObject>?> SetVehicleClimate(VehicleSessionBase session, string? vin, decimal targetTemp, bool active);
    Task<Response<JsonObject>?> SetVehicleLock(VehicleSessionBase session, string? vin, bool locked);
    Task<Response<JsonObject>?> FlashLights(VehicleSessionBase session, string? vin, int duration = 5);
    Task<Response<JsonObject>?> BeepHorn(VehicleSessionBase session, string? vin, int duration = 5);

    Task<bool> Login(VehicleSessionBase session);
}

public delegate void AuthDelegate(object sender, string? authToken);

public delegate void RequestDelegate(object sender, bool requestSuccess);

public abstract class BaseSessionService : IDisposable
{
    public BaseSessionService(HttpClient client, LeafContext leafContext, BaseStorageService storageManager, LoggingService logging, IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration)
    {
        Client = client;
        LeafContext = leafContext;
        StorageManager = storageManager;
        Logging = logging;
        ServiceScopeFactory = serviceScopeFactory;
        Configuration = configuration;

        OnRequest += BaseSessionManager_OnRequest;
        OnAuthenticationAttempt += BaseSessionManager_OnAuthenticationAttempt;
    }

    public void Dispose()
    {
        OnRequest -= BaseSessionManager_OnRequest;
        OnAuthenticationAttempt -= BaseSessionManager_OnAuthenticationAttempt;
    }

    protected Dictionary<DateTime, HttpRequestMessage> RetryQueue = new();

    protected HttpClient Client { get; }

    protected LeafContext LeafContext { get; }

    protected BaseStorageService StorageManager { get; }

    protected LoggingService Logging { get; }

    private IServiceScopeFactory ServiceScopeFactory { get; }

    protected IConfiguration Configuration { get; }

    public static event RequestDelegate OnRequest;

    public static event AuthDelegate OnAuthenticationAttempt;

    private async void BaseSessionManager_OnAuthenticationAttempt(object sender, string? authToken)
    {
        if (sender is VehicleSessionBase session)
        {
            var scope = ServiceScopeFactory.CreateScope();
            var nissanContext = scope.ServiceProvider.GetRequiredService<LeafContext>();

            if (!session.Authenticated)
                session.LoginFailedCount += 1;
            else
                session.LoginFailedCount = 0;

            if (!session.Authenticated && session.LoginGivenUp)
            {
                var leaf = await nissanContext.NissanLeafs.FirstOrDefaultAsync(car =>
                    car.CarModelId == session.SessionId);
                if (leaf != null)
                {
                    leaf.Deleted = DateTime.UtcNow;
                    nissanContext.Entry(leaf).State = EntityState.Modified;
                }

                Console.WriteLine(Logging.AddLog(session.SessionId, AuditAction.Delete, AuditContext.Leaf,
                    "Deleting Stale Leaf"));

                StorageManager.VehicleSessions.Remove(session.SessionId);
            }
            else if (session.Authenticated)
            {
                Console.WriteLine(Logging.AddLog(session.SessionId, AuditAction.Access, AuditContext.Leaf,
                    "Authentication Success"));
            }
            else
            {
                Console.WriteLine(Logging.AddLog(session.SessionId, AuditAction.Access, AuditContext.Leaf,
                    "Authentication Failed"));
                await Login(session).ConfigureAwait(false);
            }

            await nissanContext.SaveChangesAsync();
        }
    }

    private async void BaseSessionManager_OnRequest(object sender, bool requestSuccess)
    {
        // This should not be used as various contexts may be disposed of when calling.
        if (sender is VehicleSessionBase session)
        {
            session.LastRequestSuccessful = requestSuccess;

            if (!session.Authenticated && !session.LoginGivenUp &&
            session.LastAuthenticated > DateTime.MinValue && !requestSuccess && !session.LoginAuthenticationAttempting)
            {
                var loginSuccess = await Login(session);
                if (loginSuccess)
                    await AttemptRetryQueue().ConfigureAwait(false);
            }
        }
    }

    public async Task AttemptRetryQueue()
    {
        var queue = RetryQueue.Select(item => (DateTime.UtcNow - item.Key) <= TimeSpan.FromSeconds(15)).ToList();
        RetryQueue.Clear();

        foreach (var item in queue)
        {

        }
    }

    [Obsolete]
    protected async Task<Response<JsonObject>?> MakeRequest(VehicleSessionBase session, HttpRequestMessage httpRequestMessage,
        string baseUri = "")
    {
        return await MakeRequest<JsonObject>(session, httpRequestMessage, baseUri);
    }

    protected async Task<Response<T>?> MakeRequest<T>(VehicleSessionBase session, HttpRequestMessage httpRequestMessage,
        string baseUri = "")
    {
        var success = true;
        Response<T>? result = null;

        try
        {
            httpRequestMessage.RequestUri =
                new Uri(
                    $"{(string.IsNullOrEmpty(baseUri) ? Configuration["Nissan:EU:auth_base_url"] : baseUri)}{httpRequestMessage.RequestUri?.ToString() ?? ""}");
            httpRequestMessage.Headers.Add("User-Agent", "NissanConnect/2 CFNetwork/978.0.7 Darwin/18.7.0");

            result = await Client.MakeRequest<T>(httpRequestMessage);

            if (result?.Code != (int)HttpStatusCode.OK && result?.Code != (int)HttpStatusCode.Found)
                success = false;
        }
        catch (Exception)
        {
            success = false;
            throw;
        }

        if (result != null)
            result.Success = success;

        OnRequest?.Invoke(session, result?.Success ?? false);

        return result;
    }

    public async Task<bool> Login(VehicleSessionBase session)
    {
        if (session is null)
            return false;

        Console.WriteLine(Logging.AddLog(session.SessionId, AuditAction.Access, AuditContext.Leaf,
    "Authentication Method Invoked"));

        // If this is called concurrently we could add it to a queue/task where we can await the result of the previous authentication attempt although I am unsure of the benifit of this.
        if (session.LoginAuthenticationAttempting)
            return false;

        // Add cooldown to authentication attempts in-case multiple requests hit at once.
        if (DateTime.UtcNow - session.LastLoginAuthenticaionAttempted <= TimeSpan.FromSeconds(5))
            return false;

        if (!session.LoginGivenUp && !session.Authenticated)
        {
            session.LoginAuthenticationAttempting = true;

            Console.WriteLine(Logging.AddLog(session.SessionId, AuditAction.Access, AuditContext.Leaf,
                "Authentication Attempting"));
            await LoginImplementation(session);

            session.LoginAuthenticationAttempting = false;
        }
        else
        {
            Console.WriteLine(Logging.AddLog(session.SessionId, AuditAction.Access, AuditContext.Leaf,
                "Authentication Attempted - But Given Up"));

            return false;
        }

        OnAuthenticationAttempt?.Invoke(session, session.AuthenticatedAccessToken);

        return session.Authenticated;
    }

    public async Task StartAsync()
    {
        // Queue saved sessions into memory.
        foreach (var leaf in LeafContext.NissanLeafs)
            await AddAsync(new CarEntity(leaf.NissanUsername, leaf.NissanPassword) { CarModelId = leaf.CarModelId }).ConfigureAwait(false);

        await LeafContext.SaveChangesAsync();
    }

    private async Task<bool> AddAsync(CarEntity NewCar, bool _ = false)
    {
        var session = new NissanConnectSession(NewCar.NissanUsername, NewCar.NissanPassword, NewCar.CarModelId);

        // Skip login check if already authenticated.
        if (StorageManager.VehicleSessions.ContainsKey(session.SessionId) && StorageManager.VehicleSessions[session.SessionId].Authenticated)
            return true;

        var success = false;
        try
        {
            StorageManager.VehicleSessions.TryAdd(session.SessionId, session);
            success = await Login(session);
        }
        catch (Exception ex)
        {
            Console.WriteLine(Logging.AddLog(session.SessionId, AuditAction.Exception, AuditContext.Leaf,
                ex.ToString()));
        }

        return success;
    }

    public async Task<bool> AddAsync(CarEntity NewCar)
    {
        var success = await AddAsync(NewCar, false);

        if (success)
            await LeafContext.SaveChangesAsync();

        return success;
    }

    protected abstract Task<bool> LoginImplementation(VehicleSessionBase session);

    protected async Task<Response<JsonObject>?> PerformAction(VehicleSessionBase session, string? vin, string action, string type,
        JsonObject attributes)
    {
        Console.WriteLine(Logging.AddLog(session.SessionId, AuditAction.Execute, AuditContext.Leaf,
            $"Performing action {action} on {vin}"));
        var response = await PerformActionImplementation(session, vin, action, type, attributes);

        return response;
    }

    protected abstract Task<Response<JsonObject>?> PerformActionImplementation(VehicleSessionBase session, string? vin, string action,
        string type, JsonObject attributes);

    protected async Task<Response<JsonObject>?> GetStatus(VehicleSessionBase session, string? vin, string action)
    {
        Console.WriteLine(Logging.AddLog(session.SessionId, AuditAction.Execute, AuditContext.Leaf,
            $"Getting status {action} on {vin}"));
        var response = await GetStatusImplementation(session, vin, action);

        return response;
    }

    protected abstract Task<Response<JsonObject>?> GetStatusImplementation(VehicleSessionBase session, string? vin, string action);
}