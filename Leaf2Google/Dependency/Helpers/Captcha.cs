// Copyright (c) Nathan Ford. All rights reserved. CaptchaVerification.cs

using Leaf2Google.Helpers;
using Microsoft.IdentityModel.Protocols;
using Newtonsoft.Json;
using static System.Net.WebRequestMethods;
using System.Reflection.PortableExecutable;
using System.Text;
using System;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Leaf2Google.Dependency.Helpers
{
    public class Captcha
    {
        private readonly HttpClient _client;

        private readonly IConfiguration _configuration;

        public HttpClient Client => _client;

        public IConfiguration Configuration => _configuration;

        public Captcha(HttpClient client, IConfiguration configuration)
        {
            _client = client;
            _configuration = configuration;
        }

        public async Task<bool> VerifyCaptcha(string response, string? remoteip)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"recaptcha/api/siteverify")
            {
                Headers =
                {
                    { "Accept", "application/x-www-form-urlencoded" }
                },
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("secret", Configuration["Google:captcha:secret_key"]),
                    new KeyValuePair<string, string>("response", response),
                    new KeyValuePair<string, string>("remoteip", remoteip)
                })
            };
            httpRequestMessage.RequestUri = new Uri("https://www.google.com/recaptcha/api/siteverify");

            var responseData = await Client.MakeRequest(httpRequestMessage);

            return responseData.Data?.success ?? false;
        }
    }
}
