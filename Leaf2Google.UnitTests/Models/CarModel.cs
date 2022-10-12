// Copyright (c) Nathan Ford. All rights reserved. Class1.cs

using Leaf2Google.Models.Car;
using Leaf2Google.UnitTests;
using NUnit.Framework;

namespace Leaf2Google.UnitTests.Models
{
    [TestFixture]
    public class CarModel_Tests
    {
        private CarModel _carModel;

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
            // Arrange / Act
            // Each new instance has a new IV/Key so to ensure that testing is consistent use a single instance.
            _carModel = new CarModel(_username, _password);
        }

        // This test now may be redundant as the DbSet has a property converter assigned to it instead - the StorageManager test replaces this.

        [Description("When CarModel is constructed it takes the password, transforms it to bytes, then encrypts it. This test proves the reverse through the NissanPassword getter.")]
        [TestCase(_username, _password)]
        public void NissanPassword_WithValidCredentials_IsEqual(string username, string password)
        {
            // Assert
            Assert.AreEqual(_carModel.NissanPassword, password);
        }

        [Description("Ensures that only the originally constructed credentials are matched by previding some negative credentials.")]
        [TestCaseSource(nameof(CredentialsSource))]
        public void NissanPassword_WithInvalidCredentials_IsNotEqual(string username, string password)
        {
            // Assert
            Assert.AreNotEqual(_carModel.NissanPassword, password);
        }
    }
}