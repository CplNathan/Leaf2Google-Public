// Copyright (c) Nathan Ford. All rights reserved. Class1.cs

using Leaf2Google.Dependency;
using Leaf2Google.Dependency.Google;
using Leaf2Google.Dependency.Google.Devices;
using Leaf2Google.Models.Car;
using NUnit.Framework;

namespace Leaf2Google.UnitTests.Models
{
    [TestFixture]
    public class GoogleStateManager_Tests
    {
        private GoogleStateManager _stateManager;

        private Guid _testGuid;

        private IEnumerable<Type> _testTypes;

        [SetUp]
        public void SetUp()
        {
            // Arrange
            _stateManager = new GoogleStateManager(new Dictionary<Guid, Dictionary<Type, Leaf2Google.Models.Google.Devices.BaseDeviceModel>>(), (ICarSessionManager)null);
            _testGuid = Guid.NewGuid();

            // Get all devices, to later ensure they are assigned.
            var type = typeof(IDevice);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface);

            _testTypes = types;
        }

        [TestCase]
        public void WhenMakeDevices_ThenEnsureDevicesExists()
        {
            // Act
            _stateManager.GetOrCreateDevices(_testGuid);

            // Assert
            Assert.That(_stateManager.Devices, Does.ContainKey(_testGuid));
        }

        [TestCase]
        public void WhenMakeDevices_ThenEnsureAllDevicesImplemented()
        {
            // Act
            _stateManager.GetOrCreateDevices(_testGuid);

            // Assert
            var devices = _stateManager.Devices[_testGuid];
            foreach (var type in _testTypes)
            {
                Assert.That(devices, Does.ContainKey(type));
            }
        }
    }
}