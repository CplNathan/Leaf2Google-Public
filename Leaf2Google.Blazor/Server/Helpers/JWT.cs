// Copyright (c) Nathan Ford. All rights reserved. JWTSecurityToken.cs

using Leaf2Google.Entities.Google;
using Leaf2Google.Models.Car.Sessions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json.Nodes;
using System.Web;

namespace Leaf2Google.Blazor.Server.Helpers
{
    public class GoogleAuthorizeAttribute : TypeFilterAttribute
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
            bool isAuthenticated = context?.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
            if (!isAuthenticated)
            {
                context.Result = JWT.UnauthorizedResponse();
            }
        }
    }

    public static class JWT
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

        public static JsonResult UnauthorizedResponse()
        {
            return new JsonResult(new JsonObject { { "error", "invalid_grant" } })
            {
                ContentType = "application/json",
                StatusCode = StatusCodes.Status401Unauthorized
            };
        }

        public static JwtSecurityToken CreateJWT(VehicleSessionBase session, IConfiguration _configuration, AuthEntity? authEntity = null)
        {
            var secretkey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_configuration["jwt:key"] ?? Guid.NewGuid().ToString())); // NOTE: SAME KEY AS USED IN Program.cs FILE
            var credentials = new SigningCredentials(secretkey, SecurityAlgorithms.HmacSha256);

            var jti = new List<string>() { session.SessionId.ToString(), authEntity?.AuthId.ToString() };
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, session.Username),
                new Claim(JwtRegisteredClaimNames.Sub, session.Username),
                new Claim(JwtRegisteredClaimNames.Email, session.Username),
                new Claim(JwtRegisteredClaimNames.Jti, string.Join(",", jti))
            };

            return new JwtSecurityToken(issuer: IsDebugRelease ? "localhost" : _configuration["fido2:serverDomain"], audience: IsDebugRelease ? "localhost" : _configuration["fido2:serverDomain"], claims: claims, expires: DateTime.UtcNow.AddMinutes(60), signingCredentials: credentials);
        }
    }
}
