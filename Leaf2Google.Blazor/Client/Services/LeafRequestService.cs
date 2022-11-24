using Leaf2Google.Models.Google;
using Leaf2Google.Models.Car;
using System.Net.Http.Json;
using System.Drawing;
using System.Text.Json.Nodes;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;

public delegate void AuthenticationEventHandler(object? sender, bool authenticated);

public interface IAuthService
{
    public static AuthenticationEventHandler OnAuthentication;
    public Task<LoginResponse?> Login(LoginModel loginModel);
    public Task<RegisterResponse?> Register(RegisterModel registerRequest);
    public Task Logout();
    public Task<CurrentUser?> CurrentUserInfo();
}

public interface IRequestService
{
    public Task<ActionResponse?> PerformAction<T>(T actionModel);

    public Task<T?> PerformQuery<T>(QueryType queryType);
}

public static class LeafHttpExtensions
{
    private static void UpdateBearer(this HttpClient httpClient, string? jwtBearer)
    {
        httpClient.DefaultRequestHeaders.Remove("Authorization");
        if (!string.IsNullOrEmpty(jwtBearer))
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + jwtBearer);

        // TODO: store in session storage (use microsoft apis)
    }
} 

public class LeafAuthService : IAuthService
{
    private readonly HttpClient _httpClient;

    public LeafAuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LoginResponse?> Login(LoginModel loginModel)
    {
        HttpResponseMessage loginResult = await _httpClient.PostAsJsonAsync<LoginModel>("/API/Authentication/Login", loginModel);
        LoginResponse? result = await loginResult.Content.ReadFromJsonAsync<LoginResponse>();

        if (loginResult.IsSuccessStatusCode && (result?.success ?? false))
        {
            //_httpClient.UpdateBearer(result?.jwtBearer);
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            if (!string.IsNullOrEmpty(result?.jwtBearer))
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + result?.jwtBearer);

            IAuthService.OnAuthentication?.Invoke(this, true);

            return result;
        }
        else
        {
            IAuthService.OnAuthentication?.Invoke(this, false);

            return null;
        }
    }

    public async Task<CurrentUser?> CurrentUserInfo()
    {
        try
        {
            HttpResponseMessage userResult = await _httpClient.PostAsync("/API/Authentication/UserInfo", null);
            if (userResult.IsSuccessStatusCode)
            {
                CurrentUser? result = await userResult.Content.ReadFromJsonAsync<CurrentUser>();
                return result;
            }
            else
            {
                return new();
            }
        }
        catch
        {
            return new();
        }
    }

    public async Task Logout()
    {
        throw new NotImplementedException();
    }

    public async Task<RegisterResponse?> Register(RegisterModel registerRequest)
    {
        throw new NotImplementedException();
    }
} 

public class LeafRequestService : IRequestService
{
    private readonly HttpClient _httpClient;

    private readonly IAuthService _authService;

    public LeafRequestService(HttpClient httpClient, IAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    public async Task<ActionResponse?> PerformAction<T>(T actionModel)
    {
        HttpResponseMessage actionResult = await _httpClient.PostAsJsonAsync<T>("API/Car/Action", actionModel);

        if (actionResult.IsSuccessStatusCode)
        {
            ActionResponse? result = await actionResult.Content.ReadFromJsonAsync<ActionResponse>();
            return result;
        }
        else if (actionResult.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await _authService.Logout();
            return null;
        }
        else
        {
            throw new InvalidOperationException("Action performed before first authentication.");
        }
    }

    public async Task<T?> PerformQuery<T>(QueryType queryType)
    {
        var queryModel = new QueryRequest()
        {
            QueryType = queryType,
            ActiveVin = null//PrimaryVin
        };

        var actionResult = await _httpClient.PostAsJsonAsync<QueryRequest>("API/Car/Query", queryModel);

        if (actionResult.IsSuccessStatusCode)
        {
            T? result = await actionResult.Content.ReadFromJsonAsync<T>();
            return result;
        }
        else if (actionResult.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await _authService.Logout();
            return default(T);
        }
        else
        {
            throw new InvalidOperationException("Action performed before first authentication.");
        }
    }

    protected async Task<string?> GetPrimaryVin()
    {
        return await PerformQuery<string?>(QueryType.PrimaryVin);
    }

    public async Task<PointF> GetLocation()
    {
        var result = await PerformQuery<JsonObject>(QueryType.Location);

        return new PointF()
        {
            X = result["lat"]?.GetValue<float>() ?? 0,
            Y = result["long"]?.GetValue<float>() ?? 0
        };
    }

    public async Task<string> GetPhoto()
    {
        return await PerformQuery<string?>(QueryType.Photo) ?? "";
    }

    public async Task<BatteryData?> GetBattery()
    {
        return await PerformQuery<BatteryData?>(QueryType.Battery);
    }
}
