// Copyright (c) Nathan Ford. All rights reserved. Program.cs

using Leaf2Google.Controllers;
using Leaf2Google.Entities.Security;
using Leaf2Google.Services.Car;
using Leaf2Google.Services.Google;
using Leaf2Google.Services.Google.Devices;
using Leaf2Google.Services.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Leaf2Google.Blazor
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            _ = builder.Services.AddControllersWithViews();
            _ = builder.Services.AddRazorPages();

            // Add sensitive config variables from docker environment variables
            _ = builder.Configuration.AddEnvironmentVariables();

            // Use listen address specified in environment variable
            _ = builder.WebHost.UseUrls(builder.Configuration["APPLICATION_URL"]);

            _ = builder.Services.AddDbContext<LeafContext>(options => options
                //.UseLazyLoadingProxies()
                .UseNpgsql(builder.Configuration[$"ConnectionStrings:{(builder.Environment.IsDevelopment() ? "Test" : "Live")}"])
            //.UseSqlServer(builder.Configuration[$"ConnectionStrings:{(builder.Environment.IsDevelopment() ? "Test" : "Live")}"])
            );
            _ = builder.Services.AddHttpClient<BaseController>(c =>
            {
                //c.BaseAddress = new Uri(builder.Configuration["Nissan:EU:auth_base_url"]);
            });

            _ = builder.Services.AddFido2(options =>
            {
                options.ServerDomain = builder.Environment.IsDevelopment() ? "localhost" : builder.Configuration["fido2:serverDomain"];
                options.ServerName = "Leaf2Google";
                options.Origins = builder.Configuration.GetSection("fido2:origins").Get<HashSet<string>>();
                options.TimestampDriftTolerance = builder.Configuration.GetValue<int>("fido2:timestampDriftTolerance");
                options.MDSCacheDirPath = builder.Configuration["fido2:MDSCacheDirPath"];
            });

            var jwtKey = builder.Configuration["jwt:key"] = Guid.NewGuid().ToString();
            _ = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidAudience = builder.Environment.IsDevelopment() ? "localhost" : builder.Configuration["fido2:serverDomain"],
                    ValidateIssuer = true,
                    ValidIssuer = builder.Environment.IsDevelopment() ? "localhost" : builder.Configuration["fido2:serverDomain"],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey)) // Static value will allow sessions to persist accross restart
                };
            });

            _ = builder.Services.AddSingleton<SessionStorageContainer>();

            _ = builder.Services.AddScoped<BaseStorageService>();
            _ = builder.Services.AddScoped<IUserStorage, UserStorage>();

            _ = builder.Services.AddScoped<ICarSessionManager, LeafSessionService>();
            _ = builder.Services.AddScoped<GoogleStateService>();

            _ = builder.Services.AddScoped<IDevice, LockDeviceService>();
            _ = builder.Services.AddScoped<IDevice, ThermostatDeviceService>();

            _ = builder.Services.AddTransient<Captcha>();
            _ = builder.Services.AddTransient<LoggingService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                _ = app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                _ = app.UseHsts();
                _ = app.UseHttpsRedirection();
            }
            else
            {
                app.UseWebAssemblyDebugging();
            }

            _ = app.UseBlazorFrameworkFiles();
            _ = app.UseStaticFiles();

            _ = app.UseAuthentication();

            _ = app.UseRouting();

            _ = app.MapRazorPages();
            _ = app.MapControllers();
            _ = app.MapFallbackToFile("index.html");

            _ = app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            _ = app.UseAuthorization();

            using (var scope = app.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<LeafContext>().Database.Migrate();
                await scope.ServiceProvider.GetRequiredService<ICarSessionManager>().StartAsync().ConfigureAwait(false);
            }

            await app.RunAsync();
        }
    }
}

namespace Leaf2Google.Models.Security
{
    public class StoredCredentialModel : StoredCredential
    {
        public byte[] CredentialId { get; set; }
    }
}