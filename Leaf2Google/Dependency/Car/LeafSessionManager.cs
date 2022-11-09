using System.Drawing;
using System.Net;
using System.Text;
using Leaf2Google.Models.Car;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Leaf2Google.Dependency.Car;

public class LeafSessionManager : BaseSessionManager, ICarSessionManager
{
    public LeafSessionManager(HttpClient client, LeafContext leafContext, LoggingManager logging,
        BaseStorageManager storageManager, IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration)
        : base(client, leafContext, storageManager, logging, serviceScopeFactory, configuration)
    {
    }

    public async Task<PointF> VehicleLocation(VehicleSessionBase session, string? vin)
    {
        if (DateTime.UtcNow - session.LastLocation.Item1 > TimeSpan.FromMinutes(1))
        {
            var location = await GetStatus(session, vin, "location");

            if (location != null)
            {
                session.LastLocation = Tuple.Create(DateTime.UtcNow,
                    (PointF?)new PointF((float?)location?.Data?.data?.attributes.gpsLatitude ?? 0,
                        (float?)location?.Data?.data?.attributes.gpsLongitude ?? 0));
                return session.LastLocation?.Item2 ?? new PointF(0f, 0f);
            }
        }

        return session.LastLocation?.Item2 ?? new PointF(0f, 0f);
    }

    public async Task<Response?> VehicleClimate(VehicleSessionBase session, string? vin, bool forceUpdate = true)
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
            await PerformAction(session, vin, "hvac-start", "HvacStart", new JObject
            {
                { "action", "cancel" },
                { "targetTemperature", targetTemp }
            });

