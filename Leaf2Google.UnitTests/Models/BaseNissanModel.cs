// Copyright (c) Nathan Ford. All rights reserved. Class1.cs

using Leaf2Google.Models.Car;
using NUnit.Framework;

namespace Leaf2Google.UnitTests.Models
{
    [TestFixture]
    public class VehicleSessionBase_Tests
    {
        private VehicleSessionBase _vehicleSession;

        public const string _username = "TestUser";

        public const string _password = "TestPassword";

        public static IEnumerable<string[]> CredentialsSource()
        {
            yield return new string[] { "TestUser", "TestPassword1" };
            yield return new string[] { "TestUser ", "TestPassword2" };
            yield return new string[] { "TestUser1", "TestPassword " };
            yield return new string[] { "TestUser2", "TestPassword$" };
        }

        [SetUp]
        public void SetUp()
        {
            // Arrange
            _vehicleSession = new VehicleSessionBase(_username, _password, Guid.NewGuid());
            _vehicleSession.AuthenticatedAccessToken = Guid.NewGuid().ToString();
        }

        [TestCase]
        public void Authenticated_WithLastRequestFailed_ReturnsTrue()
        {
            // Act
            _vehicleSession.LastRequestSuccessful = false;

            // Assert
            Assert.IsFalse(_vehicleSession.Authenticated);
        }
    }
}