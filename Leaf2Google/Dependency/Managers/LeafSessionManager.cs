using Leaf2Google.Contexts;
using Leaf2Google.Helpers;
using Leaf2Google.Models.Generic;
using Leaf2Google.Models.Car;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace Leaf2Google.Dependency.Managers
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

        public async Task<PointF> VehicleLocation(VehicleSessionBase session, string? vin)
        {
            if (DateTime.UtcNow - session.LastLocation.Item1 > TimeSpan.FromMinutes(1))
            {
                var location = await GetStatus(session, vin, "location");
                return session.LastLocation?.Item2 ?? new PointF((float)location.Data?.data.attributes.gpsLatitude, (float)location.Data?.data.attributes.gpsLongitude);
            }
            else
            {
                return session.LastLocation?.Item2 ?? new PointF(0f, 0f);
            }
        }

        public async Task<Response?> VehicleClimate(VehicleSessionBase session, string? vin, bool forceUpdate = false)
        {
            if (forceUpdate)
                await PerformAction(session, vin, "refresh-hvac-status", "RefreshHvacStatus", new JObject());

            return await GetStatus(session, vin, "hvac-status");
        }

        public async Task<Response?> VehicleLock(VehicleSessionBase session, string? vin)
        {
            return await GetStatus(session, vin, "lock-status");
        }

        public async Task<Response?> VehicleBattery(VehicleSessionBase session, string? vin)
        {
            return await GetStatus(session, vin, "battery-status");
        }

        public async Task<Response?> SetVehicleClimate(VehicleSessionBase session, string? vin, decimal targetTemp, bool active)
        {
            if (!active)
            {
                await PerformAction(session, vin, "hvac-start", "HvacStart", new JObject {
                    { "action", "cancel" },
                    { "targetTemperature", targetTemp }
                });
            }

            return await PerformAction(session, vin, "hvac-start", "HvacStart", new JObject {
                { "action", active ? "start" : "stop" },
                { "targetTemperature", targetTemp }
            });
        }

        public async Task<Response?> SetVehicleLock(VehicleSessionBase session, string? vin, bool locked)
        {
            return await PerformAction(session, vin, "lock-unlock", "LockUnlock", new JObject {
                { "action", locked ? "lock" : "unlock" },
                { "target", "doors_hatch" }, // 'driver_s_door' : 'doors_hatch'
                { "srp", "" /* Need to investigate SRP */ }
            });
        }

        public async Task<Response?> FlashLights(VehicleSessionBase session, string? vin, int duration = 5)
        {
            return await PerformAction(session, vin, "horn-lights", "HornLights", new JObject {
                { "action", "start" },
                { "duration", duration },
                { "target", "lights" }
            });
        }

        public async Task<Response?> BeepHorn(VehicleSessionBase session, string? vin, int duration = 5)
        {
            return await PerformAction(session, vin, "horn-lights", "HornLights", new JObject {
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

        private async void Session_OnAuthenticationAttempt(object sender, string authToken)
        {
            var session = sender as VehicleSessionBase;

            if (session != null)
            {
                using var serviceScope = _serviceScopeFactory.CreateScope();
                var leafContext = serviceScope.ServiceProvider.GetRequiredService<LeafContext>();
                var googleState = serviceScope.ServiceProvider.GetRequiredService<GoogleStateManager>();

                Func<CarModel, bool> authenticationPredicate = leaf =>
                {
                    return leaf.CarModelId == session.SessionId;
                };

                if (!session.LoginGivenUp && !session.Authenticated)
                    await Login(session);

                if (!session.Authenticated && session.LoginGivenUp)
                {
                    VehicleSessions.Remove(session);

                    var leaf = leafContext.NissanLeafs.FirstOrDefault(authenticationPredicate);
                    if (leaf != null)
                    {
                        leaf.Deleted = DateTime.UtcNow;
                        leafContext.Entry(leaf).State = EntityState.Modified;
                    }

                    leafContext.NissanAudits.Add(new Audit<CarModel>
                    {
                        Owner = leaf,
                        Action = AuditAction.Delete,
                        Context = AuditContext.Account,
                        Data = $"{session.Username} - Deleting Stale Leaf",
                    });

                    Console.WriteLine($"{session.Username} - Authentication Failed");
                    leafContext.NissanAudits.Add(new Audit<CarModel>
                    {
                        Owner = leafContext.NissanLeafs.FirstOrDefault(authenticationPredicate),
                        Action = AuditAction.Access,
                        Context = AuditContext.Account,
                        Data = $"{session.Username} - Authentication Failed",
                    });
                }
                else if (session.Authenticated)
                {
                    googleState.GetOrCreateDevices(session.SessionId);

                    Console.WriteLine($"{session.Username} - Authentication Success");
                    leafContext.NissanAudits.Add(new Audit<CarModel>
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

        private void Session_OnRequest(object sender, bool requestSuccess)
        {
            //throw new NotImplementedException();
        }

        public async Task<bool> AddAsync(CarModel NewCar, AuthEventHandler onAuthentication)
        {
            var session = new NissanConnectSession(NewCar.NissanUsername, NewCar.NissanPassword, NewCar.CarModelId);
            session.OnRequest += Session_OnRequest;
            session.OnAuthenticationAttempt += Session_OnAuthenticationAttempt;
            session.OnAuthenticationAttempt += onAuthentication;
            session.tcs = new TaskCompletionSource<bool>();

            bool success = false;
            Console.WriteLine("Authenticating");
            try
            {
                success = await Login(session);
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