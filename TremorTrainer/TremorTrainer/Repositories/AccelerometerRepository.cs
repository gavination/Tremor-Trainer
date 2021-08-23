using Xamarin.Essentials;

namespace TremorTrainer.Repositories
{
    public class AccelerometerRepository : IAccelerometerRepository
    {
        public bool IsMonitoring
        {
            get => Accelerometer.IsMonitoring;
        }

        public void Start(SensorSpeed speed)
        {
            Accelerometer.Start(speed);
        }

        public void Stop()
        {
            Accelerometer.Stop();
        }
    }

    public interface IAccelerometerRepository
    {
        bool IsMonitoring { get; }
        void Start(SensorSpeed speed);
        void Stop();
    }
}
