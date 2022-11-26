// Copyright (c) Nathan Ford. All rights reserved. JWTSecurityToken.cs

using Leaf2Google.Models.Car.Sessions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Leaf2Google.Blazor.Server.Helpers
{
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

        public static JwtSecurityToken CreateJWT(VehicleSessionBase session, IConfiguration _configuration)
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

            return new JwtSecurityToken(issuer: IsDebugRelease ? "localhost" : _configuration["fido2:serverDomain"], audience: IsDebugRelease ? "localhost" : _configuration["fido2:serverDomain"], claims: claims, expires: DateTime.UtcNow.AddMinutes(60), signingCredentials: credentials);
        }
    }
}
