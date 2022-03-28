using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security;
using System.Threading.Tasks;
using TremorTrainer.Models;
using TremorTrainer.Repositories;
using Xamarin.Essentials;

namespace TremorTrainer.Services
{
    public class AccelerometerService : IAccelerometerService
    {

        private readonly IMessageService _messageService;
        private readonly IAccelerometerRepository _accelerometerRepository;
        private readonly ISessionRepository _sessionRepository;

        public List<Vector3> Readings { get; }
        public int SampleRate { get; set; }

        public AccelerometerService(IMessageService messageService, IAccelerometerRepository accelerometerRepository, ISessionRepository sessionRepository)
        {
            _messageService = messageService;
            Readings = new List<Vector3>();
            _accelerometerRepository = accelerometerRepository;
            _sessionRepository = sessionRepository;
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
            }
            catch (Exception ex)
            {
                // unknown error has occurred.
                await _messageService.ShowAsync(Constants.UnknownErrorMessage);
                Console.WriteLine($"An unknown error occurred: {ex.Message}");
                throw;
            }
        }

        // Determines sample rate based on how many samples collected over time elapsed
        // Should be measured in Hz (samples/second)
        public double DetermineSampleRate(int millisecondsElapsed)
        {
            var secondsElapsed = millisecondsElapsed / 1000;
            var sampleRate = Readings.Count / secondsElapsed;
            SampleRate = sampleRate;
            return sampleRate;

        }

        private Complex32[] DownSample(Complex32[] samples, int desiredRate, int sampleRate)
        {
            // takes every nth element in originally collected samples to downsample 
            // this may be a naive implementation of sampling. May need to be updated in the future

            var samplingFactor = sampleRate / desiredRate;
            var downSampledArray = new List<Complex32>();
            if (samples.Length > 0)
            {
                for (int i = 0; i < samples.Length; i++)
                {
                    if (i % samplingFactor == 0)
                    {
                        downSampledArray.Add(samples[i]);
                    }
                }
                return downSampledArray.ToArray();
            }
            else
            {
                var errorMessage = "Sample list is empty";
                throw new ArgumentException(errorMessage);
            }

        }

        private void ButterworthFilter(Complex32[] samples, double sampleRate, int order, double cutoffFrequency, double dcGain)
        {
            // Concept borrowed heavily from the Centerspace blog: https://www.centerspace.net/butterworth-filter-csharp
            // High pass butterworth filter to whittle out effects caused by gravity

            if (cutoffFrequency > 0)
            {
                var length = samples.Length;
                var numBins = length / 2;  // Half the length of the FFT by symmetry
                double binWidth = sampleRate / length; // Hz

                // Filter
                Parallel.For(1, length / 2, i =>
                {
                    var binFreq = binWidth * i;
                    //cutoffFrequency / binFreq is highpass and binFreq / cutoffFrequency is lowpass
                    var gain = dcGain / (Math.Sqrt((1 +
                                  Math.Pow(cutoffFrequency / binFreq, 2.0 * order)))); 

                    var complexGain = new Complex32((float)gain, 0);
                    samples[i] = Complex32.Multiply(samples[i], complexGain);
                    samples[length - i] = Complex32.Multiply(samples[length - i], complexGain);
                });

                samples[0] = Complex32.Multiply(samples[0], new Complex32((float)dcGain, 0));
            }
        }

