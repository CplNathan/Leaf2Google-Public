using System.Drawing;

namespace Leaf2Google.Models.Car
{
    public delegate void AuthEventHandler(object sender, string? authToken);

    public delegate void RequestEventHandler(object sender, bool requestSuccess);

    public class VehicleSessionBase
    {
        private string? _authenticatedAccessToken = string.Empty;

        public string? AuthenticatedAccessToken
        {
            get
            {
                return _authenticatedAccessToken;
            }
            set
            {
                if (_authenticatedAccessToken != value)
                    OnAuthenticationAttempt?.Invoke(this, value);

                _authenticatedAccessToken = value;
            }
        }

        public bool Authenticated { get => !string.IsNullOrEmpty(_authenticatedAccessToken) && LastRequestSuccessful; }

        public string Username { get; init; }
        public string Password { get; init; }
        public Guid SessionId { get; init; }

        public Tuple<DateTime, PointF?> LastLocation { get; set; } = Tuple.Create<DateTime, PointF?>(DateTime.MinValue, null);

        public string? CarPictureUrl { get; set; }

        public string? PrimaryVin { get => VINs.FirstOrDefault(); }

        public List<string?> VINs { get; init; } = new List<string?>();

        public bool LastRequestSuccessful { get; protected set; }

        public int LoginFailedCount { get; protected set; }

        public bool LoginGivenUp { get => LoginFailedCount >= 5; }

        public event RequestEventHandler OnRequest;

        public event AuthEventHandler OnAuthenticationAttempt;

        public VehicleSessionBase(string username, string password, Guid sessionId)
        {
            this.Username = username;
            this.Password = password;
            this.SessionId = sessionId;

            OnRequest += VehicleSessionBase_OnRequest;
            OnAuthenticationAttempt += VehicleSessionBase_OnAuthenticationAttempt;
        }

        public void Invoke_OnRequest(bool requestSuccess)
        {
            OnRequest?.Invoke(this, requestSuccess);
        }

        private void VehicleSessionBase_OnRequest(object? sender, bool requestSuccess)
        {
            LastRequestSuccessful = requestSuccess;
        }

        private void VehicleSessionBase_OnAuthenticationAttempt(object? sender, string? authToken)
        {
            LoginFailedCount = Authenticated ? 0 : LoginFailedCount++;
        }
    }

    public class NissanConnectSession : VehicleSessionBase
    {
        public NissanConnectSession(string username, string password, Guid sessionId)
            : base(username, password, sessionId)
        {
        }
    }
}