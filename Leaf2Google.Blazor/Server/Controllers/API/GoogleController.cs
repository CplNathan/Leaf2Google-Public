// Copyright (c) Nathan Ford. All rights reserved. GoogleController.cs

using Leaf2Google.Blazor.Server.Helpers;
using Leaf2Google.Controllers;
using Leaf2Google.Entities.Generic;
using Leaf2Google.Entities.Google;
using Leaf2Google.Json.Google;
using Leaf2Google.Services.Google;
using Leaf2Google.Services.Google.Devices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Nodes;

namespace Leaf2Google.Blazor.Server.Controllers.API;

[ApiController]
[Route("api/[controller]/[action]/{id?}")]
public class GoogleController : BaseController
{
    public GoogleController(BaseStorageService storageManager, ICarSessionManager sessionManager, GoogleStateService googleState, LeafContext leafContext,
        LoggingService logging, IEnumerable<IDevice> activeDevices, IConfiguration configuration)
        : base(storageManager)
    {
        SessionManager = sessionManager;
        GoogleState = googleState;
        LeafContext = leafContext;
        Logging = logging;
        Devices = activeDevices;
        Configuration = configuration;
    }

    protected ICarSessionManager SessionManager { get; }

    protected GoogleStateService GoogleState { get; }

    protected LeafContext LeafContext { get; }

    protected LoggingService Logging { get; }

    protected IEnumerable<IDevice> Devices { get; }

    protected IConfiguration Configuration { get; }

    // Welcome to hell
    [HttpPost]
    [Consumes("application/json")]
    [GoogleAuthorize]
    public async Task<ActionResult> Fulfillment([FromBody] GoogleIntentRequest request)
    {
        //return Unauthorized("{\"error\": \"invalid_grant\"}");

        var response = new GoogleIntentResponse(request);

        if (AuthenticatedSession is null)
        {
            return JWT.UnauthorizedResponse();
        }

        var userDevices = GoogleState.GetOrCreateDevices(AuthenticatedSession.SessionId);

        foreach (Input action in request?.inputs ?? new Input[0])
        {
            var intent = (action.intent ?? string.Empty).Split("action.devices.");

            if (intent.Length <= 1)
            {
                return BadRequest();
            }

            switch (intent[1])
            {
                case "SYNC":
                    {
                        var devices = new List<JsonObject>();

                        foreach (var device in userDevices)
                        {
                            devices.Add(device.Value.Sync());
                        }

                        response.payload = new SyncPayload()
                        {
                            devices = devices,
                            agentUserId = AuthenticatedSession.SessionId.ToString()
                        };

                        break;
                    }

                case "QUERY":
                    {
                        // Logging
                        Console.WriteLine(Logging.AddLog(AuthenticatedSession?.SessionId ?? Guid.Empty, AuditAction.Execute,
                            AuditContext.Google, $"Google executing query for {AuthenticatedSession?.Username ?? string.Empty}"));
                        AuthenticatedSessionEntity.LastQuery = DateTime.UtcNow;

                        var requestedDevices = action.payload.devices.Select(device => device.id);

                        var deviceQuery = new JsonObject();
                        Dictionary<string, Task<QueryDeviceData>> deviceQueryTask = new Dictionary<string, Task<QueryDeviceData>>();

                        foreach (var deviceData in userDevices.Where(device => requestedDevices.Contains(device.Value.Id)))
                        {
                            var deviceType = deviceData.Key;
                            var device = Devices.FirstOrDefault(x => x.GetType() == deviceType);

                            if (device != null)
                            {
                                deviceQueryTask.Add(deviceData.Value.Id, device.QueryAsync(AuthenticatedSession, deviceData.Value, AuthenticatedSession.PrimaryVin));
                            }
                        }

                        foreach (var deviceTask in deviceQueryTask)
                        {
                            deviceQuery.Add(deviceTask.Key, JsonValue.Create(await deviceTask.Value));
                        }

                        response.payload = new QueryPayload()
                        {
                            devices = deviceQuery,
                            agentUserId = AuthenticatedSession.SessionId.ToString()
                        };

                        break;
                    }
                case "EXECUTE":
                    {
                        // Logging
                        Console.WriteLine(Logging.AddLog(AuthenticatedSession?.SessionId ?? Guid.Empty, AuditAction.Execute,
                            AuditContext.Google, $"Google executing command for {AuthenticatedSession?.Username ?? string.Empty}"));
                        AuthenticatedSessionEntity.LastExecute = DateTime.UtcNow;

                        var executedCommands = new List<ExecuteDeviceData>();

                        foreach (var command in action.payload.commands)
                        {
                            var requestedDevices = command.devices.Select(device => device.id);

                            foreach (var execution in command.execution)
                            {
                                var updatedIds = new List<string>();

                                Dictionary<string, Task<ExecuteDeviceData>> updatedStatesTask = new Dictionary<string, Task<ExecuteDeviceData>>();

                                foreach (var deviceData in userDevices.Where(device =>
                                             requestedDevices.Contains(device.Value.Id) &&
                                             device.Value.SupportedCommands.Contains(execution.command)))
                                {
                                    var deviceType = deviceData.Key;
                                    var device = Devices.FirstOrDefault(x => x.GetType() == deviceType);

                                    var parsedCommand = execution.command.Split("action.devices.commands.")[1];

                                    if (device != null)
                                    {
                                        updatedStatesTask.Add(deviceData.Value.Id, device.ExecuteAsync(AuthenticatedSession, deviceData.Value, AuthenticatedSession.PrimaryVin, parsedCommand, execution._params));
                                    }
                                }

                                foreach (var stateTask in updatedStatesTask)
                                {
                                    var state = await stateTask.Value;
                                    state.ids = new List<string>() { stateTask.Key };

                                    executedCommands.Add(state);
                                }
                            }
                        }

                        response.payload = new ExecutePayload()
                        {
                            commands = executedCommands,
                            agentUserId = AuthenticatedSession.SessionId.ToString()
                        };

                        break;
                    }

                case "DISCONNECT":
                    {
                        // Todo: handle, do we get a header sent with this to id?
                        break;
                    }
            }

            // Need to update auth last used.
            LeafContext.GoogleAuths.Update(AuthenticatedSessionEntity);
            await LeafContext.SaveChangesAsync();
        }

        return Json(response);
    }

