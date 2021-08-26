using Moq;
using System;
using TremorTrainer.Repositories;
using Xamarin.Essentials;

namespace TremorTrainerTests.Mocks
{
    public class MockAccelerometerRepository : Mock<IAccelerometerRepository>
    {
        public MockAccelerometerRepository Start()
        {
            Setup(x => x.Start(It.IsAny<SensorSpeed>()))
                .Verifiable();

            return this;
        }

        public MockAccelerometerRepository Stop()
        {
            Setup(x => x.Stop())
                .Verifiable();

            return this;
        }

        public MockAccelerometerRepository StartThrowsFeatureNotSupportedException()
        {
            Setup(x => x.Start(It.IsAny<SensorSpeed>()))
                .Throws( new FeatureNotSupportedException());

            return this;
        }

        public MockAccelerometerRepository StartThrowsException()
        {
            Setup(x => x.Start(It.IsAny<SensorSpeed>()))
                .Throws(new Exception());

            return this;
        }

    }
}
