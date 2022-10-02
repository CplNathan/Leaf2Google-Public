using Castle.Core.Internal;
using Leaf2Google.Contexts;
using Leaf2Google.Dependency.Google;
using Leaf2Google.Helpers;
using Leaf2Google.Models.Car;
using Leaf2Google.Models.Generic;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Net;
using System.Text;

namespace Leaf2Google.Dependency.Car
{
    public class LeafSessionManager : BaseSessionManager, ICarSessionManager
    {
        public LeafSessionManager(HttpClient client, LeafContext leafContext, LoggingManager logging, Dictionary<Guid, VehicleSessionBase> vehicleSessions, IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
            : base(client, leafContext, logging, vehicleSessions, serviceScopeFactory, configuration)
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

        public async Task<Response?> VehicleClimate(Guid sessionId, string? vin, bool forceUpdate = true)
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

        protected override async Task<Response?> PerformActionImplementation(Guid sessionId, string? vin, string action, string type, JObject attributes)
        {
            var session = VehicleSessions[sessionId];

            dynamic httpRequestData = new JObject {
                { "data", new JObject {
                    { "type", $"{type}" },
                    { "attributes", attributes}
                }}
            };

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"v1/cars/{vin}/actions/{action}")
            {
                Headers =
                {
                    { "Authorization", $"Bearer {session.AuthenticatedAccessToken}" },
                    { "Accept", "*/*" }
                },
                Content = new StringContent(JsonConvert.SerializeObject(httpRequestData), Encoding.UTF8, "application/vnd.api+json")
            };

            var response = await MakeRequest(session.SessionId, httpRequestMessage, Configuration["Nissan:EU:car_adapter_base_url"]);

            return response;
        }

        protected override async Task<Response?> GetStatusImplementation(Guid sessionId, string? vin, string action)
        {
            var session = VehicleSessions[sessionId];

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"v1/cars/{vin}/{action}")
            {
                Headers =
                {
                    { "Authorization", $"Bearer {session.AuthenticatedAccessToken}" }
                }
            };

            var response = await MakeRequest(sessionId, httpRequestMessage, Configuration["Nissan:EU:car_adapter_base_url"]);

            return response;
        }

        protected async override Task<VehicleSessionBase> LoginImplementation(VehicleSessionBase session)
        {
            var authenticateResult = await Authenticate(session.SessionId);
            Response? authenticationResult = null;

            authenticationResult = await Authenticate(session.SessionId, session.Username, session.Password, authenticateResult);
            var authorizeResult = await Authorize(session.SessionId, authenticationResult);
            var accessTokenResult = await AccessToken(session.SessionId, authorizeResult);

            if (accessTokenResult?.Success == true)
            {
                session.AuthenticatedAccessToken = (string?)accessTokenResult?.Data?.access_token;

                // TODO add dropdown select on register
                var usersResult = await UsersResult(session.SessionId);

                var vehiclesResult = await VehiclesResult(session.SessionId, (string)usersResult!.Data.userId);

                session.VINs.AddRange(((JArray)vehiclesResult!.Data.data).Select(vehicle => (string?)((JObject)vehicle)["vin"]).Where(vehicle => !string.IsNullOrEmpty(vehicle)));
                session.CarPictureUrl = (((JArray)vehiclesResult!.Data.data).Select(vehicle => (string?)((JObject)vehicle)["pictureURL"]).FirstOrDefault());
            }
            else
            {
                session.AuthenticatedAccessToken = null;
            }

            return session;
        }

