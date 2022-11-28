// Copyright (c) Nathan Ford. All rights reserved. Class1.cs

using Leaf2Google.Contexts;
using Leaf2Google.Services;
using Leaf2Google.Services.Google;
using Leaf2Google.Services.Google.Devices;
using Leaf2Google.Json.Google;
using Leaf2Google.Models.Car;
using Leaf2Google.Models.Car.Sessions;
using Leaf2Google.Models.Google.Devices;
using System.Text.Json;
using NUnit.Framework;

namespace Leaf2Google.UnitTests.Dependency
{
    [TestFixture]
    public class Devices_Tests
    {
        private VehicleSessionBase _dummySession;

        private List<IDevice> _devices;

        [SetUp]
        public void SetUp()
        {
            // Arrange
            _dummySession = new VehicleSessionBase("dummy", "dummy", Guid.Empty) {
                AuthenticatedAccessToken = "dummy",
                LastRequestSuccessful = true,
            };

            _dummySession.VINs.Add("dummy");

            _devices = new List<IDevice>() {
                new LockDeviceService(null),
                new ThermostatDeviceService(null)
            };
        }

        // TODO:
        // Add Sync

        [TestCase]
        public void QueryLock_WithDefault_ValidJson()
        {
            // Arrange
            var lockDevice = new LockDeviceService(null);
            // Act
            var queryResult = lockDevice.QueryAsync(_dummySession, new LockModel("1-leaf-lock", "Leaf")
            {
                LastUpdated = DateTime.UtcNow
            }, "").Result;

            var jsonResult = JsonSerializer.Serialize<object>(queryResult);
            // Assert
            Assert.AreEqual(jsonResult, """"{"descriptiveCapacityRemaining":"FULL","capacityRemaining":[{"rawValue":100,"unit":"PERCENTAGE"},{"rawValue":200,"unit":"KILOMETERS"},{"rawValue":124,"unit":"MILES"}],"capacityUntilFull":[{"rawValue":21600,"unit":"SECONDS"}],"isCharging":false,"isPluggedIn":false,"isLocked":true,"isJammed":false,"status":"SUCCESS","online":true}"""");
        }

        [TestCase]
        public void QueryThermostat_WithDefault_ValidJson()
        {
            // Arrange
            var thermostatDevice = new ThermostatDeviceService(null);
            // Act
            var queryResult = thermostatDevice.QueryAsync(_dummySession, new ThermostatModel("1-leaf-ac", "Air Conditioner")
            {
                LastUpdated = DateTime.UtcNow
            }, "").Result;

            var jsonResult = JsonSerializer.Serialize<object>(queryResult);
            // Assert
            Assert.AreEqual(jsonResult, """"{"thermostatMode":"off","thermostatTemperatureSetpoint":21,"thermostatTemperatureAmbient":21,"status":"SUCCESS","online":true}"""");
        }
    }
}