        // passing 0 as an argument assumes the down sampling process will not occur and the FFT process will run on all
        // provided values
        public async Task<double> ProcessFftAsync(int milliSecondsElapsed, int desiredSampleRate)
        {
            try
            {
                // Get complex values from the Readings List

                var xSamples = Readings
                    .Select(x => x.X)
                    .Select(c => new Complex32(c, 0))
                    .ToArray();

                var ySamples = Readings
                    .Select(y => y.Y)
                    .Select(c => new Complex32(c, 0))
                    .ToArray();

                var zSamples = Readings
                    .Select(z => z.Z)
                    .Select(c => new Complex32(c, 0))
                    .ToArray();

                // Run the FFT algorithm and create the baseline tremor level

                Fourier.Forward(xSamples);
                Fourier.Forward(ySamples);
                Fourier.Forward(zSamples);

                var secondsElapsed = milliSecondsElapsed / 1000;

                if (desiredSampleRate > 0)
                {
                    // Filter and Down sample the readings for better processing later
                    var currentSampleRate = DetermineSampleRate(milliSecondsElapsed);

                    ButterworthFilter(xSamples, currentSampleRate, 5, 0.3, 1);
                    ButterworthFilter(ySamples, currentSampleRate, 5, 0.3, 1);
                    ButterworthFilter(zSamples, currentSampleRate, 5, 0.3, 1);

                    // down sample the values before finding the dominant frequency
                    var downSampledX = DownSample(xSamples, desiredSampleRate, (int)currentSampleRate);
                    var downSampledY = DownSample(ySamples, desiredSampleRate, (int)currentSampleRate);
                    var downSampledZ = DownSample(zSamples, desiredSampleRate, (int)currentSampleRate);

                    Readings.Clear();

                    var xFrequencyAndMagnitude =
                        FindHighestFrequencyAndMagnitude(downSampledX, secondsElapsed);
                    var yFrequencyAndMagnitude =
                        FindHighestFrequencyAndMagnitude(downSampledY, secondsElapsed);
                    var zFrequencyAndMagnitude =
                        FindHighestFrequencyAndMagnitude(downSampledZ, secondsElapsed);


                    // put these lovely tuples in a list and compare them to find which axis has the highest magnitude
                    var results = new List<(double, float)>
                    {
                        xFrequencyAndMagnitude,
                        yFrequencyAndMagnitude,
                        zFrequencyAndMagnitude
                    };

                    var maxReading =
                        results.Aggregate((i1, i2) => i1.Item2 >= i2.Item2 ? i1 : i2);
                    var localVelocityMaxima = FindPeakMovementVelocity(maxReading.Item1, maxReading.Item2);

                    return localVelocityMaxima;
                }
                else
                {
                    Readings.Clear();

                    var xFrequencyAndMagnitude = 
                        FindHighestFrequencyAndMagnitude(xSamples, secondsElapsed);
                    var yFrequencyAndMagnitude = 
                        FindHighestFrequencyAndMagnitude(ySamples, secondsElapsed);
                    var zFrequencyAndMagnitude = 
                        FindHighestFrequencyAndMagnitude(zSamples, secondsElapsed);

                    var results = new List<(double, float)>
                    {
                        xFrequencyAndMagnitude,
                        yFrequencyAndMagnitude,
                        zFrequencyAndMagnitude
                    };

                    var maxReading =
                        results.Aggregate((i1, i2) => i1.Item2 >= i2.Item2 ? i1 : i2);

                    var localVelocityMaxima = FindPeakMovementVelocity(maxReading.Item1, maxReading.Item2);
                    return localVelocityMaxima;
                }
            }
            catch (Exception e)
            {
                // todo: determine proper exception handling protocol here. 
                await _messageService.ShowAsync(Constants.UnknownErrorMessage + e.Message);
                throw;
            }
        }

        private (double, float) FindHighestFrequencyAndMagnitude(Complex32[] values, int secondsElapsed)
        {
            int index = -1;
            var max = new Complex32();
            double radianFreq = 0.0;
            double frequency = 0.0;

            // assume time limit is 10s
            var samplesPerSecond = values.Length / secondsElapsed;
            var T = values.Length / samplesPerSecond;
            var dw = 2 * Math.PI / T;

            for (var i = 1; i < values.Length / 2; ++i)
            {
                if (!(max.Magnitude < values[i].Magnitude)) continue;
                index = i;
                max = values[i];
                //frequency measured in rads/s
                radianFreq = dw * index;
                

            }
            // convert freq to hz...
            frequency = radianFreq / (2 * Math.PI);
            
            (double, float) freqAndMag = (frequency, max.Magnitude);

            return freqAndMag;

        }

        private double FindPeakMovementVelocity(double frequency, float magnitude)
        {
            // using this formula allows us to calculate a local maxima for movement velocity at t=0
            // m * ω * cos(ω * t) 
            // cos(0) = 1, so formula realistically evaluates to m* w
            return frequency * magnitude;
        }
    }
    public interface IAccelerometerService
    {
        List<Vector3> Readings { get; }
        Task<bool> StartAccelerometer(int sessionLength);

        // passing 0 as an argument assumes the down sampling process will not occur and the FFT process will run on all
        // provided values
        Task<double> ProcessFftAsync(int milliSecondsElapsed, int desiredSampleRate = 0);
        double DetermineSampleRate(int secondsElapsed);
        Task StopAccelerometer();
    }
}