        private async Task<Response?> Authenticate(Guid sessionId)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"json/realms/root/realms/{Configuration["Nissan:EU:realm"]}/authenticate")
            {
                Headers =
                {
                    { "Accept-Api-Version", Configuration["Nissan:api_version"] },
                    { "X-Username", "anonymous" },
                    { "X-Password", "anonymous" },
                    { "Accept", "application/json" }
                }
            };

            return await MakeRequest(sessionId, httpRequestMessage);
        }

        private async Task<Response?> Authenticate(Guid sessionId, string username, string password, Response? authenticateResult)
        {
            if (authenticateResult?.Success != true)
                return null;

            // Because this data is so 'hand-crafted' I have left it as a simple JObject instead of creating a bespoke object to control this.
            dynamic httpRequestData = new JObject {
                { "authId", authenticateResult.Data.authId },
                { "template", string.Empty },
                { "stage", "LDAP1" },
                { "header", "Sign in" },
                { "callbacks", new JArray(
                    new JObject
                    {
                        { "type", "NameCallback" },
                        { "output", new JArray (
                            new JObject {
                                { "name", "prompt" },
                                { "value", "User Name:" }
                            }
                        )},
                        { "input", new JArray (
                            new JObject {
                                { "name", "IDToken1" },
                                { "value", username }
                            }
                        )},
                    },
                    new JObject
                    {
                        { "type", "PasswordCallback" },
                        { "output", new JArray (
                            new JObject {
                                { "name", "prompt" },
                                { "value", "Password:" }
                            }
                        )},
                        { "input", new JArray (
                            new JObject {
                                { "name", "IDToken2" },
                                { "value", password }
                            }
                        )},
                    })
                }
            };

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"json/realms/root/realms/{Configuration["Nissan:EU:realm"]}/authenticate")
            {
                Headers =
                {
                    { "Accept-Api-Version", Configuration["Nissan:api_version"] },
                    { "X-Username", "anonymous" },
                    { "X-Password", "anonymous" },
                    { "Accept", "application/json" }
                },
                Content = new StringContent(JsonConvert.SerializeObject(httpRequestData), Encoding.UTF8, "application/json")
            };

            var response = await MakeRequest(sessionId, httpRequestMessage);

            return response;
        }

        private async Task<Response?> Authorize(Guid sessionId, Response? authenticateResult)
        {
            if (authenticateResult?.Success != true)
                return null;

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"oauth2{authenticateResult.Data.realm}/authorize?client_id={Configuration["Nissan:EU:client_id"]}&redirect_uri={Configuration["Nissan:EU:redirect_uri"]}&response_type=code&scope={Configuration["Nissan:EU:scope"]}&nonce=sdfdsfez")
            {
                Headers =
                {
                    { "Cookie", $"i18next=en-UK; amlbcookie=05; kauthSession=\"{authenticateResult.Data.tokenId}\"" }
                }
            };

            var response = await MakeRequest(sessionId, httpRequestMessage);

            if (response != null)
                response.Data = authenticateResult!.Data;

            return response;
        }

        private async Task<Response?> AccessToken(Guid sessionId, Response? authenticateResult)
        {
            if (authenticateResult?.Code != (int)HttpStatusCode.Found)
                return null;

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"oauth2{authenticateResult.Data.realm}/access_token?code={authenticateResult!.Headers.Location?.ToString().Split('=')[1].Split('&')[0]}&client_id={Configuration["Nissan:EU:client_id"]}&client_secret={Configuration["Nissan:EU:client_secret"]}&redirect_uri={Configuration["Nissan:EU:redirect_uri"]}&grant_type=authorization_code")
            {
                Headers =
                {
                    { "Accept-Api-Version", Configuration["Nissan:api_version"] },
                    { "X-Username", "anonymous" },
                    { "X-Password", "anonymous" },
                    { "Accept", "application/json" }
                },
                Content = new StringContent(string.Empty, Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            var response = await MakeRequest(sessionId, httpRequestMessage);

            return response;
        }

        private async Task<Response?> UsersResult(Guid sessionId)
        {
            var session = AllSessions[sessionId];

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"v1/users/current")
            {
                Headers =
                {
                    { "Authorization", $"Bearer {session.AuthenticatedAccessToken}" }
                }
            };

            var response = await MakeRequest(sessionId, httpRequestMessage, Configuration["Nissan:EU:user_adapter_base_url"]);

            return response;
        }

        private async Task<Response?> VehiclesResult(Guid sessionId, string userId)
        {
            var session = AllSessions[sessionId];

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"v4/users/{userId}/cars")
            {
                Headers =
                {
                    { "Authorization", $"Bearer {session.AuthenticatedAccessToken}" }
                }
            };

            var response = await MakeRequest(sessionId, httpRequestMessage, Configuration["Nissan:EU:user_base_url"]);

            return response;
        }
    }
}