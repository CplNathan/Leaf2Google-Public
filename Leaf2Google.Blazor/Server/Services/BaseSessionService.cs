// Copyright (c) Nathan Ford. All rights reserved. BaseSessionService.cs

using Leaf2Google.Entities.Car;
using Leaf2Google.Entities.Generic;
using Leaf2Google.Models.Car.Sessions;
using Leaf2Google.Models.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Drawing;
using System.Net;
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
        IOptions<ConfigModel> options)
    {
        Client = client;
        LeafContext = leafContext;
        StorageManager = storageManager;
        Logging = logging;
        ServiceScopeFactory = serviceScopeFactory;
        AppOptions = options.Value;

        OnRequest += BaseSessionManager_OnRequest;
        OnAuthenticationAttempt += BaseSessionManager_OnAuthenticationAttempt;
    }

    protected bool Disposed { get; private set; }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.Disposed)
        {
            if (disposing)
            {
                OnRequest -= BaseSessionManager_OnRequest;
                OnAuthenticationAttempt -= BaseSessionManager_OnAuthenticationAttempt;
            }

            this.Disposed = true;
        }
    }

    protected HttpClient Client { get; }

    protected LeafContext LeafContext { get; }

    protected BaseStorageService StorageManager { get; }

    protected LoggingService Logging { get; }

    private IServiceScopeFactory ServiceScopeFactory { get; }

    protected ConfigModel AppOptions { get; }

    public static event RequestDelegate OnRequest;

    public static event AuthDelegate OnAuthenticationAttempt;

    private async void BaseSessionManager_OnAuthenticationAttempt(object sender, string? authToken)
    {
        if (sender is VehicleSessionBase session)
        {
            var scope = ServiceScopeFactory.CreateScope();
            var nissanContext = scope.ServiceProvider.GetRequiredService<LeafContext>();

            if (!session.Authenticated)
            {
                session.LoginFailedCount += 1;
            }
            else
            {
                session.LoginFailedCount = 0;
            }

            if (!session.Authenticated && session.LoginGivenUp)
            {
                await StorageManager.DeleteAndUnload(session.SessionId).ConfigureAwait(false);

                Console.WriteLine(Logging.AddLog(session.SessionId, AuditAction.Delete, AuditContext.Leaf,
                    "Deleting Stale Leaf"));
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

            await nissanContext.SaveChangesAsync().ConfigureAwait(false);
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
                var loginSuccess = await Login(session).ConfigureAwait(false);
                if (loginSuccess)
                {
                    await AttemptRetryQueue().ConfigureAwait(false);
                }
            }
        }
    }

    public Task AttemptRetryQueue()
    {
        return Task.CompletedTask;
    }

    [Obsolete("Use MakeRequest<T> instead.")]
    protected async Task<Response<JsonObject>?> MakeRequest(VehicleSessionBase session, HttpRequestMessage httpRequestMessage,
        string baseAddress = "")
    {
        return await MakeRequest<JsonObject>(session, httpRequestMessage, baseAddress).ConfigureAwait(false);
    }

    protected async Task<Response<T>?> MakeRequest<T>(VehicleSessionBase session, HttpRequestMessage httpRequestMessage,
        string baseAddress = "")
    {
        var success = true;
        Response<T>? result;
        try
        {
            httpRequestMessage.RequestUri =
                new Uri(
                    $"{(string.IsNullOrEmpty(baseAddress) ? AppOptions.Nissan.EU.auth_base_url : baseAddress)}{httpRequestMessage.RequestUri?.ToString() ?? ""}");
            httpRequestMessage.Headers.Add("User-Agent", "NissanConnect/2 CFNetwork/978.0.7 Darwin/18.7.0");

            result = await Client.MakeRequest<T>(httpRequestMessage).ConfigureAwait(false);

            if (result?.Code is not ((int)HttpStatusCode.OK) and not ((int)HttpStatusCode.Found))
            {
                success = false;
            }
        }
        catch (Exception)
        {
            throw;
        }

        if (result != null)
        {
            result.Success = success;
        }

        OnRequest?.Invoke(session, result?.Success ?? false);

        return result;
    }

    public async Task<bool> Login(VehicleSessionBase session)
    {
        if (session is null)
        {
            return false;
        }

        Console.WriteLine(Logging.AddLog(session.SessionId, AuditAction.Access, AuditContext.Leaf,
    "Authentication Method Invoked"));

        // If this is called concurrently we could add it to a queue/task where we can await the result of the previous authentication attempt although I am unsure of the benifit of this.
        if (session.LoginAuthenticationAttempting)
        {
            return false;
        }

        // Add cooldown to authentication attempts in-case multiple requests hit at once.
        if (DateTime.UtcNow - session.LastLoginAuthenticaionAttempted <= TimeSpan.FromSeconds(5))
        {
            return false;
        }

        if (!session.LoginGivenUp && !session.Authenticated)
        {
            session.LoginAuthenticationAttempting = true;

            Console.WriteLine(Logging.AddLog(session.SessionId, AuditAction.Access, AuditContext.Leaf,
                "Authentication Attempting"));
            await LoginImplementation(session).ConfigureAwait(false);

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
        {
            await AddAsync(new CarEntity(leaf.NissanUsername, leaf.NissanPassword) { CarModelId = leaf.CarModelId }).ConfigureAwait(false);
        }

        await LeafContext.SaveChangesAsync().ConfigureAwait(false);
    }

    private async Task<bool> InternalAddAsync(CarEntity NewCar)
    {
        var session = new NissanConnectSession(NewCar.NissanUsername, NewCar.NissanPassword, NewCar.CarModelId);

        // Skip login check if already authenticated.
        VehicleSessionBase vehicleSession;
        var vehicleSessionFound = StorageManager.VehicleSessions.TryGetValue(session.SessionId, out vehicleSession!);
        if (vehicleSessionFound && vehicleSession.Authenticated)
        {
            return true;
        }

        var success = false;
        try
        {
            StorageManager.VehicleSessions.TryAdd(session.SessionId, session);
            success = await Login(session).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine(Logging.AddLog(session.SessionId, AuditAction.Exception, AuditContext.Leaf,
                ex.ToString()));
            throw;
        }

        return success;
    }

    public async Task<bool> AddAsync(CarEntity NewCar)
    {
        if (NewCar == null)
            return false;

        var success = await InternalAddAsync(NewCar).ConfigureAwait(false);

        if (success)
        {
            await LeafContext.SaveChangesAsync().ConfigureAwait(false);
        }

        return success;
    }

    protected abstract Task<bool> LoginImplementation(VehicleSessionBase session);

    protected async Task<Response<JsonObject>?> PerformAction(VehicleSessionBase session, string? vin, string action, string type,
        JsonObject attributes)
    {
        if (session == null)
            throw new InvalidOperationException("Session can not be null");

        Console.WriteLine(Logging.AddLog(session.SessionId, AuditAction.Execute, AuditContext.Leaf,
            $"Performing action {action} on {vin}"));
        var response = await PerformActionImplementation(session, vin, action, type, attributes).ConfigureAwait(false);

        return response;
    }

    protected abstract Task<Response<JsonObject>?> PerformActionImplementation(VehicleSessionBase session, string? vin, string action,
        string type, JsonObject attributes);

    protected async Task<Response<JsonObject>?> GetStatus(VehicleSessionBase session, string? vin, string action)
    {
        if (session == null)
            throw new InvalidOperationException("Session can not be null");

        Console.WriteLine(Logging.AddLog(session.SessionId, AuditAction.Execute, AuditContext.Leaf,
            $"Getting status {action} on {vin}"));
        var response = await GetStatusImplementation(session, vin, action).ConfigureAwait(false);

        return response;
    }

    protected abstract Task<Response<JsonObject>?> GetStatusImplementation(VehicleSessionBase session, string? vin, string action);
}