// Copyright (c) Nathan Ford. All rights reserved. SessionManagerBase.cs

using Leaf2Google.Helpers;
using Leaf2Google.Models.Car;
using Leaf2Google.Models.Generic;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Net;

namespace Leaf2Google.Dependency
{
    public interface ICarSessionManager
    {
        Task StartAsync();

        Task<bool> AddAsync(CarModel NewLeaf);

        Dictionary<Guid, VehicleSessionBase> VehicleSessions { get; }

        IReadOnlyDictionary<Guid, VehicleSessionBase> AllSessions { get; }

        Task<PointF> VehicleLocation(Guid sessionId, string? vin);
        Task<Response?> VehicleClimate(Guid sessionId, string? vin, bool forceUpdate = true);
        Task<Response?> VehicleLock(Guid sessionId, string? vin);
        Task<Response?> VehicleBattery(Guid sessionId, string? vin);
        Task<Response?> SetVehicleClimate(Guid sessionId, string? vin, decimal targetTemp, bool active);
        Task<Response?> SetVehicleLock(Guid sessionId, string? vin, bool locked);
        Task<Response?> FlashLights(Guid sessionId, string? vin, int duration = 5);
        Task<Response?> BeepHorn(Guid sessionId, string? vin, int duration = 5);
    }

    public delegate void AuthEventHandler(object sender, Guid sessionId, string? authToken);

    public delegate void RequestEventHandler(object sender, Guid sessionId, bool requestSuccess);

    public abstract class BaseSessionManager
    {
        private readonly HttpClient _client;

        protected HttpClient Client { get => _client; }

        private readonly LeafContext _leafContext;

        protected LeafContext LeafContext { get => _leafContext; }

        private readonly LoggingManager _logging;

        protected LoggingManager Logging { get => _logging; }

        private readonly IServiceScopeFactory _serviceScopeFactory;

        private IServiceScopeFactory ServiceScopeFactory { get => _serviceScopeFactory; }

        private readonly IConfiguration _configuration;

        protected IConfiguration Configuration { get => _configuration; }

        private Dictionary<Guid, VehicleSessionBase> _vehicleSessions = new Dictionary<Guid, VehicleSessionBase>();

        public Dictionary<Guid, VehicleSessionBase> VehicleSessions { get => _vehicleSessions; }

        public IReadOnlyDictionary<Guid, VehicleSessionBase> AllSessions { get => _vehicleSessions; }


        public event RequestEventHandler OnRequest;


        public event AuthEventHandler OnAuthenticationAttempt;

