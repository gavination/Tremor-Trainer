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
        private CircularBuffer bufferX { get; set; }
        private CircularBuffer bufferY { get; set; }
        private CircularBuffer bufferZ { get; set; }

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

        private (double, float, int) FindHighestFrequencyAndMagnitude(Complex32[] values, double sampleRate)
        {
            int index = -1;
            var max = new Complex32();

            for (var i = 1; i < values.Length / 2; ++i)
            {
                if (max.Magnitude >= values[i].Magnitude) continue;
                index = i;
                max = values[i];
            }

            //https://stackoverflow.com/questions/4364823/how-do-i-obtain-the-frequencies-of-each-value-in-an-fft
            double freqPerBin = sampleRate / values.Length;
            double frequency = index * freqPerBin;
            return (frequency, max.Magnitude, index);
        }

        private (double, float, int) GetMaxFrequencyAndAmplitude(Complex32 [] xSamples, Complex32 [] ySamples, Complex32 [] zSamples, int desiredSampleRate = 0)
        {
            // represents the sample rate, post downsampling
            double effectiveSampleRate = SampleRate;

            Fourier.Forward(xSamples);
            Fourier.Forward(ySamples);
            Fourier.Forward(zSamples);

            ButterworthFilter(xSamples, effectiveSampleRate, 5, 0.3, 1);
            ButterworthFilter(ySamples, effectiveSampleRate, 5, 0.3, 1);
            ButterworthFilter(zSamples, effectiveSampleRate, 5, 0.3, 1);

            if (desiredSampleRate > 0)
            {
                xSamples = DownSample(xSamples, desiredSampleRate, (int)effectiveSampleRate);
                ySamples = DownSample(ySamples, desiredSampleRate, (int)effectiveSampleRate);
                zSamples = DownSample(zSamples, desiredSampleRate, (int)effectiveSampleRate);

                effectiveSampleRate = desiredSampleRate;
            }

            var xFrequencyAndMagnitude =
                FindHighestFrequencyAndMagnitude(xSamples, effectiveSampleRate);
            var yFrequencyAndMagnitude =
                FindHighestFrequencyAndMagnitude(ySamples, effectiveSampleRate);
            var zFrequencyAndMagnitude =
                FindHighestFrequencyAndMagnitude(zSamples, effectiveSampleRate);

            var results = new List<(double, float, int)>
                    {
                        xFrequencyAndMagnitude,
                        yFrequencyAndMagnitude,
                        zFrequencyAndMagnitude
                    };

            var maxReading = results.Aggregate((i1, i2) => i1.Item2 >= i2.Item2 ? i1 : i2);


            Console.Write($"SR: {effectiveSampleRate} maxFAI{maxReading} : ({ySamples.Length})[");
            for (int i = 1; i < 1 + 8; i++)
            {
                Console.Write($"{ySamples[i].Magnitude}, ");
            }
            Console.Write("]");

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

            //Initialize circular buffers
            const double bufferTime = 2.0;
            bufferX = new CircularBuffer((int)(bufferTime * SampleRate));
            bufferY = new CircularBuffer((int)(bufferTime * SampleRate));
            bufferZ = new CircularBuffer((int)(bufferTime * SampleRate));

            var maxFreqAndAmp = GetMaxFrequencyAndAmplitude(xSamples, ySamples, zSamples, desiredSampleRate);

            // Finish the log that's written inside GetMaxFrequencyAndAmplitude()
            Console.WriteLine("");

            var localVelocityMaxima = FindPeakMovementVelocity(maxFreqAndAmp.Item1, maxFreqAndAmp.Item2);

            BaselineFreq = maxFreqAndAmp.Item1;
            Baseline = localVelocityMaxima;

            return localVelocityMaxima;
        }

        public async Task<(double, double)> ProcessDetectionStage(int millisecondsElapsed)
        {
            // returns the local velocity maxima along with the max frequency of tremors detected in the elapsed timeframe
            var maxFreqAndAmp = GetMaxFrequencyAndAmplitude(bufferX.ToArray(), bufferY.ToArray(), bufferZ.ToArray());

            var localVelocityMaxima = FindPeakMovementVelocity(maxFreqAndAmp.Item1, maxFreqAndAmp.Item2);

            float dt = millisecondsElapsed / 1000.0f;

            //if(maxFreqAndAmp.Item1 > BaselineFreq)
            if (localVelocityMaxima > Baseline)
            {
                TremorCount += dt * maxFreqAndAmp.Item1;
            }

            // Finish the log that's written inside GetMaxFrequencyAndAmplitude()
            Console.WriteLine($" : count {TremorCount}");

            return (localVelocityMaxima, maxFreqAndAmp.Item1);
        }

        public void AddAccelerometerReading(AccelerometerData data)
        {
            if (!IsReading)
            {
                Console.WriteLine("HOW DID WE GET HERE?");
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
