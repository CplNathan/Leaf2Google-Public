using System.Reflection;
using Leaf2Google.Controllers.API;
using Leaf2Google.Models.Generic;
using Leaf2Google.Models.Car.Sessions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json.Nodes;
using System.Security.Principal;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Linq;

namespace Leaf2Google.Controllers;

public class BaseController : Controller
{
    public BaseController(BaseStorageService storageManager)
    {
        StorageManager = storageManager;
    }

    protected ClaimsPrincipal AuthenticatedUser
    {
        get
        {
            return HttpContext.User;
        }
    }

    protected IIdentity? AuthenticatedIdentity
    {
        get
        {
            return AuthenticatedUser.Identity;
        }
    }

    protected VehicleSessionBase? AuthenticatedSession
    {
        get
        {
            var jtiClaim = AuthenticatedUser.FindFirst(JwtRegisteredClaimNames.Jti);

            if (jtiClaim is null)
                return null;

            return StorageManager.VehicleSessions.FirstOrDefault(session => session.Key.ToString() == jtiClaim.Value).Value;
        }
    }

    protected BaseStorageService StorageManager { get; }

    protected Guid? SessionId
    {
        get
        {
            var sessionGuid = "";
            Guid parsedGuid;

            var success = Guid.TryParse(sessionGuid, out parsedGuid);

            return success ? parsedGuid : null;
        }
        set { bool success = true; }
    }

    protected VehicleSessionBase? Session =>
        StorageManager.VehicleSessions.FirstOrDefault(session => session.Key == SessionId).Value;

    protected string? SelectedVin
    {
        get => AuthenticatedSession?.PrimaryVin;
    }
}