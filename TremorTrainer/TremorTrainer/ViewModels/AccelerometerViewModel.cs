using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using TremorTrainer.Models;
using TremorTrainer.Services;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace TremorTrainer.ViewModels
{
    public class AccelerometerViewModel : BaseViewModel
    {
        private string _readingText = "Placeholder XYZ values";
        private string _timerText;
        private readonly IMessageService _messageService;
        private readonly ISessionService _sessionService;   
        private int _sessionLength;
        private Timer _sessiontimer;
        private readonly int _interval = Constants.CountdownInterval;
        private bool _isSessionRunning = false;
        private List<Vector3> _accelerometerReadings;

        public string ReadingText => _readingText;
        public string TimerText => _timerText;

        public AccelerometerViewModel(IMessageService messageService, ISessionService sessionService)
        {
            // ViewModel Page Setup
            // Setup UI elements and register propertychanged events
            Title = "Start Training";
            _sessionLength = (int)App.Current.Properties["SessionLength"];
            _sessiontimer = new Timer(_interval);
            TimeSpan timespan = TimeSpan.FromMilliseconds(_sessionLength);
            _timerText = $"Time Remaining: {(int)timespan.TotalMinutes}:{(int)timespan.TotalSeconds}";
            OnPropertyChanged("TimerText");
            _accelerometerReadings = new List<Vector3>();

            // Register Button Press Commands and subscribe to necessary events
            StartSessionCommand = new Command(async () => await StartAccelerometer());
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;

            ViewResultsCommand = new Command(async () => await Shell.Current.GoToAsync("/ItemsPage"));

            // Fulfill external services 
            _messageService = messageService;
            _sessionService = sessionService;
        }

        private async Task SaveSessionAsync(Vector3 reading)
        {
            //note: test session until new features roll in. 

            Session newSession = new Session
            {
                Id = Guid.NewGuid(),
                Description = "This is a test session",
                Text = $"Average Session Value - X: {reading.X}, Y: {reading.Y}, Z: {reading.Z}"
            };

            bool result = await _sessionService.AddItemAsync(newSession);

            if (!result)
            {
                //todo: consider throwing an exception here, perhaps.
                string errorMessage = "Unable to save the results of your session.";
                await _messageService.ShowAsync(errorMessage);
            }
        }

        private void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
            AccelerometerData data = e.Reading;
            _accelerometerReadings.Add(data.Acceleration);
            string readingFormat = $"Reading: X: { data.Acceleration.X}, Y: { data.Acceleration.Y}, Z: { data.Acceleration.Z}";

            Console.WriteLine(readingFormat);

            _readingText = readingFormat;
            OnPropertyChanged("ReadingText");
        }

        private async Task StartAccelerometer()
        {
            try
            {
                if (Accelerometer.IsMonitoring && _sessionLength > 0)
                {
                    await _messageService.ShowAsync("Session is already active.");
                }
                else
                {
                    // Clearing the list before starting a new Session
                    _accelerometerReadings.Clear();
                    Accelerometer.Start(Constants.SensorSpeed);
                    await StartTimerAsync();
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

        private async Task StopAccelerometer()
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

        private async Task StartTimerAsync()
        {
            if (_sessionLength > 0 && !_isSessionRunning)
            {
                //trigger a timer event every for every interval that passes 
                _sessiontimer.Elapsed += OnTimedEvent;
                _sessiontimer.Enabled = true;
                _isSessionRunning = true;
            }
            else if (!_isSessionRunning && _sessionLength <= 0)
            {
                //reset the session length to start a new one
                _sessionLength = (int)App.Current.Properties["SessionLength"];
                _sessiontimer = new Timer(_interval);
                _isSessionRunning = true;

                //trigger a timer event every for every interval that passes
                _sessiontimer.Elapsed += OnTimedEvent;
                _sessiontimer.Enabled = true;
            }
            else
            {
                // session is still active if boolean is true
                await _messageService.ShowAsync("Session is already active.");
            }

        }

        private async Task StopTimer()
        {
            try
            {
                _sessiontimer.Stop();
                _sessiontimer.Enabled = false;
                _sessiontimer.Dispose();
            }
            catch (Exception e)
            {
                var message = $"{Constants.UnknownErrorMessage}. Details: {e.Message}";
                Console.WriteLine(message);
                await _messageService.ShowAsync(Constants.UnknownErrorMessage);
            }

        }

        private async void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            _sessionLength -= _interval;
            Console.WriteLine($"Timed event triggered. Session Length Remaining: {_sessionLength}");

            //update the ui with a propertychanged event here
            TimeSpan span = TimeSpan.FromMilliseconds(_sessionLength);

            //TODO: modify this to incorporate leading zeros in the Label.
            _timerText = $"Time Remaining: {(int)span.TotalMinutes}:{(int)span.TotalSeconds}";
            OnPropertyChanged("TimerText");

            if (_sessionLength == 0)
            {
                OnPropertyChanged("TimerText");

                //stop the timer, saves the result. resets the _sessionRunning flag
                await StopTimer();
                await StopAccelerometer();
                var averageReading = GetAverageReading();

                _isSessionRunning = false;
                SaveSessionAsync(averageReading).Wait();
            }
        }

        private Vector3 GetAverageReading()
        {
            if (_accelerometerReadings.Count > 0)
            {
                // get x, y, and z averages 
                var xAverage = _accelerometerReadings.Select(x => x.X).Average();
                var yAverage = _accelerometerReadings.Select(y => y.Y).Average();
                var zAverage = _accelerometerReadings.Select(z => z.Z).Average();

                return new Vector3(x: xAverage, y: yAverage, z: zAverage);

            }
            else
            {
                Console.WriteLine("No values to compute.");
                throw new ArgumentException();
            }

        }

        public ICommand StartSessionCommand { get; }
        public ICommand SaveSessionCommand { get; }
        public ICommand ViewResultsCommand { get; }
    }
}