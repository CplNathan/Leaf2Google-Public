using Leaf2Google.Contexts;
using Leaf2Google.Dependency.Managers;
using Leaf2Google.Models;
using Leaf2Google.Models.Google;
using Leaf2Google.Models.Car;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Web;

namespace Leaf2Google.Controllers
{
    public class GoogleController : BaseController
    {
        private readonly ILogger<HomeController> _logger;

        private readonly GoogleStateManager _googleState;

        private readonly LeafContext _leafContext;

        private readonly IConfiguration _configuration;

        public GoogleController(ILogger<HomeController> logger, LeafSessionManager sessions, GoogleStateManager googleState, LeafContext googleContext, IConfiguration configuration)
            : base(logger, sessions)
        {
            _logger = logger;
            _googleState = googleState;
            _leafContext = googleContext;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index(AuthForm form)
        {
            ViewBag.AuthForm = form;

            return View();
        }

        // Welcome to hell
        [HttpPost]
        [Consumes("application/json")]
        public async Task<ActionResult> Fulfillment([FromBody] JObject fulfillment, [FromHeader] string Authorization)
        {
            var accessToken = Authorization?.Split("Bearer ")[1];

            var token = await _leafContext.GoogleTokens.FirstOrDefaultAsync(token => accessToken == token.AccessToken.ToString() && token.TokenExpires > DateTime.UtcNow);
            if (token is null)
                return Unauthorized("{\"error\": \"invalid_grant\"}");

            var auth = token.Owner;
            if (auth.Owner is null || auth.Deleted.HasValue)
                return Unauthorized("{\"error\": \"invalid_grant\"}");

            JObject response = new JObject {
                { "requestId", fulfillment["requestId"] },
                { "payload", new JObject() {
                    { "agentUserId", token.TokenId }
                }}
            };

            var leafSession = Sessions.VehicleSessions.FirstOrDefault(session => session.SessionId == auth.Owner.CarModelId);
            if (leafSession is null)
                return Unauthorized("{\"error\": \"invalid_grant\"}");

            var userDevices = _googleState.GetOrCreateDevices(leafSession.SessionId);

            var inputs = (JArray?)fulfillment["inputs"] ?? new JArray();
            foreach (JObject action in inputs)
            {
                string[] intent = (action["intent"]?.ToString() ?? string.Empty).Split("action.devices.");

                if (intent.Length <= 1)
                    return BadRequest();

                switch (intent[1])
                {
                    case "SYNC":
                        {
                            ((JObject)response["payload"]!).Add("devices", JArray.FromObject(userDevices.Select(device => device.Sync())));
                            break;
                        }

                    case "QUERY":
                        {
                            auth.LastQuery = DateTime.UtcNow;
                            var requestedDevicesObj = action["payload"]?["devices"]?.ToObject<List<JObject>>() ?? new List<JObject>();
                            var requestedDevices = requestedDevicesObj.Select(device => (string?)device["id"]).Where(device => device is not null) ?? new List<string>();

                            var deviceQuery = new JObject();
                            foreach (var device in userDevices.Where(device => requestedDevices.Contains(device.Id)))
                            {
                                deviceQuery.Add(new JProperty($"{device.Id}", await device.QueryAsync(Sessions, leafSession, leafSession.PrimaryVin)));
                            }

                            ((JObject)response["payload"]!).Add("devices", deviceQuery);
                            break;
                        }
                    case "EXECUTE":
                        {
                            auth.LastExecute = DateTime.UtcNow;
                            List<JObject> executedCommands = new List<JObject>();

                            var requestedCommands = action["payload"]?["commands"]?.ToObject<List<JObject>>() ?? new List<JObject>();
                            foreach (JObject command in requestedCommands)
                            {
                                var requestedDevicesObj = command?["devices"]?.ToObject<List<JObject>>() ?? new List<JObject>();
                                var requestedDevices = requestedDevicesObj.Select(device => (string?)device["id"]).Where(device => device is not null) ?? new List<string>();

                                var requestedExecution = command?["execution"]?.ToObject<List<JObject>>() ?? new List<JObject>();

                                foreach (JObject execution in requestedExecution)
                                {
                                    List<string> updatedIds = new List<string>();
                                    List<JObject> updatedStates = new List<JObject>();

                                    foreach (var device in userDevices.Where(device => requestedDevices.Contains(device.Id) && device.SupportedCommands.Contains((string?)execution["command"] ?? string.Empty)))
                                    {
                                        updatedIds.Add(device.Id);
                                        updatedStates.Add(await device.ExecuteAsync(Sessions, leafSession, leafSession.PrimaryVin, (JObject)execution["params"]!));
                                    }

                                    var mergedStates = new JObject();
                                    foreach (var state in updatedStates)
                                    {
                                        mergedStates.Merge(state, new JsonMergeSettings
                                        {
                                            // union array values together to avoid duplicates
                                            MergeArrayHandling = MergeArrayHandling.Union
                                        });
                                    }

                                    if (mergedStates.ContainsKey("errors"))
                                    {
                                        mergedStates.Merge(mergedStates["errors"]!);
                                    }

                                    var executedCommand = new JObject()
                                    {
                                        { "ids", JArray.FromObject(updatedIds) },
                                        { "status", "SUCCESS" }, // ??
                                        { "states", mergedStates }
                                    };

                                    executedCommands.Add(new JObject()
                                    {
                                        { "ids", JArray.FromObject(updatedIds) },
                                        { "status", "SUCCESS" }, // ??
                                        { "states", mergedStates }
                                    });
                                }
                            }

                            ((JObject)response["payload"]!).Add("commands", JArray.FromObject(executedCommands));

                            break;
                        }

                    case "DISCONNECT":
                        {
                            // Todo: handle
                            break;
                        }
                }

                _leafContext.GoogleAuths.Update(auth);
                await _leafContext.SaveChangesAsync();
                _googleState.Devices[leafSession.SessionId] = userDevices;

                /*
                 * ????
                var user = _googleContext.GoogleAuths.FirstOrDefault(user => user.Auth == token.Auth);
                if (user == null)
                    return Unauthorized("{\"error\": \"invalid_grant\"}");
                */
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
            if (form["grant_type"] == "authorization_code" && !(await _leafContext.GoogleAuths.AnyAsync(auth => auth.AuthCode.ToString() == form["code"].ToString() || auth.ClientId == form["client_id"].ToString())))
                return BadRequest("{\"error\": \"invalid_grant\"}");

            if (form["grant_type"] == "authorization_code" && string.IsNullOrEmpty(form["redirect_uri"]))
            {
                return BadRequest("{\"error\": \"invalid_grant\"}");
            }
            else if (form["grant_type"] == "authorization_code")
            {
                // Ensure that the uri which requested this matches the token request.
                var formUri = new Uri(form["redirect_uri"].ToString());
                if (form["grant_type"] == "authorization_code" && !(await _leafContext.GoogleAuths.AnyAsync(auth => auth.RedirectUri == formUri)))
                    return BadRequest("{\"error\": \"invalid_grant\"}");
            }

            if (form["grant_type"] == "refresh_token" && !(await _leafContext.GoogleTokens.AnyAsync(token => (form["refresh_token"].ToString() == token.RefreshToken.ToString()) && (form["client_id"].ToString() == token.Owner.ClientId))))
                return BadRequest("{\"error\": \"invalid_grant\"}");

            // Ensure that the client secret given by google matches our stored one.
            if (form["client_secret"] != _configuration["Google:client_secret"])
                return BadRequest("{\"error\": \"invalid_grant\"}");

            // Token state
            Token? token = null;
            EntityState tokenState = EntityState.Unchanged;

            if (form["grant_type"] == "authorization_code")
            {
                token = new Token()
                {
                    Owner = (await _leafContext.GoogleAuths.FirstOrDefaultAsync(auth => form["code"].ToString() == auth.AuthCode.ToString()))!,
                    RefreshToken = Guid.NewGuid()
                };

                tokenState = EntityState.Added;
            }
            else if (form["grant_type"] == "refresh_token")
            {
                token = await _leafContext.GoogleTokens.FirstOrDefaultAsync(token => form["refresh_token"].ToString() == token.RefreshToken.ToString())!;
                tokenState = EntityState.Modified;
            }

            if (token == null || token.Owner == null || token.Owner.Deleted.HasValue)
                return BadRequest("{\"error\": \"invalid_grant\"}");

            token.AccessToken = Guid.NewGuid(); // generate
            token.TokenExpires = DateTime.UtcNow + TimeSpan.FromMinutes(30);

            _leafContext.Entry(token).State = tokenState;
            await _leafContext.SaveChangesAsync();

            if (tokenState == EntityState.Added)
                return new RefreshTokenDto(token);
            else if (tokenState == EntityState.Modified)
                return new AccessTokenDto(token);
            else
                return BadRequest("{\"error\": \"invalid_grant\"}");
        }

        private void Session_OnAuthenticationAttempt(object sender, string authToken)
        {
            var session = sender as VehicleSessionBase;

            if (session != null)
            {
                session.tcs?.TrySetResult(true);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Index([FromForm] AuthPostForm form)
        {
            if (!ModelState.IsValid)
                return View();

            if (form == null)
                return View();

            var auth = await _leafContext.GoogleAuths.FirstOrDefaultAsync(auth => auth.AuthState == form.state);
            if (auth == null)
                return BadRequest();

            auth.AuthCode = Guid.NewGuid();

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["code"] = auth.AuthCode.ToString();
            query["state"] = form.state;

            var redirect_uri_processed = new UriBuilder(form.redirect_uri!);
            redirect_uri_processed.Query = query.ToString();

            CarModel? leaf = null;
            IEnumerable<CarModel> leafs = _leafContext.NissanLeafs.AsEnumerable();

            Func<CarModel, bool> authenticationPredicate = leaf =>
            {
                return leaf.NissanUsername == form.NissanUsername && leaf.NissanPassword == form.NissanPassword;
            };

            if (leafs.Any(authenticationPredicate))
            {
                leaf = leafs.First(authenticationPredicate);
                leaf.Deleted = null;
            }
            else
            {
                leaf = new CarModel(form.NissanUsername, form.NissanPassword);
            }

            if (await Sessions.AddAsync(leaf, Session_OnAuthenticationAttempt))
            {
                auth.Owner = leaf;

                _leafContext.Entry(auth).State = EntityState.Modified;
                await _leafContext.SaveChangesAsync();

                return Redirect(redirect_uri_processed.ToString());
            }
            else
            {
                AddToast(new ToastViewModel()
                {
                    Title = "Authentication",
                    Message = "Unable to authenticate to Nissan Services using the supplied credentials."
                });

                return RedirectToAction("Index", "Google", form);
            }
        }

        [HttpGet]
        public async Task<ActionResult> Auth([FromQuery] AuthForm form)
        {
            if (form.client_id != _configuration["Google:client_id"])
                return BadRequest();

            var redirect_application = form!.redirect_uri?.AbsolutePath.Split('/')
                .Where(item => !string.IsNullOrEmpty(item))
                .Skip(1)
                .Take(1)
                .FirstOrDefault();

            if (redirect_application != _configuration["Google:client_reference"])
                return BadRequest();

            var code = Guid.NewGuid().ToString();

            Auth auth = new Auth()
            {
                RedirectUri = form.redirect_uri,
                ClientId = form.client_id,
                AuthState = form.state
            };

            ViewBag.AuthForm = form;

            _leafContext.GoogleAuths.Add(auth);
            await _leafContext.SaveChangesAsync();

            return RedirectToAction("Index", "Google", form);
        }
    }
}