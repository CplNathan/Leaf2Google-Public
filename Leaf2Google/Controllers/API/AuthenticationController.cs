// Copyright (c) Nathan Ford. All rights reserved. APIController.cs

using Leaf2Google.Models.Car.Sessions;
using Leaf2Google.Models.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Leaf2Google.Controllers.API;

public static class Secret
{
    public static Guid SecretValue { get; } = Guid.NewGuid();
}


[Route("api/[controller]/[action]/{id?}")]
[ApiController]
public class AuthenticationController : BaseAPIController
{
    private readonly LeafContext _googleContext;

    private readonly IConfiguration _configuration;

    public static bool IsDebugRelease
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }

    public AuthenticationController(BaseStorageService storageManager, ICarSessionManager sessionManager, LeafContext googleContext, IConfiguration configuration)
        : base(storageManager, sessionManager)
    {
        _googleContext = googleContext;
        _configuration = configuration;
    }

    private string CreateJWT(VehicleSessionBase session)
    {
        var secretkey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_configuration["jwt:key"] ?? Guid.NewGuid().ToString())); // NOTE: SAME KEY AS USED IN Program.cs FILE
        var credentials = new SigningCredentials(secretkey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, session.Username),
            new Claim(JwtRegisteredClaimNames.Sub, session.Username),
            new Claim(JwtRegisteredClaimNames.Email, session.Username),
            new Claim(JwtRegisteredClaimNames.Jti, session.SessionId.ToString())
        };

        var token = new JwtSecurityToken(issuer: IsDebugRelease ? "localhost" : _configuration["fido2:serverDomain"], audience: IsDebugRelease ? "localhost" : _configuration["fido2:serverDomain"], claims: claims, expires: DateTime.Now.AddMinutes(60), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [HttpPost]
    public async Task<JsonResult> Login([FromBody] LoginModel loginData)
    {
        Guid loginResult = await StorageManager.UserStorage.LoginUser(loginData.NissanUsername, loginData.NissanPassword);

        LoginResponse loginResponse = new();
        if (loginResult == Guid.Empty)
        {
            loginResponse.success = false;
            loginResponse.message = "Invalid User";
        }
        if (loginResult != Guid.Empty && StorageManager.VehicleSessions.Any(session => session.Key == loginResult))
        {
            VehicleSessionBase vehicleSession = StorageManager.VehicleSessions.First(session => session.Key == loginResult).Value;

            loginResponse.success = true;
            loginResponse.NissanUsername = vehicleSession.Username;
            loginResponse.sessionId = vehicleSession.SessionId.ToString();
            loginResponse.jwtBearer = CreateJWT(vehicleSession);
            loginResponse.message = "Login Success";
        }

        return Json(loginResponse);
    }

    [HttpPost]
    [Authorize]
    public JsonResult UserInfo()
    {
        CurrentUser userResponse = new()
        {
            NissanUsername = AuthenticatedSession?.Username,
            SessionId = AuthenticatedSession.SessionId,
            IsAuthenticated = AuthenticatedIdentity.IsAuthenticated,
            Claims = AuthenticatedUser.Claims.ToDictionary(c => c.Type, c => c.Value)
        };

        return Json(userResponse);
    }

    /*
    [HttpPost]
    [Authorize]
    public async Task<ViewComponentResult> Delete([FromForm] Guid? authId)
    {
        if (SessionId != null && authId != null && await _googleContext.GoogleAuths.Include(auth => auth.Owner).AnyAsync(auth =>
                auth.Owner != null && auth.Owner.CarModelId == SessionId && auth.AuthId == authId &&
                auth.Deleted == null))
        {
            var auth = _googleContext.GoogleAuths.First(auth => auth.AuthId == authId);
            auth.Deleted = DateTime.UtcNow;

            await _googleContext.SaveChangesAsync();
        }

        return ViewComponent("SessionInfo", new
        {
            viewName = "Auths",
            sessionId = ViewBag?.SessionId ?? null
        });
    }
    */
}