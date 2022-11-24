// Copyright (c) Nathan Ford. All rights reserved. BaseAPIController.cs

using Leaf2Google.Blazor.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Leaf2Google.Controllers.API;

// Instances of this type get picked up by reflection and rendered on the frontend as an endpoint object.
public class BaseAPIController : BaseController
{
    protected ICarSessionManager SessionManager { get; }

    public BaseAPIController(BaseStorageService storageManager, ICarSessionManager sessionManager)
        : base(storageManager)
    {
        SessionManager = sessionManager;
    }
}

[Route("/WeatherForecast")]
[ApiController]
public class WeatherController : BaseAPIController
{
    public WeatherController(BaseStorageService storageManager, ICarSessionManager sessionManager)
    : base(storageManager, sessionManager)
    {
    }

    [HttpGet]
    public WeatherForecast[] WeatherData()
    {
        return Enumerable.Range(1, 4).Select(i => new WeatherForecast() {  Date = DateOnly.FromDateTime(DateTime.Now), Summary = i.ToString(), TemperatureC = i }).ToArray();
    }
}