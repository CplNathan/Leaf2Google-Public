using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Web;
using Leaf2Google.Dependency;
using Leaf2Google.Dependency.Google;
using Leaf2Google.Dependency.Google.Devices;
using Leaf2Google.Entities.Car;
using Leaf2Google.Entities.Generic;
using Leaf2Google.Entities.Google;
using Leaf2Google.Json.Google;
using Leaf2Google.Models.Generic;
using Leaf2Google.Models.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;

namespace Leaf2Google.Controllers;

public class GoogleController : BaseController
{
    public GoogleController(BaseStorageManager storageManager, ICarSessionManager sessionManager, GoogleStateManager googleState, LeafContext leafContext,
        LoggingManager logging, IEnumerable<IDevice> activeDevices, IConfiguration configuration)
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

    protected GoogleStateManager GoogleState { get; }

    protected LeafContext LeafContext { get; }

    protected LoggingManager Logging { get; }

    protected IEnumerable<IDevice> Devices { get; }

    protected IConfiguration Configuration { get; }

    // Welcome to hell
    [HttpPost]
    [Consumes("application/json")]
    public async Task<ActionResult> Fulfillment([FromHeader] string Authorization, [FromBody] GoogleIntentRequest request)
    {
        var accessToken = Authorization?.Split("Bearer ")[1];

        var token = await LeafContext.GoogleTokens.Include(token => token.Owner).ThenInclude(auth => auth.Owner).FirstOrDefaultAsync(token =>
            accessToken == token.AccessToken.ToString() && token.TokenExpires > DateTime.UtcNow);
        if (token is null)
            return Unauthorized("{\"error\": \"invalid_grant\"}");

        var auth = token.Owner;
        if (auth.Owner is null || auth.Deleted.HasValue)
            return Unauthorized("{\"error\": \"invalid_grant\"}");

        var response = new GoogleIntentResponse(request);

        var leafSession = StorageManager.VehicleSessions.FirstOrDefault(session => session.Key == auth.Owner.CarModelId)
            .Value;
        if (leafSession is null)
            return Unauthorized("{\"error\": \"invalid_grant\"}");

        var userDevices = GoogleState.GetOrCreateDevices(leafSession.SessionId);

        foreach (Input action in request.inputs)
        {
            var intent = (action.intent ?? string.Empty).Split("action.devices.");

            if (intent.Length <= 1)
                return BadRequest();

            switch (intent[1])
            {
                case "SYNC":
                    {
                        response.payload = new SyncPayload() { devices = new List<JsonObject>( userDevices.Select(device => device.Value.Sync())), agentUserId = token.TokenId.ToString() };

                        break;
                    }

                case "QUERY":
                    {
                        // Logging
                        Console.WriteLine(await Logging.AddLog(token?.Owner?.Owner?.CarModelId ?? Guid.Empty, AuditAction.Execute,
                            AuditContext.Google, $"Google executing query for {token?.Owner?.Owner?.NissanUsername ?? string.Empty}"));
                        auth.LastQuery = DateTime.UtcNow;

                        var requestedDevices = action.payload.devices.Select(device => device.id);

                        var deviceQuery = new JsonObject();
                        Dictionary<string, Task<QueryDeviceData>> deviceQueryTask = new Dictionary<string, Task<QueryDeviceData>>();

                        foreach (var device in userDevices.Where(device => requestedDevices.Contains(device.Value.Id)))
                        {
                            var deviceType = device.Key;
                            var deviceData = Devices.FirstOrDefault(x => x.GetType() == deviceType);

                            if (deviceData != null)
                                deviceQueryTask.Add(device.Value.Id, deviceData.QueryAsync(leafSession, leafSession.PrimaryVin));
                        }

                        foreach (var deviceTask in deviceQueryTask)
                        {
                            deviceQuery.Add(deviceTask.Key, JsonValue.Create(await deviceTask.Value));
                        }

                        response.payload = new QueryPayload() { devices = deviceQuery, agentUserId = token.TokenId.ToString() };

                        break;
                    }
                case "EXECUTE":
                    {
                        // Logging
                        Console.WriteLine(await Logging.AddLog(token?.Owner?.Owner?.CarModelId ?? Guid.Empty, AuditAction.Execute,
                            AuditContext.Google, $"Google executing command for {token?.Owner?.Owner?.NissanUsername ?? string.Empty}"));
                        auth.LastExecute = DateTime.UtcNow;

                        var executedCommands = new List<ExecuteDeviceData>();

                        foreach (var command in action.payload.commands)
                        {
                            var requestedDevices = command.devices.Select(device => device.id);

                            foreach (var execution in command.execution)
                            {
                                var updatedIds = new List<string>();

                                Dictionary<string, Task<ExecuteDeviceData>> updatedStatesTask = new Dictionary<string, Task<ExecuteDeviceData>>();

                                foreach (var device in userDevices.Where(device =>
                                             requestedDevices.Contains(device.Value.Id) &&
                                             device.Value.SupportedCommands.Contains(execution.command)))
                                {
                                    var deviceType = device.Key;
                                    var deviceData = Devices.FirstOrDefault(x => x.GetType() == deviceType);

                                    var parsedCommand = execution.command.Split("action.devices.commands.")[1];

                                    if (deviceData != null)
                                    {
                                        updatedStatesTask.Add(device.Value.Id, deviceData.ExecuteAsync(leafSession, leafSession.PrimaryVin, parsedCommand, execution._params));
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

                        response.payload = new ExecutePayload() { commands = executedCommands, agentUserId = token.TokenId.ToString() };

                        break;
                    }

                case "DISCONNECT":
                    {
                        // Todo: handle
                        break;
                    }
            }

            LeafContext.GoogleAuths.Update(auth);
            await LeafContext.SaveChangesAsync();
        }

        return Json(response);
    }

    [HttpPost]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<ActionResult<AccessTokenDto>> Token([FromForm] IFormCollection form)
    {
        if (form == null)
            return BadRequest();

        if (form["grant_type"] != "refresh_token" && form["grant_type"] != "authorization_code")
            return BadRequest();

        if (form["grant_type"] == "authorization_code" && string.IsNullOrEmpty(form["code"]))
            return BadRequest();

        // Ensure that the provided code matches the same client that requested it.
        if (form["grant_type"] == "authorization_code" && !await LeafContext.GoogleAuths.AnyAsync(auth =>
                auth.AuthCode.ToString() == form["code"].ToString() || auth.ClientId == form["client_id"].ToString()))
            return BadRequest("{\"error\": \"invalid_grant\"}");

        if (form["grant_type"] == "authorization_code" && string.IsNullOrEmpty(form["redirect_uri"]))
            return BadRequest("{\"error\": \"invalid_grant\"}");

        if (form["grant_type"] == "authorization_code")
        {
            // Ensure that the uri which requested this matches the token request.
            var formUri = new Uri(form["redirect_uri"].ToString());
            if (form["grant_type"] == "authorization_code" &&
                !await LeafContext.GoogleAuths.AnyAsync(auth => auth.RedirectUri == formUri))
                return BadRequest("{\"error\": \"invalid_grant\"}");
        }

        if (form["grant_type"] == "refresh_token" && !await LeafContext.GoogleTokens.Include(token => token.Owner).AnyAsync(token =>
                form["refresh_token"].ToString() == token.RefreshToken.ToString() &&
                form["client_id"].ToString() == token.Owner.ClientId))
            return BadRequest("{\"error\": \"invalid_grant\"}");

        // Ensure that the client secret given by google matches our stored one.
        if (form["client_secret"] != Configuration["Google:client_secret"])
            return BadRequest("{\"error\": \"invalid_grant\"}");

        // Token state
        TokenModel? token = null;
        var tokenState = EntityState.Unchanged;

        if (form["grant_type"] == "authorization_code")
        {
            token = new TokenModel
            {
                Owner = (await LeafContext.GoogleAuths.Include(auth => auth.Owner).FirstOrDefaultAsync(auth =>
                    form["code"].ToString() == auth.AuthCode.ToString()))!,
                RefreshToken = Guid.NewGuid()
            };

            Console.WriteLine(await Logging.AddLog(token?.Owner?.Owner?.CarModelId ?? Guid.Empty, AuditAction.Update,
                AuditContext.Google, $"Regenerating refresh token for {token?.Owner?.Owner?.NissanUsername ?? string.Empty}"));

            tokenState = EntityState.Added;
        }
        else if (form["grant_type"] == "refresh_token")
        {
            token = await LeafContext.GoogleTokens.Include(token => token.Owner).FirstOrDefaultAsync(token =>
                form["refresh_token"].ToString() == token.RefreshToken.ToString())!;
            tokenState = EntityState.Modified;

            Console.WriteLine(await Logging.AddLog(token?.Owner?.Owner?.CarModelId ?? Guid.Empty, AuditAction.Update,
                AuditContext.Google, $"Regenerating authorization code for {token?.Owner?.Owner?.NissanUsername ?? string.Empty}"));
        }

        if (token == null || token.Owner == null || token.Owner.Deleted.HasValue)
            return BadRequest("{\"error\": \"invalid_grant\"}");

        token.AccessToken = Guid.NewGuid(); // generate
        token.TokenExpires = DateTime.UtcNow + TimeSpan.FromMinutes(30);

        LeafContext.Entry(token).State = tokenState;
        await LeafContext.SaveChangesAsync();

        if (tokenState == EntityState.Added)
            return new RefreshTokenDto(token);
        if (tokenState == EntityState.Modified)
            return new AccessTokenDto(token);
        return BadRequest("{\"error\": \"invalid_grant\"}");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Auth([FromForm] AuthPostFormGoogleModel form)
    {
        if (form == null)
        {
            var model = new AuthViewModel
            {
                client_id = form?.client_id ?? "",
                redirect_uri = form?.redirect_uri,
                state = form?.state ?? ""
            };
            return await Auth(model);
        }

        var auth = await LeafContext.GoogleAuths.Include(auth => auth.Owner).FirstOrDefaultAsync(auth => auth.AuthState == form.state);
        if (auth == null)
            return BadRequest();

        auth.AuthCode = Guid.NewGuid();

        var query = HttpUtility.ParseQueryString(string.Empty);
        query["code"] = auth.AuthCode.ToString();
        query["state"] = form.state;

        var redirect_uri_processed = new UriBuilder(form.redirect_uri!);
        redirect_uri_processed.Query = query.ToString();

        CarModel? leaf = null;

        var leafId = await StorageManager.UserStorage.DoCredentialsMatch(form.NissanUsername, form.NissanPassword, true);
        if (leafId != Guid.Empty)
        {
            leaf = await StorageManager.UserStorage.RestoreUser(leafId);
        }

        if (leaf == null)
        {
            leaf = new CarModel(form.NissanUsername, form.NissanPassword);
        }

        if (await SessionManager.AddAsync(leaf))
        {
            auth.Owner = leaf;

            if (!await LeafContext.NissanLeafs.AnyAsync(car => car.CarModelId == leaf.CarModelId))
                await LeafContext.NissanLeafs.AddAsync(leaf);

            LeafContext.Entry(auth).State = EntityState.Modified;
            await LeafContext.SaveChangesAsync();

            return Redirect(redirect_uri_processed.ToString());
        }

        {
            AddToast(new ToastViewModel
            {
                Title = "Authentication",
                Message = "Unable to authenticate to Nissan Services using the supplied credentials."
            });

            var model = new AuthViewModel
            {
                client_id = form.client_id,
                redirect_uri = form.redirect_uri,
                state = form.state
            };

            ReloadViewBag();
            return await Auth(model);
        }
    }

    [HttpGet]
    public async Task<ActionResult> Auth([FromQuery] AuthViewModel form)
    {
        if (form.client_id != Configuration["Google:client_id"])
            return BadRequest();

        var redirect_application = form!.redirect_uri?.AbsolutePath.Split('/')
            .Where(item => !string.IsNullOrEmpty(item))
            .Skip(1)
            .Take(1)
            .FirstOrDefault();

        if (redirect_application != Configuration["Google:client_reference"])
            return BadRequest();

        var code = Guid.NewGuid().ToString();

        var auth = new AuthModel
        {
            RedirectUri = form.redirect_uri,
            ClientId = form.client_id,
            AuthState = form.state
        };

        await LeafContext.GoogleAuths.AddAsync(auth);
        await LeafContext.SaveChangesAsync();

        return View("Index", form);
    }
}