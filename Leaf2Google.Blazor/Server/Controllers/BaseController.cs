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
using Leaf2Google.Entities.Google;

namespace Leaf2Google.Controllers;

public class BaseController : Controller
{
    public BaseController(BaseStorageService storageManager, LeafContext leafContext)
    {
        StorageManager = storageManager;
        LeafContext = leafContext;
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

    protected VehicleSessionBase AuthenticatedSession
    {
        get
        {
            var jtiClaim = AuthenticatedUser?.FindFirst(JwtRegisteredClaimNames.Jti);

            if (jtiClaim is null || jtiClaim.Value.Split(",").Count() < 1)
                throw new UnauthorizedAccessException("Attempted to access active session but JWT was not valid or claim was not found. Invalid call.");

            var claimValue = jtiClaim.Value.Split(",")[0];
            return StorageManager.VehicleSessions.First(session => session.Key.ToString() == claimValue).Value;
        }
    }

    protected AuthEntity AuthenticatedSessionEntity
    {
        get
        {
            var jtiClaim = AuthenticatedUser?.FindFirst(JwtRegisteredClaimNames.Jti);

            if (jtiClaim is null || jtiClaim.Value.Split(",").Count() < 2)
                throw new UnauthorizedAccessException("Attempted to access active session but JWT was not valid or claim was not found. Invalid call.");

            var claimValue = jtiClaim.Value.Split(",")[1];
            return LeafContext.GoogleAuths.First(auth => auth.AuthId.ToString() == claimValue);
        }
    }

    protected BaseStorageService StorageManager { get; }

    protected LeafContext LeafContext;

    protected string? SelectedVin
    {
        get => AuthenticatedSession.PrimaryVin;
    }
}