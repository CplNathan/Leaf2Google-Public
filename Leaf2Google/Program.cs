using Leaf2Google.Contexts;
using Leaf2Google.Controllers;
using Leaf2Google.Dependency.Car;
using Leaf2Google.Dependency.Google;
using Leaf2Google.Dependency.Google.Devices;
using Leaf2Google.Dependency.Helpers;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

// Add-Migration NAME

// TODO re-write existing Google API to be better, fit into better MVC flow. Login page etc. JQuery validate tooltips for error messages.

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson();
builder.Services.AddDbContext<LeafContext>(options => options
    .UseLazyLoadingProxies()
    .UseSqlServer(builder.Configuration[$"ConnectionStrings:{(builder.Environment.IsDevelopment() ? "Test" : "Live")}"])
);
builder.Services.AddHttpClient<BaseController>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Nissan:EU:auth_base_url"]);
});
builder.Services.AddSingleton<LeafSessionManager>();
builder.Services.AddSingleton<GoogleStateManager>();
builder.Services.AddScoped<IDevice, LockDevice>();
builder.Services.AddScoped<IDevice, ThermostatDevice>();
builder.Services.AddTransient<Captcha>();

builder.Services.AddWebOptimizer(pipeline =>
{
    pipeline.MinifyCssFiles("css/**/*.css");
    pipeline.MinifyJsFiles("js/**/*.js");
    pipeline.AddCssBundle("/css/bundle.css", "lib/bootstrap/dist/css/bootstrap.min.css", "lib/bootstrap-icons/dist/css/bootstrap-icons.css", "css/**/*.css")
    .MinifyCss();
    pipeline.AddJavaScriptBundle("/js/bundle.js", "lib/jquery/dist/jquery.min.js", "lib/jquery-validation/dist/jquery.validate.min.js", "lib/bootstrap/dist/js/bootstrap.min.js", "js/Components/**/*.js", "js/Partials/**/*.js", "js/site.js");
    pipeline.AddJavaScriptBundle("/js/components-bundle.js", "js/WebComponents/**/*.js");
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseWebOptimizer();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<LeafContext>().Database.Migrate();
}

await app.Services.GetRequiredService<LeafSessionManager>().StartAsync();

app.UseStaticFiles();

app.UseRouting();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();