// Copyright (c) Nathan Ford. All rights reserved. SessionManagerBase.cs

using System.Drawing;
using System.Net;
using Leaf2Google.Models.Car;
using Leaf2Google.Models.Generic;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace Leaf2Google.Dependency;

public interface ICarSessionManager
{
    Dictionary<Guid, VehicleSessionBase> VehicleSessions { get; }

    IReadOnlyDictionary<Guid, VehicleSessionBase> AllSessions { get; }
    Task StartAsync();

    Task<bool> AddAsync(CarModel NewLeaf);

    Task<PointF> VehicleLocation(Guid sessionId, string? vin);
    Task<Response?> VehicleClimate(Guid sessionId, string? vin, bool forceUpdate = true);
    Task<Response?> VehicleLock(Guid sessionId, string? vin);
    Task<Response?> VehicleBattery(Guid sessionId, string? vin);
    Task<Response?> SetVehicleClimate(Guid sessionId, string? vin, decimal targetTemp, bool active);
    Task<Response?> SetVehicleLock(Guid sessionId, string? vin, bool locked);
    Task<Response?> FlashLights(Guid sessionId, string? vin, int duration = 5);
    Task<Response?> BeepHorn(Guid sessionId, string? vin, int duration = 5);

    //Task<bool> Login(string username, string password);
}

public delegate void AuthEventHandler(object sender, Guid sessionId, string? authToken);

public delegate void RequestEventHandler(object sender, Guid sessionId, bool requestSuccess);

public abstract class BaseSessionManager
{
    public BaseSessionManager(HttpClient client, LeafContext leafContext, LoggingManager logging,
        Dictionary<Guid, VehicleSessionBase> vehicleSessions, IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration)
    {
        Client = client;
        LeafContext = leafContext;
        Logging = logging;
        VehicleSessions = vehicleSessions;
        ServiceScopeFactory = serviceScopeFactory;
        Configuration = configuration;

        OnRequest += BaseSessionManager_OnRequest;
        OnAuthenticationAttempt += BaseSessionManager_OnAuthenticationAttempt;
    }

    protected HttpClient Client { get; }

    protected LeafContext LeafContext { get; }

    protected LoggingManager Logging { get; }

    private IServiceScopeFactory ServiceScopeFactory { get; }

    protected IConfiguration Configuration { get; }

    public Dictionary<Guid, VehicleSessionBase> VehicleSessions { get; } = new();

    public IReadOnlyDictionary<Guid, VehicleSessionBase> AllSessions => VehicleSessions;

    public event RequestEventHandler OnRequest;

    public event AuthEventHandler OnAuthenticationAttempt;

    private async void BaseSessionManager_OnAuthenticationAttempt(object sender, Guid sessionId, string? authToken)
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

                Console.WriteLine(await Logging.AddLog(session.SessionId, AuditAction.Delete, AuditContext.Leaf,
                    "Deleting Stale Leaf"));

