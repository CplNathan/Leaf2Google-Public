// Copyright (c) Nathan Ford. All rights reserved. Class1.cs

using Leaf2Google.Models.Car;
using NUnit.Framework;

namespace Leaf2Google.UnitTests.Models
{
    [TestFixture]
    public class VehicleSessionBase_Authenticated
    {
        private VehicleSessionBase _vehicleSession;

        private const string _username = "TestUser";

        private const string _password = "TestPassword";

        [SetUp]
        public void SetUp()
        {
            // Arrange
            _vehicleSession = new VehicleSessionBase(_username, _password, Guid.NewGuid());
            _vehicleSession.AuthenticatedAccessToken = Guid.NewGuid().ToString();
        }

        [TestCase]
        public void WhenRequestFails_ThenEnsureAuthenticationFalse()
        {
            // Act
            _vehicleSession.LastRequestSuccessful = false;

            // Assert
            Assert.IsFalse(_vehicleSession.Authenticated);
        }
    }

    [TestFixture]
    public class VehicleSessionBase_Authenticat
    {
        private VehicleSessionBase _vehicleSession;

        private const string _username = "TestUser";

        private const string _password = "TestPassword";

        [SetUp]
        public void SetUp()
        {
            // Arrange
            _vehicleSession = new VehicleSessionBase(_username, _password, Guid.NewGuid());
            _vehicleSession.AuthenticatedAccessToken = Guid.NewGuid().ToString();
        }

        [TestCase]
        public void WhenRequestFails_ThenEnsureAuthenticationFalse()
        {
            // Act
            _vehicleSession.LastRequestSuccessful = false;

            // Assert
            Assert.IsFalse(_vehicleSession.Authenticated);
        }
    }
}