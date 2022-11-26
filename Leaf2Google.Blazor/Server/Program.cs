using Leaf2Google.Controllers;
using Leaf2Google.Services.Car;
using Leaf2Google.Services.Google;
using Leaf2Google.Services.Google.Devices;
using Leaf2Google.Services.Helpers;
using Leaf2Google.Entities.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Leaf2Google.Controllers.API;

namespace Leaf2Google.Blazor
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            // Add sensitive config variables from docker environment variables
            builder.Configuration.AddEnvironmentVariables();

            // Use listen address specified in environment variable
            builder.WebHost.UseUrls(builder.Configuration["APPLICATION_URL"]);

            builder.Services.AddDbContext<LeafContext>(options => options
                //.UseLazyLoadingProxies()
                .UseSqlServer(builder.Configuration[$"ConnectionStrings:{(builder.Environment.IsDevelopment() ? "Test" : "Live")}"])
            );
            builder.Services.AddHttpClient<BaseController>(c =>
            {
                //c.BaseAddress = new Uri(builder.Configuration["Nissan:EU:auth_base_url"]);
            });

            builder.Services.AddFido2(options =>
            {
                options.ServerDomain = builder.Environment.IsDevelopment() ? "localhost" : builder.Configuration["fido2:serverDomain"];
                options.ServerName = "Leaf2Google";
                options.Origins = builder.Configuration.GetSection("fido2:origins").Get<HashSet<string>>();
                options.TimestampDriftTolerance = builder.Configuration.GetValue<int>("fido2:timestampDriftTolerance");
                options.MDSCacheDirPath = builder.Configuration["fido2:MDSCacheDirPath"];
            });

            var jwtKey = builder.Configuration["jwt:key"] = Guid.NewGuid().ToString();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
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

            builder.Services.AddSingleton<SessionStorageContainer>();

            builder.Services.AddScoped<BaseStorageService>();
            builder.Services.AddScoped<IUserStorage, UserStorage>();

            builder.Services.AddScoped<ICarSessionManager, LeafSessionService>();
            builder.Services.AddScoped<GoogleStateService>();

            builder.Services.AddScoped<IDevice, LockDeviceService>();
            builder.Services.AddScoped<IDevice, ThermostatDeviceService>();

            builder.Services.AddTransient<Captcha>();
            builder.Services.AddTransient<LoggingService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
                app.UseHttpsRedirection();
            }
            else
            {
                app.UseWebAssemblyDebugging();
            }

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseRouting();

            app.MapRazorPages();
            app.MapControllers();
            app.MapFallbackToFile("index.html");

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseAuthorization();

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