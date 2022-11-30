// Copyright (c) Nathan Ford. All rights reserved. AuthenticationController.cs

using Leaf2Google.Blazor.Server.Helpers;
using Leaf2Google.Entities.Car;
using Leaf2Google.Entities.Google;
using Leaf2Google.Models.Car.Sessions;
using Leaf2Google.Models.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace Leaf2Google.Controllers.API;

[Route("api/[controller]/[action]/{id?}")]
[ApiController]
public class AuthenticationController : BaseAPIController
{
    private readonly LeafContext _googleContext;

    private readonly IConfiguration _configuration;

    public AuthenticationController(BaseStorageService storageManager, ICarSessionManager sessionManager, LeafContext googleContext, IConfiguration configuration)
        : base(storageManager, sessionManager)
    {
        _googleContext = googleContext;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<JsonResult> Login([FromBody] LoginModel loginData)
    {
        Guid loginResult = await StorageManager.UserStorage.LoginUser(loginData.NissanUsername, loginData.NissanPassword);

        LoginResponse loginResponse = new();
        if (loginResult == Guid.Empty)
        {
            loginResponse.success = false;
            loginResponse.message = ResponseState.InvalidCredentials;
        }
        if (loginResult != Guid.Empty && StorageManager.VehicleSessions.Any(session => session.Key == loginResult))
        {
            VehicleSessionBase vehicleSession = StorageManager.VehicleSessions.First(session => session.Key == loginResult).Value;

            var jwtString = new JwtSecurityTokenHandler().WriteToken(JWT.CreateJWT(vehicleSession, _configuration));

            loginResponse.success = true;
            loginResponse.NissanUsername = vehicleSession.Username;
            loginResponse.sessionId = vehicleSession.SessionId.ToString();
            loginResponse.jwtBearer = jwtString;
            loginResponse.message = ResponseState.Success;
        }

        return Json(loginResponse);
    }

    [HttpPost]
    public async Task<JsonResult> Register([FromBody] RegisterModel form) // maybe this needs to be optional?
    {
        switch (form.request)
        {
            case RequestState.Initial:
                {
                    if (form.Data.client_id != _configuration["Google:client_id"])
                    {
                        return Json(BadRequest());
                    }

                    var redirect_application = new Uri(form.Data.redirect_uri).AbsolutePath.Split('/')
                        .Where(item => !string.IsNullOrEmpty(item))
                        .Skip(1)
                        .Take(1)
                        .FirstOrDefault();

                    if (redirect_application != _configuration["Google:client_reference"])
                    {
                        return Json(BadRequest());
                    }

                    var auth = new GoogleAuth
                    {
                        redirect_uri = form.Data.redirect_uri,
                        client_id = form.Data.client_id,
                        state = form.Data.state
                    };

                    var authEntity = new AuthEntity
                    {
                        Data = auth
                    };

                    _googleContext.GoogleAuths.Add(authEntity);
                    await _googleContext.SaveChangesAsync().ConfigureAwait(false);

                    return Json(new RegisterResponse
                    {
                        message = ResponseState.Success,
                        success = true,
                        Data = auth,
                        request = RequestState.Final
                    });
                }
            case RequestState.Final:
                {
                    var authEntity = await _googleContext.GoogleAuths.Include(auth => auth.Owner).FirstOrDefaultAsync(auth => auth.Data.state == form.Data.state);
                    if (authEntity == null)
                    {
                        return Json(BadRequest());
                    }

                    CarEntity leaf = new CarEntity(form.NissanUsername, form.NissanPassword);
                    var leafId = await StorageManager.UserStorage.DoCredentialsMatch(form.NissanUsername, form.NissanPassword, true);
                    if (leafId != Guid.Empty)
                    {
                        leaf = await StorageManager.UserStorage.RestoreUser(leafId) ?? leaf;
                    }

                    var response = new RegisterResponse
                    {
                        Data = authEntity.Data
                    };

                    if (await SessionManager.AddAsync(leaf))
                    {
                        var authCode = Guid.NewGuid();

                        authEntity.AuthCode = authCode;
                        authEntity.Owner = leaf;

                        if (!await _googleContext.NissanLeafs.AnyAsync(car => car.CarModelId == leaf.CarModelId))
                        {
                            await _googleContext.NissanLeafs.AddAsync(leaf);
                        }

                        _googleContext.Entry(authEntity).State = EntityState.Modified;
                        await _googleContext.SaveChangesAsync();

                        response.success = true;
                        response.Data = form.Data;
                        response.message = ResponseState.Success;
                        response.code = authCode;
                    }
                    else
                    {
                        response.success = false;
                        response.message = ResponseState.InvalidCredentials;
                    }

                    return Json(response);
                }
            default:
                return Json(new RegisterResponse { message = ResponseState.BadRequest, success = false });
        }
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