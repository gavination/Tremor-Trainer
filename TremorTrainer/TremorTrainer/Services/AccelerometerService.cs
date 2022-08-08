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
using TremorTrainer.Utilities;
using Xamarin.Essentials;

namespace TremorTrainer.Services
{
    public class AccelerometerService : IAccelerometerService
    {

        private readonly IMessageService _messageService;
        private readonly IAccelerometerRepository _accelerometerRepository;
        private readonly ISessionRepository _sessionRepository;

        private List<Vector3> Readings { get; }
        private CircularBuffer bufferX { get; }
        private CircularBuffer bufferY { get; }
        private CircularBuffer bufferZ { get; }

        public double SampleRate { get; private set; }

        private float TotalSamplingTime = 0.0f;

        public double TremorCount { get; private set; }
        private double Baseline;
        private double BaselineFreq;

        public bool IsReadyToDetect { get => Readings.Count >= bufferX.Capacity; }

        private bool IsReading = false;

        public AccelerometerService(IMessageService messageService, IAccelerometerRepository accelerometerRepository, ISessionRepository sessionRepository)
        {
            _messageService = messageService;
            Readings = new List<Vector3>();
            bufferX = new CircularBuffer(200);
            bufferY = new CircularBuffer(200);
            bufferZ = new CircularBuffer(200);
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
                    bufferX.Clear();
                    bufferY.Clear();
                    bufferZ.Clear();
                    _accelerometerRepository.Start(Constants.SensorSpeed);
                    IsReading = true;
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
                    IsReading = false;
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
        private void CalculateSampleRate()
        {
            var sampleRate = Readings.Count / TotalSamplingTime;

            Console.WriteLine($"Accelerometer Sample Rate: {sampleRate}");
            Console.WriteLine($"Buffer Holds: {((float)bufferX.Capacity) / sampleRate}s");
            SampleRate = sampleRate;
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

        private (double, float) GetMaxFrequencyAndAmplitude(Complex32 [] xSamples, Complex32 [] ySamples, Complex32 [] zSamples, int desiredSampleRate = 0)
        {
            Fourier.Forward(xSamples);
            Fourier.Forward(ySamples);
            Fourier.Forward(zSamples);

            // represents the sample rate, post downsampling
            double effectiveSampleRate = SampleRate;

            if (desiredSampleRate > 0)
            {
                xSamples = DownSample(xSamples, desiredSampleRate, (int)effectiveSampleRate);
                ySamples = DownSample(ySamples, desiredSampleRate, (int)effectiveSampleRate);
                zSamples = DownSample(zSamples, desiredSampleRate, (int)effectiveSampleRate);

                effectiveSampleRate = desiredSampleRate;
            }

            float secondsElapsed = (float)((xSamples.Length) / effectiveSampleRate);

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

            var maxReading = results.Aggregate((i1, i2) => i1.Item2 >= i2.Item2 ? i1 : i2);

            return maxReading;
        }

        public async Task<double> ProcessSamplingStage(int millisecondsElapsed, int desiredSampleRate)
        {
            TotalSamplingTime += millisecondsElapsed / 1000;

            Complex32 [] xSamples = new Complex32[Readings.Count];
            Complex32 [] ySamples = new Complex32[Readings.Count];
            Complex32 [] zSamples = new Complex32[Readings.Count];

            for (int i = 0; i < xSamples.Length; i++)
            {
                xSamples[i] = new Complex32(Readings[i].X, 0);
                ySamples[i] = new Complex32(Readings[i].Y, 0);
                zSamples[i] = new Complex32(Readings[i].Z, 0);
            }

            CalculateSampleRate();

            var maxFreqAndAmp = GetMaxFrequencyAndAmplitude(xSamples, ySamples, zSamples, desiredSampleRate);

            var localVelocityMaxima = FindPeakMovementVelocity(maxFreqAndAmp.Item1, maxFreqAndAmp.Item2);

            BaselineFreq = maxFreqAndAmp.Item1;
            Baseline = localVelocityMaxima;

            return localVelocityMaxima;
        }

        public async Task<(double, double)> ProcessDetectionStage(int millisecondsElapsed)
        {
            // returns the local velocity maxima along with the max frequency of tremors detected in the elapsed timeframe
            Console.WriteLine($"Processing Detection [0]:{bufferX[0]}, {bufferY[0]}, {bufferZ[0]} ");
            var maxFreqAndAmp = GetMaxFrequencyAndAmplitude(bufferX.ToArray(), bufferY.ToArray(), bufferZ.ToArray());

            var localVelocityMaxima = FindPeakMovementVelocity(maxFreqAndAmp.Item1, maxFreqAndAmp.Item2);

            float dt = millisecondsElapsed / 1000.0f;

            //if(maxFreqAndAmp.Item1 > BaselineFreq)
            if (localVelocityMaxima > Baseline)
            {
                TremorCount += dt * maxFreqAndAmp.Item1;
            }

            return (localVelocityMaxima, maxFreqAndAmp.Item1);
        }

        // passing 0 as an argument assumes the down sampling process will not occur and the FFT process will run on all
        // provided values
        /*public async Task<double> ProcessFftAsync(int milliSecondsElapsed, int desiredSampleRate)
        {
            try
            {
                // Get complex values from the Readings List

                // Run the FFT algorithm and create the baseline tremor level
                Complex32 [] xSamples = bufferX.ToArray();
                Complex32 [] ySamples = bufferY.ToArray();
                Complex32 [] zSamples = bufferZ.ToArray();


                Fourier.Forward(xSamples);
                Fourier.Forward(ySamples);
                Fourier.Forward(zSamples);

                float secondsElapsed = milliSecondsElapsed / 1000.0f;

                if (desiredSampleRate > 0)
                {
                    // Filter and Downsample the readings for better processing later
                    var currentSampleRate = CalculateSampleRate(milliSecondsElapsed);

                    ButterworthFilter(xSamples, currentSampleRate, 5, 0.3, 1);
                    ButterworthFilter(ySamples, currentSampleRate, 5, 0.3, 1);
                    ButterworthFilter(zSamples, currentSampleRate, 5, 0.3, 1);

                    // down sample the values before finding the dominant frequency
                    var downSampledX = DownSample(xSamples, desiredSampleRate, (int)currentSampleRate);
                    var downSampledY = DownSample(ySamples, desiredSampleRate, (int)currentSampleRate);
                    var downSampledZ = DownSample(zSamples, desiredSampleRate, (int)currentSampleRate);

                    //todo: remove
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
        }*/

        public void AddAccelerometerReading(AccelerometerData data)
        {
            if (!IsReading)
            {
                Console.WriteLine("HOW THE FUCK DID WE GET HERE?");
                return;
            }

            //todo: remove this when buffer component functional
            Readings.Add(data.Acceleration);

            bufferX.Push(new Complex32(data.Acceleration.X, 0));
            bufferY.Push(new Complex32(data.Acceleration.Y, 0));
            bufferZ.Push(new Complex32(data.Acceleration.Z, 0));
        }

        public void Reset()
        {
            Readings.Clear();
        }

        private (double, float) FindHighestFrequencyAndMagnitude(Complex32[] values, float secondsElapsed)
        {
            int index = -1;
            var max = new Complex32();
            double radianFreq = 0.0;
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
            double frequency = radianFreq / (2 * Math.PI);
            (double, float) freqAndMag = (frequency, max.Magnitude);

            return freqAndMag;

        }

        private double FindPeakMovementVelocity(double frequency, float magnitude)
        {
            // using this formula allows us to calculate a local maxima for movement velocity at t=0
            // m * ω * cos(ω * t) 
            // cos(0) = 1, so formula realistically evaluates to m* w
            // courtesy of LordTocs: https://github.com/LordTocs
            return frequency * magnitude;
        }


        public string Dump()
        {
           return _sessionRepository.ExportReadings(Readings, "allAxes");
        }

    }
    public interface IAccelerometerService
    {
        Task<bool> StartAccelerometer(int sessionLength);
        bool IsReadyToDetect { get; }
        double TremorCount { get; }

        // passing 0 as an argument assumes the down sampling process will not occur and the FFT process will run on all
        // provided values
        //Task<double> ProcessFftAsync(int milliSecondsElapsed, int desiredSampleRate = 0);
        Task<double> ProcessSamplingStage(int millisecondsElapsed, int desiredSampleRate);
        Task<(double, double)> ProcessDetectionStage(int millisecondsElapsed);
        Task StopAccelerometer();
        void AddAccelerometerReading(AccelerometerData data);
        void Reset();
        string Dump();
    }
}