        return await PerformAction(session, vin, "hvac-start", "HvacStart", new JObject
        {
            { "action", active ? "start" : "stop" },
            { "targetTemperature", targetTemp }
        });
    }

    public async Task<Response?> SetVehicleLock(VehicleSessionBase session, string? vin, bool locked)
    {
        return await PerformAction(session, vin, "lock-unlock", "LockUnlock", new JObject
        {
            { "action", locked ? "lock" : "unlock" },
            { "target", "doors_hatch" }, // 'driver_s_door' : 'doors_hatch'
            { "srp", "" /* Need to investigate SRP */ }
        });
    }

    public async Task<Response?> FlashLights(VehicleSessionBase session, string? vin, int duration = 5)
    {
        return await PerformAction(session, vin, "horn-lights", "HornLights", new JObject
        {
            { "action", "start" },
            { "duration", duration },
            { "target", "lights" }
        });
    }

    public async Task<Response?> BeepHorn(VehicleSessionBase session, string? vin, int duration = 5)
    {
        return await PerformAction(session, vin, "horn-lights", "HornLights", new JObject
        {
            { "action", "start" },
            { "duration", duration },
            { "target", "horn" }
        });
    }

    protected override async Task<Response?> PerformActionImplementation(VehicleSessionBase session, string? vin, string action,
        string type, JObject attributes)
    {
        dynamic httpRequestData = new JObject
        {
            {
                "data", new JObject
                {
                    { "type", $"{type}" },
                    { "attributes", attributes }
                }
            }
        };

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"v1/cars/{vin}/actions/{action}")
        {
            Headers =
            {
                { "Authorization", $"Bearer {session.AuthenticatedAccessToken}" },
                { "Accept", "*/*" }
            },
            Content = new StringContent(JsonConvert.SerializeObject(httpRequestData), Encoding.UTF8,
                "application/vnd.api+json")
        };

        var response = await MakeRequest(session, httpRequestMessage,
            Configuration["Nissan:EU:car_adapter_base_url"]);

        return response;
    }

    protected override async Task<Response?> GetStatusImplementation(VehicleSessionBase session, string? vin, string action)
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"v1/cars/{vin}/{action}")
        {
            Headers =
            {
                { "Authorization", $"Bearer {session.AuthenticatedAccessToken}" }
            }
        };

        var response =
            await MakeRequest(session, httpRequestMessage, Configuration["Nissan:EU:car_adapter_base_url"]);

        return response;
    }

    protected override async Task<bool> LoginImplementation(VehicleSessionBase session)
    {
        var authenticateResult = await Authenticate(session);
        Response? authenticationResult = null;

        authenticationResult =
            await Authenticate(session, session.Username, session.Password, authenticateResult);
        var authorizeResult = await Authorize(session, authenticationResult);
        var accessTokenResult = await AccessToken(session, authorizeResult);

        if (accessTokenResult?.Success == true)
        {
            session.AuthenticatedAccessToken = (string?)accessTokenResult?.Data?.access_token;

            // TODO add dropdown select on register
            var usersResult = await UsersResult(session);

            var vehiclesResult = await VehiclesResult(session, (string)usersResult!.Data.userId);

            session.VINs.AddRange(((JArray)vehiclesResult!.Data.data)
                .Select(vehicle => (string?)((JObject)vehicle)["vin"])
                .Where(vehicle => !string.IsNullOrEmpty(vehicle)));
            session.CarPictureUrl = ((JArray)vehiclesResult!.Data.data)
                .Select(vehicle => (string?)((JObject)vehicle)["pictureURL"]).FirstOrDefault();
        }
        else
        {
            session.AuthenticatedAccessToken = null;
        }

        return session.Authenticated;
    }

    private async Task<Response?> Authenticate(VehicleSessionBase session)
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post,
            $"json/realms/root/realms/{Configuration["Nissan:EU:realm"]}/authenticate")
        {
            Headers =
            {
                { "Accept-Api-Version", Configuration["Nissan:api_version"] },
                { "X-Username", "anonymous" },
                { "X-Password", "anonymous" },
                { "Accept", "application/json" }
            }
        };

        var response = await MakeRequest(session, httpRequestMessage);

        return response;
    }

    private async Task<Response?> Authenticate(VehicleSessionBase session, string username, string password,
        Response? authenticateResult)
    {
        if (authenticateResult?.Success != true)
            return null;

        // Because this data is so 'hand-crafted' I have left it as a simple JObject instead of creating a bespoke object to control this.
        dynamic httpRequestData = new JObject
        {
            { "authId", authenticateResult.Data.authId },
            { "template", string.Empty },
            { "stage", "LDAP1" },
            { "header", "Sign in" },
            {
                "callbacks", new JArray(
                    new JObject
                    {
                        { "type", "NameCallback" },
                        {
                            "output", new JArray(
                                new JObject
                                {
                                    { "name", "prompt" },
                                    { "value", "User Name:" }
                                }
                            )
                        },
                        {
                            "input", new JArray(
                                new JObject
                                {
                                    { "name", "IDToken1" },
                                    { "value", username }
                                }
                            )
                        }
                    },
                    new JObject
                    {
                        { "type", "PasswordCallback" },
                        {
                            "output", new JArray(
                                new JObject
                                {
                                    { "name", "prompt" },
                                    { "value", "Password:" }
                                }
                            )
                        },
                        {
                            "input", new JArray(
                                new JObject
                                {
                                    { "name", "IDToken2" },
                                    { "value", password }
                                }
                            )
                        }
                    })
            }
        };

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post,
            $"json/realms/root/realms/{Configuration["Nissan:EU:realm"]}/authenticate")
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

        var response = await MakeRequest(session, httpRequestMessage);

        return response;
    }

    private async Task<Response?> Authorize(VehicleSessionBase session, Response? authenticateResult)
    {
        if (authenticateResult?.Success != true)
            return null;

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get,
            $"oauth2{authenticateResult.Data.realm}/authorize?client_id={Configuration["Nissan:EU:client_id"]}&redirect_uri={Configuration["Nissan:EU:redirect_uri"]}&response_type=code&scope={Configuration["Nissan:EU:scope"]}&nonce=sdfdsfez&state=af0ifjsldkj")
        {
            Headers =
            {
                { "Cookie", $"i18next=en-UK; amlbcookie=05; kauthSession=\"{authenticateResult.Data.tokenId}\"" }
            }
        };

        var response = await MakeRequest(session, httpRequestMessage);

        if (response != null)
            response.Data = authenticateResult!.Data;

        return response;
    }

    private async Task<Response?> AccessToken(VehicleSessionBase session, Response? authenticateResult)
    {
        if (authenticateResult?.Code != (int)HttpStatusCode.Found)
            return null;

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post,
            $"oauth2{authenticateResult.Data.realm}/access_token?code={authenticateResult!.Headers.Location?.ToString().Split('=')[1].Split('&')[0]}&client_id={Configuration["Nissan:EU:client_id"]}&client_secret={Configuration["Nissan:EU:client_secret"]}&redirect_uri={Configuration["Nissan:EU:redirect_uri"]}&grant_type=authorization_code")
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

        var response = await MakeRequest(session, httpRequestMessage);

        return response;
    }

    private async Task<Response?> UsersResult(VehicleSessionBase session)
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "v1/users/current")
        {
            Headers =
            {
                { "Authorization", $"Bearer {session.AuthenticatedAccessToken}" }
            }
        };

        var response =
            await MakeRequest(session, httpRequestMessage, Configuration["Nissan:EU:user_adapter_base_url"]);

        return response;
    }

    private async Task<Response?> VehiclesResult(VehicleSessionBase session, string userId)
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"v5/users/{userId}/cars")
        {
            Headers =
            {
                { "Authorization", $"Bearer {session.AuthenticatedAccessToken}" }
            }
        };

        var response = await MakeRequest(session, httpRequestMessage, Configuration["Nissan:EU:user_base_url"]);

        return response;
    }
}