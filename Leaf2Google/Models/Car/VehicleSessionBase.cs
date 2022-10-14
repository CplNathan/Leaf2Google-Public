using System.Drawing;
using Microsoft.IdentityModel.Tokens;

namespace Leaf2Google.Models.Car;

public class VehicleSessionBase
{
    private string? _authenticatedAccessToken = string.Empty;

    private bool _loginAuthenticationAttempting = false;

    public VehicleSessionBase(string username, string password, Guid sessionId)
    {
        Username = username;
        Password = password;
        SessionId = sessionId;
    }

    public string? AuthenticatedAccessToken
    {
        get => _authenticatedAccessToken;
        set
        {
            if (!value.IsNullOrEmpty() && value != _authenticatedAccessToken)
                LastAuthenticated = DateTime.UtcNow;

            _authenticatedAccessToken = value;
        }
    }

    public DateTime LastAuthenticated { get; private set; } = DateTime.MinValue;

    public bool Authenticated => !string.IsNullOrEmpty(_authenticatedAccessToken) && LastRequestSuccessful;

    public string Username { get; init; }
    public string Password { get; init; }
    public Guid SessionId { get; init; }

    public Tuple<DateTime, PointF?> LastLocation { get; set; } =
        Tuple.Create<DateTime, PointF?>(DateTime.MinValue, null);

    public string? CarPictureUrl { get; set; }

    public string? PrimaryVin => VINs.FirstOrDefault();

    public List<string?> VINs { get; init; } = new();

    public bool LastRequestSuccessful { get; set; } = true;

    public bool LoginAuthenticationAttempting
    {
        get => _loginAuthenticationAttempting;
        set
        {
            if (value != _loginAuthenticationAttempting && value == false)
                LastLoginAuthenticaionAttempted = DateTime.UtcNow;

            _loginAuthenticationAttempting = value;
        }
    }

    public DateTime LastLoginAuthenticaionAttempted { get; private set; } = DateTime.MinValue;

    public int LoginFailedCount { get; set; }

    public bool LoginGivenUp => LoginFailedCount >= 5;
}

public class NissanConnectSession : VehicleSessionBase
{
    public NissanConnectSession(string username, string password, Guid sessionId)
        : base(username, password, sessionId)
    {
    }
}