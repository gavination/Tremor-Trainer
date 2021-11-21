using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TremorTrainer.Repositories;
using Xamarin.Essentials;

namespace TremorTrainer.Services
{
    public class AccelerometerService : IAccelerometerService
    {

        private readonly IMessageService _messageService;
        private readonly IAccelerometerRepository _accelerometerRepository;

        public List<Vector3> Readings { get; }

        public AccelerometerService(IMessageService messageService, IAccelerometerRepository accelerometerRepository)
        {
            _messageService = messageService;
            Readings = new List<Vector3>();
            _accelerometerRepository = accelerometerRepository;
        }

        public async Task<bool> StartAccelerometer(int sessionLength)
        {
            try
            {
                if (_accelerometerRepository.IsMonitoring && sessionLength > 0)
                {
                    await _messageService.ShowAsync("Session is already active.");
                    return false;
                }
                else
                {
                    // Clearing the list before starting a new Session
                    Readings.Clear();
                    _accelerometerRepository.Start(Constants.SensorSpeed);
                    return true;

                }
            }
            catch (FeatureNotSupportedException ex)
            {
                // Feature not supported on device
                await _messageService.ShowAsync($"{Constants.DeviceNotSupportedMessage} Details: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // Other unknown error has occurred.
                await _messageService.ShowAsync(Constants.UnknownErrorMessage);
                Console.WriteLine($"An unknown error occurred: {ex.Message}");
                return false;

            }
        }

        public async Task StopAccelerometer()
        {
            try
            {
                if (_accelerometerRepository.IsMonitoring)
                {
                    _accelerometerRepository.Stop();
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

        // Test method to prove processing capability of accelerometer vals. 
        // Not to be kept long-term
        public Vector3 GetAverageReading()
        {
            if (Readings.Count > 0)
            {
                // copy a snapshot of the collection to avoid any mutation exceptions
                var readingsToCompute = Readings;

                // get x, y, and z averages 
                float xAverage = readingsToCompute.Select(x => x.X).Average();
                float yAverage = readingsToCompute.Select(y => y.Y).Average();
                float zAverage = readingsToCompute.Select(z => z.Z).Average();

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
        Task<bool> StartAccelerometer(int sessionLength);
        Task StopAccelerometer();
    }
}
