using System.Drawing;

namespace Leaf2Google.Models.Car
{
    public class VehicleSessionBase
    {
        public object _authLock = new object();

        private string? _authenticatedAccessToken = string.Empty;

        public string? AuthenticatedAccessToken
        {
            get
            {
                return _authenticatedAccessToken;
            }
            set
            {
                if (!value.IsNullOrEmpty() && value != _authenticatedAccessToken)
                    _lastAuthenticated = DateTime.UtcNow;

                _authenticatedAccessToken = value;
            }
        }

        private DateTime _lastAuthenticated = DateTime.MinValue;

        public DateTime LastAuthenticated { get => _lastAuthenticated; }


        public bool Authenticated { get => !string.IsNullOrEmpty(_authenticatedAccessToken) && LastRequestSuccessful; }

        public string Username { get; init; }
        public string Password { get; init; }
        public Guid SessionId { get; init; }

        public Tuple<DateTime, PointF?> LastLocation { get; set; } = Tuple.Create<DateTime, PointF?>(DateTime.MinValue, null);

        public string? CarPictureUrl { get; set; }

        public string? PrimaryVin { get => VINs.FirstOrDefault(); }

        public List<string?> VINs { get; init; } = new List<string?>();

        public bool LastRequestSuccessful { get; set; } = true;

        public int LoginFailedCount { get; set; }

        public bool LoginGivenUp { get => LoginFailedCount >= 5; }

        public VehicleSessionBase(string username, string password, Guid sessionId)
        {
            this.Username = username;
            this.Password = password;
            this.SessionId = sessionId;
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