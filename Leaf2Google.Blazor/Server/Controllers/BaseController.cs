// Copyright (c) Nathan Ford. All rights reserved. BaseController.cs

using Leaf2Google.Entities.Google;
using Leaf2Google.Models.Car.Sessions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;
using System.Security.Principal;

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

    protected VehicleSessionBase AuthenticatedSession
    {
        get
        {
            var jtiClaim = AuthenticatedUser?.FindFirst(JwtRegisteredClaimNames.Jti);

            if (jtiClaim is null || jtiClaim.Value.Split(",").Length < 1)
            {
                throw new InvalidOperationException("Attempted to access active session but JWT was not valid or claim was not found. Invalid call.");
            }

            var claimValue = jtiClaim.Value.Split(",")[0];
            return StorageManager.VehicleSessions.First(session => session.Key.ToString() == claimValue).Value;
        }
    }

    protected AuthEntity AuthenticatedSessionEntity
    {
        get
        {
            var jtiClaim = AuthenticatedUser?.FindFirst(JwtRegisteredClaimNames.Jti);

            if (jtiClaim is null || jtiClaim.Value.Split(",").Length < 2)
            {
                throw new InvalidOperationException("Attempted to access active session but JWT was not valid or claim was not found. Invalid call.");
            }

            var claimValue = jtiClaim.Value.Split(",")[1];
            return StorageManager.AuthSessions.First(auth => auth.AuthId.ToString() == claimValue);
        }
    }

    protected BaseStorageService StorageManager { get; }

    protected string? SelectedVin
    {
        get => AuthenticatedSession.PrimaryVin;
    }
}