using Leaf2Google.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Net;
using System.Text;

namespace Leaf2Google.Models.Nissan
{
    // This shouldn't really be a 'model', TODO: Make this its own controller.

    public class NissanConnectSessionConfiguration
    {
        // TODO: Make this a distinct controller, dependency inject IConfiguration and get settings from configuration file.
        public static Dictionary<string, Dictionary<string, string>> Settings { get; set; } = new Dictionary<string, Dictionary<string, string>>()
        {
            {
                "EU",
                new Dictionary<string, string>()
                {
                    { "client_id", "a-ncb-prod-android" }, // CLIENT_ID_V2_EU_PROD_NEW
                    { "client_secret", "0sAcrtwvwEXXZp5nzQhPexSRhxUVKa0d76F4uqDvxvvKFHXpo4myoJwUuV4vuNqC" }, // CLIENT_SECRET_V2_EU_PROD_NEW
                    { "scope", "openid profile vehicles" }, // API_SCOPE_V2_EU_PROD_NEW
                    { "auth_base_url", "https://prod.eu2.auth.kamereon.org/kauth/" }, // OAUTH_AUTHORIZATION_BASE_URL_V2_EU_PROD_NEW
                    { "realm", "a-ncb-prod" }, // OAUTH_REALM_DEFAULT_V2_EU_PROD_NEW CLIENT_ID_V2_EU_PROD_NEW
                    { "redirect_uri", "org.kamereon.service.nci:/oauth2redirect" },
                    { "car_adapter_base_url", "https://alliance-platform-caradapter-prod.apps.eu2.kamereon.io/car-adapter/" }, // carAdapter_eu_prod
                    { "user_adapter_base_url", "https://alliance-platform-usersadapter-prod.apps.eu2.kamereon.io/user-adapter/" }, // userAdapter_eu_prod
                    { "user_base_url", "https://nci-bff-web-prod.apps.eu.kamereon.io/bff-web/" } // bffWeb_eu_prod
                } // TODO Config
            }
        };

        public const string APIVersion = "protocol=1.0,resource=2.1";
        public const string SRPKey = "D5AF0E14718E662D12DBB4FE42304DF5A8E48359E22261138B40AA16CC85C76A11B43200A1EECB3C9546A262D1FBD51ACE6FCDE558C00665BBF93FF86B9F8F76AA7A53CA74F5B4DFF9A4B847295E7D82450A2078B5A28814A7A07F8BBDD34F8EEB42B0E70499087A242AA2C5BA9513C8F9D35A81B33A121EEF0A71F3F9071CCD";

        private readonly HttpClient _client;

        protected HttpClient Client { get => _client; }

        private string _authenticatedAccessToken = string.Empty;

        public string AuthenticatedAccessToken { get => _authenticatedAccessToken; protected set => _authenticatedAccessToken = value; }

        public string Username { get; init; }
        public string Password { get; init; }
        public Guid SessionId { get; init; }

        public List<string?> VINs { get; protected set; } = new List<string?>();

        public bool Authenticated { get; protected set; }
        public bool LastRequestSuccessful { get; protected set; }

        public int LoginFailedCount { get; protected set; }

        public NissanConnectSessionConfiguration(HttpClient client, string username, string password, Guid sessionId)
        {
            _client = client;
            this.Username = username;
            this.Password = password;
            this.SessionId = sessionId;
        }
    }

    public class NissanConnectSessionV1 : NissanConnectSessionConfiguration
    {
        public NissanConnectSessionV1(HttpClient client, string username, string password, Guid sessionId)
            : base(client, username, password, sessionId)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
        }

        private async Task<Response?> MakeRequest(HttpRequestMessage httpRequestMessage, string baseUri = "")
        {
            try
            {
                httpRequestMessage.RequestUri = new Uri($"{(string.IsNullOrEmpty(baseUri) ? NissanConnectSessionConfiguration.Settings["EU"]["auth_base_url"] : baseUri)}{httpRequestMessage.RequestUri?.ToString() ?? ""}");
                httpRequestMessage.Headers.Add("User-Agent", "NissanConnect/2 CFNetwork/978.0.7 Darwin/18.7.0");

                var result = await Client.MakeRequest(httpRequestMessage);
                return result;
            }
            catch (Exception ex)
            {
                ex.GetHashCode();

                if (ex.Source != "Newtonsoft.Json")
                    LastRequestSuccessful = false;

                return null;
            }
        }

