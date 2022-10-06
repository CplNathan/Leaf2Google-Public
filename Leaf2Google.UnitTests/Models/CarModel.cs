// Copyright (c) Nathan Ford. All rights reserved. Class1.cs

using Leaf2Google.Models.Car;
using NUnit.Framework;

namespace Leaf2Google.UnitTests.Models
{
    [TestFixture]
    public class CarModel_Password
    {
        private CarModel _carModel;

        private const string _username = "TestUser";

        private const string _password = "TestPassword";

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

        [Description("When CarModel is constructed it takes the password, transforms it to bytes, then encrypts it. This test proves the reverse through the NissanPassword getter.")]
        [TestCase(_username, _password)]
        public void WhenGetPassword_ThenReturnEncryptedTransformedPassword(string username, string password)
        {
            // Assert
            Assert.AreEqual(_carModel.NissanPassword, password);
        }

        [Description("Ensures that only the originally constructed credentials are matched by previding some negative credentials.")]
        [TestCaseSource(nameof(CredentialsSource))]
        public void WhenGetPassword_ThenEnsureOnlyMatchingPassword(string username, string password)
        {
            // Assert
            Assert.AreNotEqual(_carModel.NissanPassword, password);
        }
    }
}