// Copyright (c) Nathan Ford. All rights reserved. Class1.cs

using Leaf2Google.Contexts;
using Leaf2Google.Services;
using Leaf2Google.Services.Google;
using Leaf2Google.Services.Google.Devices;
using Leaf2Google.Models.Car;
using NUnit.Framework;

namespace Leaf2Google.UnitTests.Models
{
    [TestFixture]
    public class GoogleStateManager_Tests
    {
        private BaseStorageService _storageService;

        private GoogleStateService _googleStateService;

        private Guid _testGuid;

        private IEnumerable<Type> _testTypes;

        [SetUp]
        public void SetUp()
        {
            // Arrange
            _storageService = new BaseStorageService(null, null, new SessionStorageContainer());
            _googleStateService = new GoogleStateService(_storageService, null);
            _testGuid = Guid.NewGuid();

            // Get all devices, to later ensure they are assigned.
            var type = typeof(IDevice);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface);

            _testTypes = types;
        }

        [TestCase]
        public void GetOrCreateDevices_WithValidGuid_ReturnsDevices()
        {
            // Act
            _googleStateService.GetOrCreateDevices(_testGuid);
            // Assert
            Assert.That(_storageService.GoogleSessions, Does.ContainKey(_testGuid));
        }

        [TestCase]
        public void GetOrCreateDevices_WithValidGuid_ImplementsAllDevices()
        {
            // Act
            _googleStateService.GetOrCreateDevices(_testGuid);

            // Assert
            var devices = _storageService.GoogleSessions[_testGuid];
            for (int i = 0; i < devices.Count; i++)
            {
                Assert.That(devices, Does.ContainKey(devices.ElementAt(i).Key));
            }
        }
    }
}