                VehicleSessions.Remove(session.SessionId);
            }
            else if (session.Authenticated)
            {
                Console.WriteLine(await Logging.AddLog(session.SessionId, AuditAction.Access, AuditContext.Leaf,
                    "Authentication Success"));
            }
            else
            {
                Console.WriteLine(await Logging.AddLog(session.SessionId, AuditAction.Access, AuditContext.Leaf,
                    "Authentication Failed"));
                _ = Login(session.SessionId);
            }

            await nissanContext.SaveChangesAsync();
        }
    }

    private void BaseSessionManager_OnRequest(object sender, Guid sessionId, bool requestSuccess)
    {
        if (sender is VehicleSessionBase session)
        {
            session.LastRequestSuccessful = requestSuccess;

            if (!session.Authenticated && !session.LoginGivenUp &&
            session.LastAuthenticated > DateTime.MinValue && !requestSuccess && !session.LoginAuthenticationAttempting)
            {
                _ = Login(sessionId);
            }
        }
    }

    protected async Task<Response?> MakeRequest(Guid sessionId, HttpRequestMessage httpRequestMessage,
        string baseUri = "")
    {
        var success = true;
        Response? result = null;

        try
        {
            httpRequestMessage.RequestUri =
                new Uri(
                    $"{(string.IsNullOrEmpty(baseUri) ? Configuration["Nissan:EU:auth_base_url"] : baseUri)}{httpRequestMessage.RequestUri?.ToString() ?? ""}");
            httpRequestMessage.Headers.Add("User-Agent", "NissanConnect/2 CFNetwork/978.0.7 Darwin/18.7.0");

            result = await Client.MakeRequest(httpRequestMessage);

            if (result?.Code != (int)HttpStatusCode.OK && result?.Code != (int)HttpStatusCode.Found)
                success = false;
        }
        catch (Exception ex)
        {
            if (ex.Source != "Newtonsoft.Json")
                success = false;
        }

        if (result != null)
            result.Success = success;

        OnRequest?.Invoke(AllSessions.FirstOrDefault(session => session.Key == sessionId).Value, sessionId,
            result?.Success ?? false);

        return result;
    }

    protected async Task<bool> Login(Guid sessionId)
    {
        var session = AllSessions.FirstOrDefault(session => session.Key == sessionId).Value;
        if (session is null)
            return false;

        Console.WriteLine(await Logging.AddLog(session.SessionId, AuditAction.Access, AuditContext.Leaf,
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

            Console.WriteLine(await Logging.AddLog(session.SessionId, AuditAction.Access, AuditContext.Leaf,
                "Authentication Attempting"));
            session = await LoginImplementation(session);

            session.LoginAuthenticationAttempting = false;

            VehicleSessions[session.SessionId] = session;
        }
        else
        {
            Console.WriteLine(await Logging.AddLog(session.SessionId, AuditAction.Access, AuditContext.Leaf,
                "Authentication Attempted - But Given Up"));

            return false;
        }

        OnAuthenticationAttempt?.Invoke(session, sessionId, session.AuthenticatedAccessToken);

        return session.Authenticated;
    }

    public async Task StartAsync()
    {
        // Queue saved sessions into memory.
        foreach (var leaf in LeafContext.NissanLeafs)
            _ = AddAsync(new CarModel(leaf.NissanUsername, leaf.NissanPassword) { CarModelId = leaf.CarModelId });

        await LeafContext.SaveChangesAsync();
    }

    private async Task<bool> AddAsync(CarModel NewCar, bool _ = false)
    {
        var session = new NissanConnectSession(NewCar.NissanUsername, NewCar.NissanPassword, NewCar.CarModelId);

        var success = false;
        try
        {
            VehicleSessions[session.SessionId] = session;
            success = await Login(session.SessionId);
        }
        catch (Exception ex)
        {
            Console.WriteLine(await Logging.AddLog(session.SessionId, AuditAction.Exception, AuditContext.Leaf,
                ex.ToString()));
        }

        return success;
    }

    public async Task<bool> AddAsync(CarModel NewCar)
    {
        var success = await AddAsync(NewCar, false);

        if (success)
            await LeafContext.SaveChangesAsync();

        return success;
    }

    protected abstract Task<VehicleSessionBase> LoginImplementation(VehicleSessionBase session);

    protected async Task<Response?> PerformAction(Guid sessionId, string? vin, string action, string type,
        JObject attributes)
    {
        Console.WriteLine(await Logging.AddLog(sessionId, AuditAction.Execute, AuditContext.Leaf,
            $"Performing action {action} on {vin}"));
        var response = await PerformActionImplementation(sessionId, vin, action, type, attributes);

        return response;
    }

    protected abstract Task<Response?> PerformActionImplementation(Guid sessionId, string? vin, string action,
        string type, JObject attributes);

    protected async Task<Response?> GetStatus(Guid sessionId, string? vin, string action)
    {
        Console.WriteLine(await Logging.AddLog(sessionId, AuditAction.Execute, AuditContext.Leaf,
            $"Getting status {action} on {vin}"));
        var response = await GetStatusImplementation(sessionId, vin, action);

        return response;
    }

    protected abstract Task<Response?> GetStatusImplementation(Guid sessionId, string? vin, string action);
}