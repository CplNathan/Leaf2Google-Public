// Copyright (c) Nathan Ford. All rights reserved. SessionManagerBase.cs

using Leaf2Google.Contexts;
using Leaf2Google.Helpers;
using Leaf2Google.Models.Car;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUglify.Helpers;
using System.Drawing;
using System.Net;
using System.Text;

namespace Leaf2Google.Dependency.Managers
{
    public class BaseSessionManager : IDisposable
    {
        protected readonly HttpClient _client;

        protected HttpClient Client { get => _client; }

        protected readonly GoogleStateManager _googleState;

        protected readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IServiceScope _serviceScope;
        protected readonly LeafContext _leafContext;

        protected readonly IConfiguration _configuration;

        private Dictionary<Guid, VehicleSessionBase> _vehicleSessions = new Dictionary<Guid, VehicleSessionBase>();

        private Dictionary<Guid, VehicleSessionBase> _pendingSessions = new Dictionary<Guid, VehicleSessionBase>();

        public Dictionary<Guid, VehicleSessionBase> VehicleSessions { get => _vehicleSessions; }

        public IReadOnlyDictionary<Guid, VehicleSessionBase> AllSessions {
            get
            {
                var allSessions = new Dictionary<Guid, VehicleSessionBase>();

                _vehicleSessions.ForEach(x => allSessions[x.Key] = x.Value);
                _pendingSessions.ForEach(x => allSessions[x.Key] = x.Value);

                return allSessions;
            }
        }

        protected Timer? _timer;

        public BaseSessionManager(HttpClient client, GoogleStateManager googleState, IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
        {
            _client = client;
            _configuration = configuration;
            _googleState = googleState;
            _serviceScopeFactory = serviceScopeFactory;
            _serviceScope = _serviceScopeFactory.CreateScope();
            _leafContext = _serviceScope.ServiceProvider.GetRequiredService<LeafContext>();
        }

        private async Task<Response?> MakeRequest(Guid sessionId, HttpRequestMessage httpRequestMessage, string baseUri = "")
        {
            bool success = true;
            Response? result = null;

            try
            {
                httpRequestMessage.RequestUri = new Uri($"{(string.IsNullOrEmpty(baseUri) ? _configuration["Nissan:EU:auth_base_url"] : baseUri)}{httpRequestMessage.RequestUri?.ToString() ?? ""}");
                httpRequestMessage.Headers.Add("User-Agent", "NissanConnect/2 CFNetwork/978.0.7 Darwin/18.7.0");

                result = await _client.MakeRequest(httpRequestMessage);
            }
            catch (Exception ex)
            {
                ex.GetHashCode();

                if (ex.Source != "Newtonsoft.Json")
                    success = false;
            }

            AllSessions[sessionId]?.Invoke_OnRequest(success);
            return result;
        }

        private async Task<Response?> Authenticate(Guid sessionId)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"json/realms/root/realms/{_configuration["Nissan:EU:realm"]}/authenticate")
            {
                Headers =
                {
                    { "Accept-Api-Version", _configuration["Nissan:api_version"] },
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

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"json/realms/root/realms/{_configuration["Nissan:EU:realm"]}/authenticate")
            {
                Headers =
                {
                    { "Accept-Api-Version", _configuration["Nissan:api_version"] },
                    { "X-Username", "anonymous" },
                    { "X-Password", "anonymous" },
                    { "Accept", "application/json" }
                },
                Content = new StringContent(JsonConvert.SerializeObject(httpRequestData), UnicodeEncoding.UTF8, "application/json")
            };

            var response = await MakeRequest(sessionId, httpRequestMessage);

            return response;
        }

        private async Task<Response?> Authorize(Guid sessionId, Response? authenticateResult)
        {
            if (authenticateResult?.Success != true)
                return null;

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"oauth2{authenticateResult.Data.realm}/authorize?client_id={_configuration["Nissan:EU:client_id"]}&redirect_uri={_configuration["Nissan:EU:redirect_uri"]}&response_type=code&scope={_configuration["Nissan:EU:scope"]}&nonce=sdfdsfez")
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
            if (authenticateResult?.Code != 302)
                return null;

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"oauth2{authenticateResult.Data.realm}/access_token?code={authenticateResult!.Headers.Location?.ToString().Split('=')[1].Split('&')[0]}&client_id={_configuration["Nissan:EU:client_id"]}&client_secret={_configuration["Nissan:EU:client_secret"]}&redirect_uri={_configuration["Nissan:EU:redirect_uri"]}&grant_type=authorization_code")
            {
                Headers =
                {
                    { "Accept-Api-Version", _configuration["Nissan:api_version"] },
                    { "X-Username", "anonymous" },
                    { "X-Password", "anonymous" },
                    { "Accept", "application/json" }
                },
                Content = new StringContent(String.Empty, UnicodeEncoding.UTF8, "application/x-www-form-urlencoded")
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

            var response = await MakeRequest(sessionId, httpRequestMessage, _configuration["Nissan:EU:user_adapter_base_url"]);

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

            var response = await MakeRequest(sessionId, httpRequestMessage, _configuration["Nissan:EU:user_base_url"]);

            return response;
        }

        protected async Task<VehicleSessionBase> Login(VehicleSessionBase session) // make abstract/interface
        {
            _pendingSessions[session.SessionId] = session;
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
            }

            _vehicleSessions[session.SessionId] = session;
            _googleState.GetOrCreateDevices(session.SessionId);

            if (!session.Authenticated)
            {
                _vehicleSessions.Remove(session.SessionId);
            }

            _pendingSessions.Remove(session.SessionId);
            return session;
        }

        protected async Task<Response?> PerformAction(Guid sessionId, string? vin, string action, string type, JObject attributes)
        {
            var session = AllSessions[sessionId];

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
                Content = new StringContent(JsonConvert.SerializeObject(httpRequestData), UnicodeEncoding.UTF8, "application/vnd.api+json")
            };

            var response = await MakeRequest(session.SessionId, httpRequestMessage, _configuration["Nissan:EU:car_adapter_base_url"]);

            return response;
        }

        protected async Task<Response?> GetStatus(Guid sessionId, string? vin, string action)
        {
            var session = AllSessions[sessionId];

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"v1/cars/{vin}/{action}")
            {
                Headers =
                {
                    { "Authorization", $"Bearer {session.AuthenticatedAccessToken}" }
                }
            };

            var response = await MakeRequest(sessionId, httpRequestMessage, _configuration["Nissan:EU:car_adapter_base_url"]);

            return response;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _serviceScope.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
