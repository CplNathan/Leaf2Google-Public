// Copyright (c) Nathan Ford. All rights reserved. WeatherForecast.cs

namespace Leaf2Google.Blazor.Shared
{
    public class WeatherForecast
    {
        public DateOnly Date { get; set; }

        public int TemperatureC { get; set; }

        public string? Summary { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}