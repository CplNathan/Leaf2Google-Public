// Copyright (c) Nathan Ford. All rights reserved. Program.cs

using Leaf2Google.Blazor.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Leaf2Google.Blazor.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            _ = builder.Services.AddOptions();
            _ = builder.Services.AddAuthorizationCore();
            _ = builder.Services.AddSingleton(sp => new HttpClient() { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            _ = builder.Services.AddScoped<LeafAuthenticationStateService>();
            _ = builder.Services.AddScoped<AuthenticationStateProvider>(s => s.GetRequiredService<LeafAuthenticationStateService>());
            _ = builder.Services.AddScoped<IAuthService, LeafAuthService>();
            _ = builder.Services.AddScoped<IRequestService, LeafRequestService>();

            await builder.Build().RunAsync();
        }
    }
}

namespace Leaf2Google.Blazor.Shared
{
    public class WeatherForecast
    {
        public DateOnly Date { get; set; }
        public int TemperatureC { get; set; }

        public string Summary { get; set; }
    }
}