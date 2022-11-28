// Copyright (c) Nathan Ford. All rights reserved. LeafAuthenticationStateProvider.cs

using Leaf2Google.Models.Google;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace Leaf2Google.Blazor.Client.Services
{
    public class LeafAuthenticationStateService : AuthenticationStateProvider
    {
        private readonly IAuthService api;
        private CurrentUser? _currentUser;
        public LeafAuthenticationStateService(IAuthService api)
        {
            this.api = api;
        }
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity();
            try
            {
                var userInfo = await GetCurrentUser();
                if (userInfo?.IsAuthenticated ?? false)
                {
                    var claims = /* new[] {
                        new Claim(ClaimTypes.Name, _currentUser.NissanUsername),
                        new Claim(JwtRegisteredClaimNames.Sub, _currentUser.NissanUsername),
                        new Claim(JwtRegisteredClaimNames.Email, _currentUser.NissanUsername),
                        new Claim(JwtRegisteredClaimNames.Jti, _currentUser.SessionId.ToString())
                    }*/new List<Claim>().Concat(_currentUser?.Claims?.Select(c => new Claim(c.Key, c.Value)) ?? new List<Claim>());
                    identity = new ClaimsIdentity(claims, "Server authentication");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Request failed:" + ex.ToString());
            }
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        private async Task<CurrentUser?> GetCurrentUser()
        {
            if (_currentUser != null && _currentUser.IsAuthenticated) return _currentUser;
            _currentUser = await api.CurrentUserInfo();
            return _currentUser;
        }
        public async Task Logout()
        {
            await api.Logout();
            _currentUser = null;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
        public async Task<LoginResponse?> Login(LoginModel loginParameters, bool notifyOnlyIfSuccess = false)
        {
            var result = await api.Login(loginParameters);

            if (result?.success ?? false && notifyOnlyIfSuccess || !notifyOnlyIfSuccess)
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            return result;
        }
        public async Task<RegisterResponse?> Register(RegisterModel registerParameters, bool notifyOnlyIfSuccess = false)
        {
            var result = await api.Register(registerParameters);
            if (result?.success ?? false && notifyOnlyIfSuccess || !notifyOnlyIfSuccess)
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            return result;
        }
    }
}
