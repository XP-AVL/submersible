using System;
using NUnit.Framework;
using Submersible.Runtime.Core;

namespace Submersible.Tests
{
    public class ServiceLocatorTests
    {
        private class TestService : Service
        {
            // Simple test service that uses manual registration
        }

        private class TestAutoService : AutoRegisteredService<TestAutoService>
        {
            // Test service that auto-registers with the service locator
            public bool DisposeWasCalled { get; private set; }

            public override void Dispose()
            {
                DisposeWasCalled = true;
                base.Dispose();
            }
        }

        private class AnotherTestService : Service
        {
            // Another service type for testing multiple service types
        }

        [SetUp]
        public void SetUp()
        {
            // Clear all services before each test
            ServiceLocator.UnregisterAllServices();
        }

        [Test]
        public void RegisterService_WhenCalled_ServiceIsRegistered()
        {
            // Arrange
            var service = new TestService();

            // Act
            ServiceLocator.RegisterService(service);
            bool success = ServiceLocator.TryGetService<TestService>(out var retrievedService);

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(service, retrievedService);
        }

        [Test]
        public void UnregisterService_WhenServiceExists_ServiceIsRemoved()
        {
            // Arrange
            var service = new TestService();
            ServiceLocator.RegisterService(service);

            // Act
            ServiceLocator.UnregisterService(service);
            bool success = ServiceLocator.TryGetService<TestService>(out var retrievedService);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(retrievedService);
        }

        [Test]
        public void UnregisterService_WhenServiceDoesNotMatch_ServiceIsNotRemoved()
        {
            // Arrange
            var service1 = new TestService();
            var service2 = new TestService();
            ServiceLocator.RegisterService(service1);

            // Act
            ServiceLocator.UnregisterService(service2);
            bool success = ServiceLocator.TryGetService<TestService>(out var retrievedService);

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(service1, retrievedService);
        }

        [Test]
        public void TryGetService_WhenServiceExists_ReturnsTrue()
        {
            // Arrange
            var service = new TestService();
            ServiceLocator.RegisterService(service);

            // Act
            bool success = ServiceLocator.TryGetService<TestService>(out var retrievedService);

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(service, retrievedService);
        }

        [Test]
        public void TryGetService_WhenServiceDoesNotExist_ReturnsFalse()
        {
            // Act
            bool success = ServiceLocator.TryGetService<TestService>(out var retrievedService);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(retrievedService);
        }

        [Test]
        public void RegisterService_WhenServiceTypeAlreadyExists_ReplacesOldService()
        {
            // Arrange
            var service1 = new TestService();
            var service2 = new TestService();
            ServiceLocator.RegisterService(service1);

            // Act
            ServiceLocator.RegisterService(service2);
            bool success = ServiceLocator.TryGetService<TestService>(out var retrievedService);

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(service2, retrievedService);
            Assert.AreNotEqual(service1, retrievedService);
        }

        [Test]
        public void RegisterAndUnregisterMultipleServiceTypes()
        {
            // Arrange
            var testService = new TestService();
            var anotherService = new AnotherTestService();

            // Act & Assert - Register both services
            ServiceLocator.RegisterService(testService);
            ServiceLocator.RegisterService(anotherService);

            bool testServiceFound = ServiceLocator.TryGetService<TestService>(out var retrievedTestService);
            bool anotherServiceFound = ServiceLocator.TryGetService<AnotherTestService>(out var retrievedAnotherService);

            Assert.IsTrue(testServiceFound);
            Assert.IsTrue(anotherServiceFound);
            Assert.AreEqual(testService, retrievedTestService);
            Assert.AreEqual(anotherService, retrievedAnotherService);

            // Act & Assert - Unregister one service
            ServiceLocator.UnregisterService(testService);

            testServiceFound = ServiceLocator.TryGetService<TestService>(out retrievedTestService);
            anotherServiceFound = ServiceLocator.TryGetService<AnotherTestService>(out retrievedAnotherService);

            Assert.IsFalse(testServiceFound);
            Assert.IsTrue(anotherServiceFound);
            Assert.IsNull(retrievedTestService);
            Assert.AreEqual(anotherService, retrievedAnotherService);
        }

        [Test]
        public void AutoRegisteredService_WhenCreated_RegistersItselfAutomatically()
        {
            // Act
            var autoService = new TestAutoService();

            // Assert
            bool success = ServiceLocator.TryGetService<TestAutoService>(out var retrievedService);
            Assert.IsTrue(success);
            Assert.AreEqual(autoService, retrievedService);
        }

        [Test]
        public void AutoRegisteredService_WhenDisposed_UnregistersItself()
        {
            // Create
            var autoService = new TestAutoService();

			// Dispose
            autoService.Dispose();
			Assert.IsTrue(autoService.DisposeWasCalled);

            // Ensure service was unregistered
            bool success = ServiceLocator.TryGetService<TestAutoService>(out var retrievedService);
            Assert.IsFalse(success);
            Assert.IsNull(retrievedService);
        }

        [Test]
        public void RegisterBothServiceTypes_ManualAndAutoRegistered_BothAreAccessible()
        {
            // Arrange
            var manualService = new TestService();
            ServiceLocator.RegisterService(manualService);
            var autoService = new TestAutoService();

            // Act
            bool manualFound = ServiceLocator.TryGetService<TestService>(out var retrievedManual);
            bool autoFound = ServiceLocator.TryGetService<TestAutoService>(out var retrievedAuto);

            // Assert
            Assert.IsTrue(manualFound);
            Assert.IsTrue(autoFound);
            Assert.AreEqual(manualService, retrievedManual);
            Assert.AreEqual(autoService, retrievedAuto);
        }
    }
}
