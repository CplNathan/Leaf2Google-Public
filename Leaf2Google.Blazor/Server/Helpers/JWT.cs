// Copyright (c) Nathan Ford. All rights reserved. JWT.cs

using Leaf2Google.Entities.Google;
using Leaf2Google.Models.Car.Sessions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json.Nodes;

namespace Leaf2Google.Blazor.Server.Helpers
{
    public sealed class GoogleAuthorizeAttribute : TypeFilterAttribute
    {
        public GoogleAuthorizeAttribute() : base(typeof(GoogleAuthorizeFilter))
        {
        }
    }

    public class GoogleAuthorizeFilter : IAuthorizationFilter
    {
        public GoogleAuthorizeFilter()
        {
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context is null)
            {
                throw new InvalidOperationException();
            }

            bool isAuthenticated = context.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
            if (!isAuthenticated)
            {
                context!.Result = JWTHelper.UnauthorizedResponse(StatusCodes.Status401Unauthorized);
            }
        }
    }

    public static class JWTHelper
    {
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

        public static JsonResult UnauthorizedResponse(int code = StatusCodes.Status401Unauthorized)
        {
            return new JsonResult(new JsonObject { { "error", "invalid_grant" } })
            {
                ContentType = "application/json",
                StatusCode = StatusCodes.Status401Unauthorized
            };
        }

        // Quick hack
        public static JwtSecurityToken CreateJWT(VehicleSessionBase session, IConfiguration _configuration, AuthEntity? authEntity = null, DateTime? validTo = null)
        {
            if (session == null)
                throw new InvalidOperationException("Session is not valid");

            validTo = validTo ?? DateTime.UtcNow.AddMinutes(60);

            var secretkey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_configuration["jwt:key"] ?? Guid.NewGuid().ToString())); // NOTE: SAME KEY AS USED IN Program.cs FILE
            var credentials = new SigningCredentials(secretkey, SecurityAlgorithms.HmacSha256);

            var jti = new List<string>() { session.SessionId.ToString(), authEntity?.AuthId.ToString() ?? "" };
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, session.Username),
                new Claim(JwtRegisteredClaimNames.Sub, session.Username),
                new Claim(JwtRegisteredClaimNames.Email, session.Username),
                new Claim(JwtRegisteredClaimNames.Jti, string.Join(",", jti.Where(val => !string.IsNullOrEmpty(val))))
            };

            return new JwtSecurityToken(issuer: IsDebugRelease ? "localhost" : _configuration["fido2:serverDomain"], audience: IsDebugRelease ? "localhost" : _configuration["fido2:serverDomain"], claims: claims, expires: validTo, signingCredentials: credentials);
        }
    }
}
