// Copyright (c) Nathan Ford. All rights reserved. BaseStorageManager.cs

using Leaf2Google.Contexts;
using Leaf2Google.Entities.Car;
using Leaf2Google.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Leaf2Google.UnitTests.Dependency
{
    [TestFixture]
    public class BaseStorageManager_Tests
    {
        private DbContextOptions<LeafContext> _options;
        private LeafContext _leafContext;
        private BaseStorageService _storageManager;

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
            _storageManager = new BaseStorageService(_leafContext, _userStorage, null);
        }

        [TearDown]
        public void TearDown()
        {
            _leafContext.Dispose();
        }

        [TestCaseSource(nameof(InvalidCredentialsSource))]
        public async Task Login_WithInvalidValid_ReturnsEmpty(string username, string password)
        {
            // Act
            using (var context = new LeafContext(_options))
            {
                await context.NissanLeafs.AddAsync(new CarEntity(_username, _password));
                await context.SaveChangesAsync();
            }

            // Assert
            Assert.AreEqual(Guid.Empty, await _storageManager.UserStorage.LoginUser(username, password));
        }

        [TestCaseSource(nameof(ValidCredentialsSource))]
        public async Task Login_WithValid_ReturnsGuid(string username, string password)
        {
            // Act
            var expectedGuid = Guid.NewGuid();
            using (var context = new LeafContext(_options))
            {
                await context.NissanLeafs.AddAsync(new CarEntity(_username, _password) { CarModelId = expectedGuid });
                await context.SaveChangesAsync();
            }

            // Assert
            Assert.AreEqual(expectedGuid, await _storageManager.UserStorage.LoginUser(username, password));
        }

        // Slow Implementation :|
        [TestCaseSource(nameof(ValidCredentialsSource))]
        public async Task Login_WithDeleted_ReturnsEmpty(string username, string password)
        {
            // Act
            using (var context = new LeafContext(_options))
            {
                await context.NissanLeafs.AddAsync(new CarEntity(_username, _password) { Deleted = DateTime.UtcNow });
                await context.SaveChangesAsync();
            }

            // Assert
            Assert.AreEqual(Guid.Empty, await _storageManager.UserStorage.LoginUser(username, password));
        }
    }
}
