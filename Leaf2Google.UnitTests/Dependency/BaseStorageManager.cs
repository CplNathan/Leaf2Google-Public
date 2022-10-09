// Copyright (c) Nathan Ford. All rights reserved. BaseStorageManager.cs

using Leaf2Google.Contexts;
using Leaf2Google.Dependency;
using Leaf2Google.Models.Car;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leaf2Google.UnitTests.Dependency
{
    [TestFixture]
    public class BaseStorageManager_Tests
    {
        private DbContextOptions<LeafContext> _options;
        private LeafContext _leafContext;
        private BaseStorageManager _storageManager;

        public const string _username = "TestUser";

        public const string _password = "TestPassword";

        public static IEnumerable<string[]> InvalidCredentialsSource()
        {
            yield return new string[] { "TestUser", "TestPassword1" };
            yield return new string[] { "TestUser ", "TestPassword2" };
            yield return new string[] { "TestUser1", "TestPassword " };
            yield return new string[] { "TestUser2", "TestPassword$" };
        }

        public static IEnumerable<string[]> ValidCredentialsSource()
        {
            yield return new string[] { "TestUser", "TestPassword" };
        }

        // Instead of mocking the DbContext I am using an in-memory provider to replicate functionality.
        [SetUp]
        public void SetUp()
        {
            // Arrange
            _options = new DbContextOptionsBuilder<LeafContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

            _leafContext = new LeafContext(_options);
            var _userStorage = new UserStorage(_leafContext);
            _storageManager = new BaseStorageManager(_leafContext, _userStorage);
        }

        [TearDown]
        public void TearDown()
        {
            _leafContext.Dispose();
        }

        [TestCaseSource(nameof(InvalidCredentialsSource))]
        public void Login_WithInvalidValid_ReturnsEmpty(string username, string password)
        {
            // Act
            using (var context = new LeafContext(_options))
            {
                context.NissanLeafs.Add(new CarModel(_username, _password));
                context.SaveChanges();
            }

            // Assert
            Assert.AreEqual(Guid.Empty, _storageManager.UserStorage.LoginUser(username, password).Result);
        }

        [TestCaseSource(nameof(ValidCredentialsSource))]
        public void Login_WithValid_ReturnsGuid(string username, string password)
        {
            // Act
            var expectedGuid = Guid.NewGuid();
            using (var context = new LeafContext(_options))
            {
                context.NissanLeafs.Add(new CarModel(_username, _password) { CarModelId = expectedGuid });
                context.SaveChanges();
            }

            // Assert
            Assert.AreEqual(expectedGuid, _storageManager.UserStorage.LoginUser(username, password).Result);
        }

        [TestCaseSource(nameof(ValidCredentialsSource))]
        public void Login_WithDeleted_ReturnsEmpty(string username, string password)
        {
            // Act
            using (var context = new LeafContext(_options))
            {
                context.NissanLeafs.Add(new CarModel(_username, _password) { Deleted = DateTime.UtcNow });
                context.SaveChanges();
            }

            // Assert
            Assert.AreEqual(Guid.Empty, _storageManager.UserStorage.LoginUser(username, password).Result);
        }
    }
}