        private async Task<Response?> Authenticate()
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"json/realms/root/realms/{NissanConnectSessionConfiguration.Settings["EU"]["realm"]}/authenticate")
            {
                Headers =
                {
                    { "Accept-Api-Version", NissanConnectSessionConfiguration.APIVersion },
                    { "X-Username", "anonymous" },
                    { "X-Password", "anonymous" },
                    { "Accept", "application/json" }
                }
            };

            return await MakeRequest(httpRequestMessage);
        }

        private async Task<Response?> Authenticate(string username, string password, Response? authenticateResult)
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

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"json/realms/root/realms/{NissanConnectSessionConfiguration.Settings["EU"]["realm"]}/authenticate")
            {
                Headers =
                {
                    { "Accept-Api-Version", NissanConnectSessionConfiguration.APIVersion },
                    { "X-Username", "anonymous" },
                    { "X-Password", "anonymous" },
                    { "Accept", "application/json" }
                },
                Content = new StringContent(JsonConvert.SerializeObject(httpRequestData), UnicodeEncoding.UTF8, "application/json")
            };

            var response = await MakeRequest(httpRequestMessage);

            return response;
        }

        private async Task<Response?> Authorize(Response? authenticateResult)
        {
            if (authenticateResult?.Success != true)
                return null;

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"oauth2{authenticateResult.Data.realm}/authorize?client_id={NissanConnectSessionConfiguration.Settings["EU"]["client_id"]}&redirect_uri={NissanConnectSessionConfiguration.Settings["EU"]["redirect_uri"]}&response_type=code&scope={NissanConnectSessionConfiguration.Settings["EU"]["scope"]}&nonce=sdfdsfez")
            {
                Headers =
                {
                    { "Cookie", $"i18next=en-UK; amlbcookie=05; kauthSession=\"{authenticateResult.Data.tokenId}\"" }
                }
            };

            var response = await MakeRequest(httpRequestMessage);

            if (response != null)
                response.Data = authenticateResult!.Data;

            return response;
        }

        private async Task<Response?> AccessToken(Response? authenticateResult)
        {
            if (authenticateResult?.Code != 302)
                return null;

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"oauth2{authenticateResult.Data.realm}/access_token?code={authenticateResult!.Headers.Location?.ToString().Split('=')[1].Split('&')[0]}&client_id={NissanConnectSessionConfiguration.Settings["EU"]["client_id"]}&client_secret={NissanConnectSessionConfiguration.Settings["EU"]["client_secret"]}&redirect_uri={NissanConnectSessionConfiguration.Settings["EU"]["redirect_uri"]}&grant_type=authorization_code")
            {
                Headers =
                {
                    { "Accept-Api-Version", NissanConnectSessionConfiguration.APIVersion },
                    { "X-Username", "anonymous" },
                    { "X-Password", "anonymous" },
                    { "Accept", "application/json" }
                },
                Content = new StringContent(String.Empty, UnicodeEncoding.UTF8, "application/x-www-form-urlencoded")
            };

            var response = await MakeRequest(httpRequestMessage);

            return response;
        }

        private async Task<Response?> UsersResult()
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"v1/users/current")
            {
                Headers =
                {
                    { "Authorization", $"Bearer {AuthenticatedAccessToken}" }
                }
            };

            var response = await MakeRequest(httpRequestMessage, NissanConnectSessionConfiguration.Settings["EU"]["user_adapter_base_url"]);

            return response;
        }

        private async Task<Response?> VehiclesResult(string userId)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"v4/users/{userId}/cars")
            {
                Headers =
                {
                    { "Authorization", $"Bearer {AuthenticatedAccessToken}" }
                }
            };

            var response = await MakeRequest(httpRequestMessage, NissanConnectSessionConfiguration.Settings["EU"]["user_base_url"]);

            return response;
        }

        public async Task<bool> Login(bool resetLogin = false)
        {
            LoginFailedCount = resetLogin ? 0 : LoginFailedCount;
            Authenticated = false;

            var authenticateResult = await Authenticate();
            Response? authenticationResult = null;

            authenticationResult = await Authenticate(Username, Password, authenticateResult);
            var authorizeResult = await Authorize(authenticationResult);
            var accessTokenResult = await AccessToken(authorizeResult);

            if (accessTokenResult?.Success == true)
            {
                AuthenticatedAccessToken = (string)accessTokenResult.Data.access_token;

                // TODO add dropdown select on register
                var usersResult = await UsersResult();
                if (usersResult?.Success == true)
                {
                    Authenticated = true;
                    LoginFailedCount = 0;

                    var vehiclesResult = await VehiclesResult((string)usersResult!.Data.userId);

                    VINs = ((JArray)vehiclesResult!.Data.data).Select(vehicle => (string?)((JObject)vehicle)["vin"]).Where(vehicle => !string.IsNullOrEmpty(vehicle)).ToList();
                }
                else
                {
                    LoginFailedCount++;
                }
            }
            else
            {
                LoginFailedCount++;
            }

            return Authenticated;
        }

        protected async Task<Response?> PerformAction(string vin, string action, string type, JObject attributes)
        {
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
                    { "Authorization", $"Bearer {AuthenticatedAccessToken}" },
                    { "Accept", "*/*" }
                },
                Content = new StringContent(JsonConvert.SerializeObject(httpRequestData), UnicodeEncoding.UTF8, "application/vnd.api+json")
            };

            var response = await MakeRequest(httpRequestMessage, NissanConnectSessionConfiguration.Settings["EU"]["car_adapter_base_url"]);

            if ((response == null || response.Success == false) && LoginFailedCount < 5)
                await Login(/* true */);

            return response;
        }

        protected async Task<Response?> GetStatus(string vin, string action)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"v1/cars/{vin}/{action}")
            {
                Headers =
                {
                    { "Authorization", $"Bearer {AuthenticatedAccessToken}" }
                }
            };

            var response = await MakeRequest(httpRequestMessage, NissanConnectSessionConfiguration.Settings["EU"]["car_adapter_base_url"]);

            if ((response == null || response.Success == false) && LoginFailedCount < 5)
                await Login(/* true */);

            return response;
        }
    }

    public class NissanConnectSession : NissanConnectSessionV1
    {
        private readonly string _primaryVin;

        public string PrimaryVin { get => _primaryVin; }

        public Tuple<DateTime, PointF?> LastLocation { get; set; } = Tuple.Create<DateTime, PointF?>(DateTime.MinValue, null);

        public NissanConnectSession(HttpClient client, string username, string password, Guid sessionId, string primaryVin = "")
            : base(client, username, password, sessionId)
        {
            _primaryVin = primaryVin;
        }

        public async Task<PointF> VehicleLocation(string vin)
        {
            if (DateTime.UtcNow - LastLocation.Item1 > TimeSpan.FromMinutes(1))
            {
                var location = await GetStatus(vin, "location");
                return LastLocation.Item2 ?? new PointF((float)location.Data?.data.attributes.gpsLatitude, (float)location.Data?.data.attributes.gpsLongitude);
            }
            else
            {
                return LastLocation.Item2 ?? new PointF(0f, 0f);
            }
        }

        public async Task<Response?> VehicleClimate(string vin, bool forceUpdate = false)
        {
            if (forceUpdate)
                await PerformAction(vin, "refresh-hvac-status", "RefreshHvacStatus", new JObject());

            return await GetStatus(vin, "hvac-status");
        }

        public async Task<Response?> VehicleLock(string vin)
        {
            return await GetStatus(vin, "lock-status");
        }

        public async Task<Response?> VehicleBattery(string vin)
        {
            return await GetStatus(vin, "battery-status");
        }

        public async Task<Response?> SetVehicleClimate(string vin, decimal targetTemp, bool active)
        {
            if (!active)
            {
                await PerformAction(vin, "hvac-start", "HvacStart", new JObject {
                    { "action", "cancel" },
                    { "targetTemperature", targetTemp }
                });
            }

            return await PerformAction(vin, "hvac-start", "HvacStart", new JObject {
                { "action", active ? "start" : "stop" },
                { "targetTemperature", targetTemp }
            });
        }

        public async Task<Response?> SetVehicleLock(string vin, bool locked)
        {
            return await PerformAction(vin, "lock-unlock", "LockUnlock", new JObject {
                { "action", locked ? "lock" : "unlock" },
                { "target", "doors_hatch" }, // 'driver_s_door' : 'doors_hatch'
                { "srp", "" /* Need to investigate SRP */ }
            });
        }

        public async Task<Response?> FlashLights(string vin, int duration = 5)
        {
            return await PerformAction(vin, "horn-lights", "HornLights", new JObject {
                { "action", "start" },
                { "duration", duration },
                { "target", "lights" }
            });
        }

        public async Task<Response?> BeepHorn(string vin, int duration = 5)
        {
            return await PerformAction(vin, "horn-lights", "HornLights", new JObject {
                { "action", "start" },
                { "duration", duration },
                { "target", "horn" }
            });
        }
    }
}