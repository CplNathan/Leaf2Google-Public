// Copyright (c) Nathan Ford. All rights reserved. LeafSessionService.cs

using Leaf2Google.Models.Car.Sessions;
using Leaf2Google.Models.Json.Nissan;
using System.Drawing;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Leaf2Google.Services.Car;

public class LeafSessionService : BaseSessionService, ICarSessionManager
{
    public LeafSessionService(HttpClient client, LeafContext leafContext, LoggingService logging,
        BaseStorageService storageManager, IServiceScopeFactory serviceScopeFactory,
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
                    (PointF?)new PointF(location?.Data["data"]?["attributes"]?["gpsLatitude"]?.GetValue<float?>() ?? 0,
                        location?.Data["data"]?["attributes"]?["gpsLongitude"]?.GetValue<float?>() ?? 0));
                return session.LastLocation?.Item2 ?? new PointF(0f, 0f);
            }
        }

        return session.LastLocation?.Item2 ?? new PointF(0f, 0f);
    }

    public async Task<Response<JsonObject>?> VehicleClimate(VehicleSessionBase session, string? vin, bool forceUpdate = true)
    {
        if (forceUpdate)
        {
            _ = await PerformAction(session, vin, "refresh-hvac-status", "RefreshHvacStatus", new JsonObject());
        }

        return await GetStatus(session, vin, "hvac-status");
    }

    public async Task<Response<JsonObject>?> VehicleLock(VehicleSessionBase session, string? vin)
    {
        return await GetStatus(session, vin, "lock-status");
    }

    public async Task<Response<JsonObject>?> VehicleBattery(VehicleSessionBase session, string? vin)
    {
        return await GetStatus(session, vin, "battery-status");
    }

    public async Task<Response<JsonObject>?> SetVehicleClimate(VehicleSessionBase session, string? vin, decimal targetTemp, bool active)
    {
        if (!active)
        {
            _ = await PerformAction(session, vin, "hvac-start", "HvacStart", new JsonObject
            {
                { "action", "cancel" },
                { "targetTemperature", targetTemp }
            });
        }

        return await PerformAction(session, vin, "hvac-start", "HvacStart", new JsonObject
        {
            { "action", active ? "start" : "stop" },
            { "targetTemperature", targetTemp }
        });
    }

    public async Task<Response<JsonObject>?> SetVehicleLock(VehicleSessionBase session, string? vin, bool locked)
    {
        return await PerformAction(session, vin, "lock-unlock", "LockUnlock", new JsonObject
        {
            { "action", locked ? "lock" : "unlock" },
            { "target", "doors_hatch" }, // 'driver_s_door' : 'doors_hatch'
            { "srp", "" /* Need to investigate SRP */ }
        });
    }

    public async Task<Response<JsonObject>?> FlashLights(VehicleSessionBase session, string? vin, int duration = 5)
    {
        return await PerformAction(session, vin, "horn-lights", "HornLights", new JsonObject
        {
            { "action", "start" },
            { "duration", duration },
            { "target", "lights" }
        });
    }

    public async Task<Response<JsonObject>?> BeepHorn(VehicleSessionBase session, string? vin, int duration = 5)
    {
        return await PerformAction(session, vin, "horn-lights", "HornLights", new JsonObject
        {
            { "action", "start" },
            { "duration", duration },
            { "target", "horn" }
        });
    }

    protected override async Task<Response<JsonObject>?> PerformActionImplementation(VehicleSessionBase session, string? vin, string action,
        string type, JsonObject attributes)
    {
        object httpRequestData = new JsonObject
        {
            {
                "data", new JsonObject
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
            Content = new StringContent(JsonSerializer.Serialize(httpRequestData), Encoding.UTF8,
                "application/vnd.api+json")
        };

        var response = await MakeRequest(session, httpRequestMessage,
            Configuration["Nissan:EU:car_adapter_base_url"]);

        return response;
    }

    protected override async Task<Response<JsonObject>?> GetStatusImplementation(VehicleSessionBase session, string? vin, string action)
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
        Response<JsonObject>? authenticationResult = await Authenticate(session, session.Username, session.Password, authenticateResult);
        var authorizeResult = await Authorize(session, authenticationResult);
        var accessTokenResult = await AccessToken(session, authorizeResult);

        if (accessTokenResult?.Success == true)
        {
            session.AuthenticatedAccessToken = accessTokenResult.Data["access_token"].GetValue<string>();

            var usersResult = await UsersResult(session);

            var vehiclesResult = await VehiclesResult(session, usersResult.Data["userId"].GetValue<string>());

            session.VINs.Add(vehiclesResult.Data.data[0].vin);
            session.CarPictureUrl = vehiclesResult.Data.data[0].pictureURL;
        }
        else
        {
            session.AuthenticatedAccessToken = null;
        }

        return session.Authenticated;
    }

    private async Task<Response<JsonObject>?> Authenticate(VehicleSessionBase session)
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

        var response = await MakeRequest(session, httpRequestMessage);

        return response;
    }

    private async Task<Response<JsonObject>?> Authenticate(VehicleSessionBase session, string username, string password, Response<JsonObject>? authenticateResult)
    {
        if (authenticateResult?.Success != true)
        {
            return null;
        }

        // Because this data is so 'hand-crafted' I have left it as a simple JObject instead of creating a bespoke object to control this.
        dynamic httpRequestData = new JsonObject
        {
            { "authId", authenticateResult.Data["authId"].GetValue<string>() },
            { "template", string.Empty },
            { "stage", "LDAP1" },
            { "header", "Sign in" },
            {
                "callbacks", JsonValue.Create(new List<JsonObject>() {
                    new JsonObject
                    {
                        { "type", "NameCallback" },
                        {
                            "output", JsonValue.Create(new List <JsonObject>() {
                                new JsonObject {
                                    { "name", "prompt" }, { "value", "User Name:" }
                                }
                            })
                        },
                        {
                            "input", JsonValue.Create(new List <JsonObject>() {
                                new JsonObject {
                                    { "name", "IDToken1" }, { "value", username }
                                }
                            })
                        }
                    },
                    new JsonObject
                    {
                        { "type", "PasswordCallback" },
                        {
                            "output", JsonValue.Create(new List<JsonObject>() {
                                new JsonObject
                                {
                                    { "name", "prompt" },
                                    { "value", "Password:" }
                                }
                            })
                        },
                        {
                            "input", JsonValue.Create(new List<JsonObject>() {
                                new JsonObject
                                {
                                    { "name", "IDToken2" },
                                    { "value", password }
                                }
                            })
                        }
                    }
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
            Content = new StringContent(JsonSerializer.Serialize(httpRequestData), Encoding.UTF8, "application/json")
        };

        var response = await MakeRequest(session, httpRequestMessage);

        return response;
    }

    private async Task<Response<JsonObject>?> Authorize(VehicleSessionBase session, Response<JsonObject>? authenticateResult)
    {
        if (authenticateResult?.Success != true)
        {
            return null;
        }

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"oauth2{authenticateResult.Data["realm"].GetValue<string>()}/authorize?client_id={Configuration["Nissan:EU:client_id"]}&redirect_uri={Configuration["Nissan:EU:redirect_uri"]}&response_type=code&scope={Configuration["Nissan:EU:scope"]}&nonce=sdfdsfez&state=af0ifjsldkj")
        {
            Headers =
            {
                { "Cookie", $"i18next=en-UK; amlbcookie=05; kauthSession=\"{authenticateResult.Data["tokenId"].GetValue<string>()}\"" }
            }
        };

        var response = await MakeRequest(session, httpRequestMessage);

        if (response != null)
        {
            response.Data = authenticateResult!.Data;
        }

        return response;
    }

    private async Task<Response<JsonObject>?> AccessToken(VehicleSessionBase session, Response<JsonObject>? authenticateResult)
    {
        if (authenticateResult?.Code != (int)HttpStatusCode.Found)
        {
            return null;
        }

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post,
            $"oauth2{authenticateResult.Data["realm"].GetValue<string>()}/access_token?code={authenticateResult!.Headers.Location?.ToString().Split('=')[1].Split('&')[0]}&client_id={Configuration["Nissan:EU:client_id"]}&client_secret={Configuration["Nissan:EU:client_secret"]}&redirect_uri={Configuration["Nissan:EU:redirect_uri"]}&grant_type=authorization_code")
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

    private async Task<Response<JsonObject>?> UsersResult(VehicleSessionBase session)
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

    private async Task<Response<Nissan>?> VehiclesResult(VehicleSessionBase session, string userId)
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"v5/users/{userId}/cars")
        {
            Headers =
            {
                { "Authorization", $"Bearer {session.AuthenticatedAccessToken}" }
            }
        };

        var response = await MakeRequest<Nissan>(session, httpRequestMessage, Configuration["Nissan:EU:user_base_url"]);

        return response;
    }
}