        public BaseSessionManager(HttpClient client, LeafContext leafContext, LoggingManager logging, Dictionary<Guid, VehicleSessionBase> vehicleSessions, IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
        {
            _client = client;
            _leafContext = leafContext;
            _logging = logging;
            _vehicleSessions = vehicleSessions;
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;

            OnRequest += BaseSessionManager_OnRequest;
            OnAuthenticationAttempt += BaseSessionManager_OnAuthenticationAttempt;
        }

        private async void BaseSessionManager_OnAuthenticationAttempt(object sender, Guid sessionId, string? authToken)
        {
            if (sender is VehicleSessionBase session)
            {
                var scope = ServiceScopeFactory.CreateScope();
                var nissanContext = scope.ServiceProvider.GetRequiredService<LeafContext>();

                if (!session.Authenticated)
                    session.LoginFailedCount += 1;

                if (!session.Authenticated && session.LoginGivenUp)
                {
                    var leaf = await nissanContext.NissanLeafs.FirstOrDefaultAsync(car => car.CarModelId == session.SessionId);
                    if (leaf != null)
                    {
                        leaf.Deleted = DateTime.UtcNow;
                        nissanContext.Entry(leaf).State = EntityState.Modified;
                    }

                    Console.WriteLine(await Logging.AddLog(session.SessionId, AuditAction.Delete, AuditContext.Leaf, "Deleting Stale Leaf"));

                    VehicleSessions.Remove(session.SessionId);
                }
                else if (session.Authenticated)
                {
                    Console.WriteLine(await Logging.AddLog(session.SessionId, AuditAction.Access, AuditContext.Leaf, "Authentication Success"));
                }
                else
                {
                    Console.WriteLine(await Logging.AddLog(session.SessionId, AuditAction.Access, AuditContext.Leaf, "Authentication Failed"));
                    await Login(session.SessionId);
                }

                await nissanContext.SaveChangesAsync();
            }
        }

        private async void BaseSessionManager_OnRequest(object sender, Guid sessionId, bool requestSuccess)
        {
            if (sender is VehicleSessionBase session && !session.Authenticated && !session.LoginGivenUp && session.LastAuthenticated > DateTime.MinValue && !requestSuccess)
            {
                session.LastRequestSuccessful = requestSuccess;
                await Login(sessionId);
            }
        }

        protected async Task<Response?> MakeRequest(Guid sessionId, HttpRequestMessage httpRequestMessage, string baseUri = "")
        {
            bool success = true;
            Response? result = null;

            try
            {
                httpRequestMessage.RequestUri = new Uri($"{(string.IsNullOrEmpty(baseUri) ? _configuration["Nissan:EU:auth_base_url"] : baseUri)}{httpRequestMessage.RequestUri?.ToString() ?? ""}");
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

            OnRequest?.Invoke(AllSessions.FirstOrDefault(session => session.Key == sessionId).Value, sessionId, result?.Success ?? false);

            return result;
        }

        protected async Task<bool> Login(Guid sessionId) // make abstract/interface
        {
            var session = AllSessions[sessionId];

            if (!session.LoginGivenUp && !session.Authenticated)
            {
                Console.WriteLine(await Logging.AddLog(session.SessionId, AuditAction.Access, AuditContext.Leaf, "Authentication Attempting"));
                session = VehicleSessions[session.SessionId] = await LoginImplementation(session);
            }
            else
            {
                Console.WriteLine(await Logging.AddLog(session.SessionId, AuditAction.Access, AuditContext.Leaf, "Authentication Attempted - But Given Up"));
                return false;
            }

            OnAuthenticationAttempt?.Invoke(session, sessionId, session.AuthenticatedAccessToken);

            return session.Authenticated;
        }

        public async Task StartAsync()
        {
            // Queue saved sessions into memory.
            foreach (var leaf in LeafContext.NissanLeafs.Where(leaf => leaf.Deleted == null))
            {
                await AddAsync(new CarModel(leaf.NissanUsername, leaf.NissanPassword) { CarModelId = leaf.CarModelId });
            }

            await LeafContext.SaveChangesAsync();
        }

        private async Task<bool> AddAsync(CarModel NewCar, bool _ = false)
        {
            var session = new NissanConnectSession(NewCar.NissanUsername, NewCar.NissanPassword, NewCar.CarModelId);

            bool success = false;
            try
            {
                VehicleSessions[session.SessionId] = session;
                success = await Login(session.SessionId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(await Logging.AddLog(session.SessionId, AuditAction.Exception, AuditContext.Leaf, ex.ToString()));
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

        protected async Task<Response?> PerformAction(Guid sessionId, string? vin, string action, string type, JObject attributes)
        {
            Console.WriteLine(await Logging.AddLog(sessionId, AuditAction.Execute, AuditContext.Leaf, $"Performing action {action} on {vin}"));
            Response? response = await PerformActionImplementation(sessionId, vin, action, type, attributes);

            return response;
        }

        protected abstract Task<Response?> PerformActionImplementation(Guid sessionId, string? vin, string action, string type, JObject attributes);

        protected async Task<Response?> GetStatus(Guid sessionId, string? vin, string action)
        {
            Console.WriteLine(await Logging.AddLog(sessionId, AuditAction.Execute, AuditContext.Leaf, $"Getting status {action} on {vin}"));
            Response? response = await GetStatusImplementation(sessionId, vin, action);

            return response;
        }

        protected abstract Task<Response?> GetStatusImplementation(Guid sessionId, string? vin, string action);
    }
}