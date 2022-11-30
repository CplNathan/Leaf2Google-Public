// Copyright (c) Nathan Ford. All rights reserved. Captcha.cs

using System.Text.Json.Nodes;

namespace Leaf2Google.Services.Helpers;

public class Captcha
{
    public Captcha(HttpClient client, IConfiguration configuration)
    {
        Client = client;
        Configuration = configuration;
    }

    public HttpClient Client { get; }

    public IConfiguration Configuration { get; }

    public string? CaptchaKey => Configuration.GetValue<string>("Google:captcha:secret_key") ?? null;

    public bool UsingCaptcha => !string.IsNullOrEmpty(CaptchaKey);

    public async Task<bool> VerifyCaptcha(string response, string? remoteip)
    {
        if (!UsingCaptcha)
        {
            return true;
        }

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "recaptcha/api/siteverify")
        {
            Headers =
            {
                { "Accept", "application/x-www-form-urlencoded" }
            },
            Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", CaptchaKey ?? ""),
                new KeyValuePair<string, string>("response", response),
                new KeyValuePair<string, string>("remoteip", remoteip ?? "")
            }),
            RequestUri = new Uri("https://www.google.com/recaptcha/api/siteverify")
        };

        var responseData = await Client.MakeRequest<JsonObject>(httpRequestMessage);

        return responseData?.Data?["success"]?.GetValue<bool?>() ?? false;
    }
}