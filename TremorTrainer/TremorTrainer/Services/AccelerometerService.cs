﻿using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

        private Complex32[] Downsample(Complex32[] samples, int desiredRate, int sampleRate)
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

        private void ButterworthFilter(Complex32[] samples, double sampleRate, int order, double cutoffFrequency, double dcGain )
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
                    
                    var gain = dcGain / (Math.Sqrt((1 +
                                  Math.Pow(cutoffFrequency / binFreq, 2.0 * order)))); //cutoffFrequency / binFreq is highpass and binFreq / cutoffFrequency is lowpass

                    var complexGain = new Complex32((float)gain, 0);
                    samples[i] = Complex32.Multiply(samples[i], complexGain);
                    samples[length - i] = Complex32.Multiply(samples[length - i], complexGain);
                });

                samples[0] = Complex32.Multiply(samples[0], new Complex32((float)dcGain, 0));
            }
        }

        // passing 0 as an argument assumes the down sampling process will not occur and the FFT process will run on all
        // provided values
        public async Task<float> ProcessFftAsync(int milliSecondsElapsed, int desiredSampleRate)
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


                if (desiredSampleRate > 0)
                {
                    
                    // Filter and Down sample the readings for better processing later

                    var currentSampleRate = DetermineSampleRate(milliSecondsElapsed);

                    ButterworthFilter(xSamples, currentSampleRate, 5, 0.3, 1);
                    ButterworthFilter(ySamples, currentSampleRate, 5, 0.3, 1);
                    ButterworthFilter(zSamples, currentSampleRate, 5, 0.3, 1);
                    
                    // down sample the values before finding the dominant frequency
                    var downSampledX = Downsample(xSamples, desiredSampleRate, (int)currentSampleRate);
                    var downSampledY = Downsample(ySamples, desiredSampleRate, (int)currentSampleRate);
                    var downSampledZ = Downsample(zSamples, desiredSampleRate, (int) currentSampleRate);

                    Readings.Clear();

                    var xMagnitude = FindHighestMagnitude(downSampledX).Result;
                    var yMagnitude = FindHighestMagnitude(downSampledY).Result;
                    var zMagnitude = FindHighestMagnitude(downSampledZ).Result;

                    var maxMagnitude = 
                        new[] { xMagnitude.Magnitude, yMagnitude.Magnitude, zMagnitude.Magnitude }.Max();

                    return maxMagnitude;
                }
                else
                {
                    Readings.Clear();

                    var xMagnitude = FindHighestMagnitude(xSamples).Result;
                    var yMagnitude = FindHighestMagnitude(ySamples).Result;
                    var zMagnitude = FindHighestMagnitude(zSamples).Result;

                    var maxMagnitude = 
                        new[] { xMagnitude.Magnitude, yMagnitude.Magnitude, zMagnitude.Magnitude }.Max();

                    return maxMagnitude;
                }
            }
            catch(Exception e)
            {
                // todo: determine proper exception handling protocol here. 
                await _messageService.ShowAsync(Constants.UnknownErrorMessage + e.Message);
                throw;
            }

        }

        private async Task<Complex32> FindHighestMagnitude(Complex32[] values)
        {
            int index = -1;
            var max = new Complex32();

            for (int i = 1; i < values.Length / 2; ++i)
            {
                if (max.Magnitude < values[i].Magnitude)
                {
                    index = i;
                    max = values[i];
                }
            }

            return max.Magnitude;

        }
        private Complex32 GetComplexAverage(Complex32[] samples)
        {
            Complex32 sum = new Complex32();

            for (int i=0; i < samples.Length; i++)
            {
                sum = Complex32.Add(sum, i);
            }

            var average = sum / samples.Length;
            return average;
        }
    }
    public interface IAccelerometerService
    {
        List<Vector3> Readings { get; }
        Task<bool> StartAccelerometer(int sessionLength);
        
        // passing 0 as an argument assumes the down sampling process will not occur and the FFT process will run on all
        // provided values
        Task<float> ProcessFftAsync(int milliSecondsElapsed, int desiredSampleRate = 0);
        double DetermineSampleRate(int secondsElapsed);
        Task StopAccelerometer();
    }
}
