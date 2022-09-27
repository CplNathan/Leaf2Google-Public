using Castle.Core.Internal;
using Leaf2Google.Contexts;
using Leaf2Google.Dependency.Google;
using Leaf2Google.Helpers;
using Leaf2Google.Models.Car;
using Leaf2Google.Models.Generic;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Drawing;

namespace Leaf2Google.Dependency.Car
{
    public interface ILeafSessionManager
    {
        Task StartAsync();

        Task<bool> AddAsync(CarModel NewLeaf, AuthEventHandler onAuthentication);
    }

    public class LeafSessionManager : BaseSessionManager, ILeafSessionManager
    {
        public LeafSessionManager(HttpClient client, GoogleStateManager googleState, IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
            : base(client, googleState, serviceScopeFactory, configuration)
        {
        }

        public async Task<PointF> VehicleLocation(Guid sessionId, string? vin)
        {
            if (DateTime.UtcNow - VehicleSessions[sessionId].LastLocation.Item1 > TimeSpan.FromMinutes(1))
            {
                var location = await GetStatus(sessionId, vin, "location");

                if (location != null)
                {
                    VehicleSessions[sessionId].LastLocation = Tuple.Create(DateTime.UtcNow, (PointF?)new PointF((float?)location?.Data?.data?.attributes.gpsLatitude ?? 0, (float?)location?.Data?.data?.attributes.gpsLongitude ?? 0));
                    return VehicleSessions[sessionId].LastLocation?.Item2 ?? new PointF(0f, 0f);
                }
            }

            return VehicleSessions[sessionId].LastLocation?.Item2 ?? new PointF(0f, 0f);
        }

        public async Task<Response?> VehicleClimate(Guid sessionId, string? vin, bool forceUpdate = false)
        {
            var session = VehicleSessions[sessionId];

            if (forceUpdate)
                await PerformAction(sessionId, vin, "refresh-hvac-status", "RefreshHvacStatus", new JObject());

            return await GetStatus(sessionId, vin, "hvac-status");
        }

        public async Task<Response?> VehicleLock(Guid sessionId, string? vin)
        {
            return await GetStatus(sessionId, vin, "lock-status");
        }

        public async Task<Response?> VehicleBattery(Guid sessionId, string? vin)
        {
            return await GetStatus(sessionId, vin, "battery-status");
        }

        public async Task<Response?> SetVehicleClimate(Guid sessionId, string? vin, decimal targetTemp, bool active)
        {
            if (!active)
            {
                await PerformAction(sessionId, vin, "hvac-start", "HvacStart", new JObject {
                    { "action", "cancel" },
                    { "targetTemperature", targetTemp }
                });
            }

            return await PerformAction(sessionId, vin, "hvac-start", "HvacStart", new JObject {
                { "action", active ? "start" : "stop" },
                { "targetTemperature", targetTemp }
            });
        }

        public async Task<Response?> SetVehicleLock(Guid sessionId, string? vin, bool locked)
        {
            return await PerformAction(sessionId, vin, "lock-unlock", "LockUnlock", new JObject {
                { "action", locked ? "lock" : "unlock" },
                { "target", "doors_hatch" }, // 'driver_s_door' : 'doors_hatch'
                { "srp", "" /* Need to investigate SRP */ }
            });
        }

        public async Task<Response?> FlashLights(Guid sessionId, string? vin, int duration = 5)
        {
            return await PerformAction(sessionId, vin, "horn-lights", "HornLights", new JObject {
                { "action", "start" },
                { "duration", duration },
                { "target", "lights" }
            });
        }

        public async Task<Response?> BeepHorn(Guid sessionId, string? vin, int duration = 5)
        {
            return await PerformAction(sessionId, vin, "horn-lights", "HornLights", new JObject {
                { "action", "start" },
                { "duration", duration },
                { "target", "horn" }
            });
        }

        public async Task StartAsync()
        {
            // Queue saved sessions into memory.
            foreach (var leaf in _leafContext.NissanLeafs.Where(leaf => leaf.Deleted == null))
            {
                var session = new NissanConnectSession(leaf.NissanUsername, leaf.NissanPassword, leaf.CarModelId)
                {
                };
                session.OnRequest += Session_OnRequest;
                session.OnAuthenticationAttempt += Session_OnAuthenticationAttempt;

                Console.WriteLine("Authenticating");
                try
                {
                    await Login(session);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        private async void Session_OnAuthenticationAttempt(object sender, string? authToken)
        {
            var session = sender as VehicleSessionBase;

            if (session != null)
            {
                using var serviceScope = _serviceScopeFactory.CreateScope();
                var leafContext = serviceScope.ServiceProvider.GetRequiredService<LeafContext>();

                Func<CarModel, bool> authenticationPredicate = leaf =>
                {
                    return leaf.CarModelId == session.SessionId;
                };

                if (!session.LoginGivenUp && !session.Authenticated && authToken.IsNullOrEmpty())
                    session = await Login(session);

                if (!session.Authenticated && session.LoginGivenUp)
                {
                    var leaf = leafContext.NissanLeafs.FirstOrDefault(authenticationPredicate);
                    if (leaf != null)
                    {
                        leaf.Deleted = DateTime.UtcNow;
                        leafContext.Entry(leaf).State = EntityState.Modified;
                    }

                    leafContext.NissanAudits.Add(new AuditModel<CarModel>
                    {
                        Owner = leaf,
                        Action = AuditAction.Delete,
                        Context = AuditContext.Account,
                        Data = $"{session.Username} - Deleting Stale Leaf",
                    });
                }
                else if (!session.Authenticated)
                {
                    Console.WriteLine($"{session.Username} - Authentication Failed");
                    leafContext.NissanAudits.Add(new AuditModel<CarModel>
                    {
                        Owner = leafContext.NissanLeafs.FirstOrDefault(authenticationPredicate),
                        Action = AuditAction.Access,
                        Context = AuditContext.Account,
                        Data = $"{session.Username} - Authentication Failed",
                    });
                }
                else if (session.Authenticated)
                {
                    Console.WriteLine($"{session.Username} - Authentication Success");
                    leafContext.NissanAudits.Add(new AuditModel<CarModel>
                    {
                        Owner = leafContext.NissanLeafs.FirstOrDefault(authenticationPredicate),
                        Action = AuditAction.Access,
                        Context = AuditContext.Account,
                        Data = $"{session.Username} - Authentication Success",
                    });

                    if (!leafContext.NissanLeafs.Any(authenticationPredicate))
                    {
                        leafContext.NissanLeafs.Add(new CarModel(session.Username, session.Password));
                    }
                }

                await leafContext.SaveChangesAsync();
            }
        }

        private async void Session_OnRequest(object sender, bool requestSuccess)
        {
            var session = sender as VehicleSessionBase;

            // If we think we are logged in and we have a failed request, we have probably timed out. Reauthenticate and if that fails re-authentication will occur elsewhere.
            if (session != null && !requestSuccess && !session.LoginGivenUp && !session.Authenticated)
            {
                await Login(session);
            }
        }

        public async Task<bool> AddAsync(CarModel NewCar, AuthEventHandler onAuthentication)
        {
            var session = new NissanConnectSession(NewCar.NissanUsername, NewCar.NissanPassword, NewCar.CarModelId);
            session.OnRequest += Session_OnRequest;
            session.OnAuthenticationAttempt += Session_OnAuthenticationAttempt;
            session.OnAuthenticationAttempt += onAuthentication;

            bool success = false;
            Console.WriteLine("Authenticating");
            try
            {
                success = await Login(session) != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            await _leafContext.SaveChangesAsync();

            return success;
        }
    }
}