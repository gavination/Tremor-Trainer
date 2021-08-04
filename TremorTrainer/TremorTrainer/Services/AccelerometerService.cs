using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace TremorTrainer.Services
{
    public class AccelerometerService : IAccelerometerService
    {

        private readonly IMessageService _messageService;
        private readonly List<Vector3> _readings;

        public List<Vector3> Readings
        {
            get => _readings;
        }

        public AccelerometerService(IMessageService messageService, ITimerService timerService)
        {
            _messageService = messageService;
            _readings = new List<Vector3>();

        }
        public async Task StartAccelerometer(int sessionLength)
        {
            try
            {
                if (Accelerometer.IsMonitoring && sessionLength > 0)
                {
                    await _messageService.ShowAsync("Session is already active.");
                }
                else
                {
                    // Clearing the list before starting a new Session
                    _readings.Clear();
                    Accelerometer.Start(Constants.SensorSpeed);

                }
            }
            catch (FeatureNotSupportedException ex)
            {
                // Feature not supported on device
                await _messageService.ShowAsync($"{Constants.DeviceNotSupportedMessage} Details: {ex.Message}");

            }
            catch (Exception ex)
            {
                // Other unknown error has occurred.
                await _messageService.ShowAsync(Constants.UnknownErrorMessage);
                Console.WriteLine($"An unknown error occurred: {ex.Message}");
                throw;

            }
        }

        public async Task StopAccelerometer()
        {
            try
            {
                if (Accelerometer.IsMonitoring)
                {
                    Accelerometer.Stop();
                }
                else
                {
                    await _messageService.ShowAsync("Session has already ended");
                }
            }
            catch (Exception ex)
            {
                // unknown error has occurred.
                await _messageService.ShowAsync(Constants.UnknownErrorMessage);
                Console.WriteLine($"An unknown error occurred: {ex.Message}");
                throw;
            }
        }

        public Vector3 GetAverageReading()
        {
            if (_readings.Count > 0)
            {
                // get x, y, and z averages 
                var xAverage = _readings.Select(x => x.X).Average();
                var yAverage = _readings.Select(y => y.Y).Average();
                var zAverage = _readings.Select(z => z.Z).Average();

                return new Vector3(x: xAverage, y: yAverage, z: zAverage);
            }
            else
            {
                Console.WriteLine("No values to compute.");
                throw new ArgumentException();
            }
        }
    }
    public interface IAccelerometerService
    {
        Vector3 GetAverageReading();
        List<Vector3> Readings { get; }
        Task StartAccelerometer(int sessionLength);
        Task StopAccelerometer();
    }
}
