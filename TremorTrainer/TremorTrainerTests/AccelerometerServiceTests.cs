using System;
using Xunit;
using TremorTrainerTests.Mocks;
using TremorTrainer.Services;
using Xamarin.Essentials;

namespace TremorTrainerTests
{
    public class AccelerometerServiceTests
    {

        [Fact]
        public void StartAccelerometerFailsWhenFeatureNotSupported()
        {
            // Arrange 
            MockAccelerometerRepository mockAccelerometerRepository = new MockAccelerometerRepository()
                .StartThrowsFeatureNotSupportedException();
            MockMessageService mockMessageService = new MockMessageService();

            // Act
            AccelerometerService accelerometerService = new AccelerometerService(
                mockMessageService.Object,
                mockAccelerometerRepository.Object
                );

            var result = accelerometerService.StartAccelerometer(123);

            // Assert
            Assert.False(result.Result);
        }
        [Fact]
        public void StartAccelerometerFailsWhenUnknownErrorOccurs()
        {
            // Arrange 
            MockAccelerometerRepository mockAccelerometerRepository = new MockAccelerometerRepository()
                .StartThrowsException();
            MockMessageService mockMessageService = new MockMessageService();

            // Act
            AccelerometerService accelerometerService = new AccelerometerService(
                mockMessageService.Object,
                mockAccelerometerRepository.Object
                );

            var result = accelerometerService.StartAccelerometer(123);

            // Assert
            Assert.False(result.Result);
        }

        [Fact]
        public void StartAccelerometerReturnsFalseWhenAccelerometerRunning()
        {
            // Arrange 
            MockAccelerometerRepository mockAccelerometerRepository = new MockAccelerometerRepository()
                .Start();
            MockMessageService mockMessageService = new MockMessageService();
            mockAccelerometerRepository.SetupGet(x => x.IsMonitoring)
                .Returns(true);

            // Act
            AccelerometerService accelerometerService = new AccelerometerService(
                mockMessageService.Object,
                mockAccelerometerRepository.Object
                );

            var result = accelerometerService.StartAccelerometer(123);
            // Assert
            Assert.False(result.Result);
        }

        [Fact]
        public void StartAccelerometerSucceedsWhenNotAlreadyMonitoring()
        {
            //Arrange 
            MockAccelerometerRepository mockAccelerometerRepository = new MockAccelerometerRepository()
                .Start();
            MockMessageService mockMessageService = new MockMessageService();
            mockAccelerometerRepository.SetupGet(x => x.IsMonitoring)
                .Returns(false);

            // Act
            AccelerometerService accelerometerService = new AccelerometerService(
                mockMessageService.Object,
                mockAccelerometerRepository.Object
                );

            var result = accelerometerService.StartAccelerometer(123);

            // Assert
            Assert.True(result.Result);

        }


    }
}