    [HttpPost]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<JsonResult> Token([FromForm] IFormCollection form)
    {
        if (form == null || form.Count <= 0)
        {
            return JWT.UnauthorizedResponse();
        }

        if (form["client_secret"] != Configuration["Google:client_secret"])
        {
            return JWT.UnauthorizedResponse();
        }

        // Token state
        TokenEntity? token = null;
        var tokenState = EntityState.Unchanged;

        switch (form["grant_type"])
        {
            case "authorization_code":
                {
                    if (string.IsNullOrEmpty(form["code"]) || string.IsNullOrEmpty(form["redirect_uri"]))
                    {
                        return JWT.UnauthorizedResponse();
                    }

                    if (!await LeafContext.GoogleAuths.AnyAsync(auth =>
                            auth.AuthCode.ToString() == form["code"].ToString() &&
                            auth.AuthCode != null &&
                            auth.Data.client_id == form["client_id"].ToString() &&
                            auth.Data.redirect_uri == form["redirect_uri"].ToString()))
                    {
                        return JWT.UnauthorizedResponse();
                    }

                    token = new TokenEntity
                    {
                        Owner = (await LeafContext.GoogleAuths.Include(auth => auth.Owner).FirstOrDefaultAsync(auth =>
                            form["code"].ToString() == auth.AuthCode.ToString()))!,
                        RefreshToken = Guid.NewGuid()
                    };

                    token.Owner.AuthCode = null;

                    Console.WriteLine(Logging.AddLog(token?.Owner?.Owner?.CarModelId ?? Guid.Empty, AuditAction.Update,
                        AuditContext.Google, $"Regenerating refresh token for {token?.Owner?.Owner?.NissanUsername ?? string.Empty}"));

                    tokenState = EntityState.Added;

                    break;
                }
            case "refresh_token":
                {
                    if (!await LeafContext.GoogleTokens.Include(token => token.Owner).AnyAsync(token =>
                            form["refresh_token"].ToString() == token.RefreshToken.ToString() &&
                            form["client_id"].ToString() == token.Owner.Data.client_id))
                    {
                        return JWT.UnauthorizedResponse();
                    }

                    token = await LeafContext.GoogleTokens.Include(token => token.Owner).FirstOrDefaultAsync(token =>
                        form["refresh_token"].ToString() == token.RefreshToken.ToString())!;
                    token.Owner = await LeafContext.GoogleAuths.Include(auth => auth.Owner).FirstOrDefaultAsync(auth =>
                        token.Owner.AuthId == auth.AuthId);

                    Console.WriteLine(Logging.AddLog(token?.Owner?.Owner?.CarModelId ?? Guid.Empty, AuditAction.Update,
                        AuditContext.Google, $"Regenerating authorization code for {token?.Owner?.Owner?.NissanUsername ?? string.Empty}"));

                    tokenState = EntityState.Modified;

                    break;
                }

            default:
                return JWT.UnauthorizedResponse();
        }

        if (token == null || token.Owner == null || token.Owner.Owner == null || token.Owner.Deleted.HasValue)
        {
            return JWT.UnauthorizedResponse();
        }

        LeafContext.Entry(token).State = tokenState;
        LeafContext.Entry(token.Owner).State = EntityState.Modified;
        await LeafContext.SaveChangesAsync();

        var jwtToken = JWT.CreateJWT(StorageManager.VehicleSessions[token.Owner.Owner.CarModelId], Configuration, token.Owner);
        if (tokenState == EntityState.Added)
        {
            return Json(new RefreshTokenDto(token, jwtToken));
        }
        else if (tokenState == EntityState.Modified)
        {
            return Json(new AccessTokenDto(token, jwtToken));
        }
        else
        {
            return JWT.UnauthorizedResponse();
        }
    }
}