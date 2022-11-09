using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Leaf2Google.Models.Car
{

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
                if (!string.IsNullOrEmpty(value) && value != _authenticatedAccessToken)
                    LastAuthenticated = DateTime.UtcNow;

                _authenticatedAccessToken = value;
            }
        }

        public DateTime LastAuthenticated { get; private set; } = DateTime.MinValue;

        public bool Authenticated => !string.IsNullOrEmpty(_authenticatedAccessToken) && LastRequestSuccessful && !string.IsNullOrEmpty(PrimaryVin);

        public string Username { get; }
        public string Password { get; }
        public Guid SessionId { get; }

        public Tuple<DateTime, PointF?> LastLocation { get; set; } =
            Tuple.Create<DateTime, PointF?>(DateTime.MinValue, null);

        public string? CarPictureUrl { get; set; }

        public string? PrimaryVin => VINs.FirstOrDefault();

        public List<string?> VINs { get; } = new List<string?>();

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

        public bool LoginGivenUp => false;//LoginFailedCount >= 10;
    }

    public class NissanConnectSession : VehicleSessionBase
    {
        public NissanConnectSession(string username, string password, Guid sessionId)
            : base(username, password, sessionId)
        {
        }
    }
}