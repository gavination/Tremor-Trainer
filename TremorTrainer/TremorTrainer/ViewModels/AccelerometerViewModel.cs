using System;
using System.ComponentModel;
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
        private readonly int _interval = Constants.COUNTDOWN_INTERVAL;
        private bool _isSessionRunning = false;

        public string ReadingText => _readingText;
        public string TimerText => _timerText;

        public AccelerometerViewModel(IMessageService messageService, ISessionService sessionService)
        {
            //ViewModel Page Setup
            Title = "Start Training";
            _sessionLength = (int)App.Current.Properties["SessionLength"];
            _sessiontimer = new Timer(_interval);
            TimeSpan timespan = TimeSpan.FromMilliseconds(_sessionLength);
            _timerText = $"Time Remaining: {(int)timespan.TotalMinutes}:{(int)timespan.TotalSeconds}";
            OnPropertyChanged("TimerText");

            // Register Button Press Commands and subscribe to necessary events
            StartSessionCommand = new Command(async () => await StartAccelerometer());
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;

            // Fulfill external services 
            _messageService = messageService;
            _sessionService = sessionService;
        }

        private async Task SaveSessionAsync()
        {
            //note: test session until new features roll in. 

            Session newSession = new Session
            {
                Id = Guid.NewGuid(),
                Description = "This is a test session",
                Text = "Sample session result text goes here"
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
                    Accelerometer.Start(Constants.SENSOR_SPEED);
                    await StartTimerAsync();
                }
            }
            catch (FeatureNotSupportedException ex)
            {
                // Feature not supported on device
                await _messageService.ShowAsync($"{Constants.DEVICE_NOT_SUPPORTED_MESSAGE} Details: {ex.Message}");

            }
            catch (Exception ex)
            {
                // Other unknown error has occurred.
                await _messageService.ShowAsync(Constants.UNKNOWN_ERROR_MESSAGE);
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
            catch (Exception e)
            {
                // unknown error has occurred.
                await _messageService.ShowAsync(Constants.UNKNOWN_ERROR_MESSAGE);
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

        private void StopTimer()
        {
            _sessiontimer.Stop();
            _sessiontimer.Enabled = false;
            _sessiontimer.Dispose();
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
                StopTimer();
                await StopAccelerometer();

                _isSessionRunning = false;
                SaveSessionAsync().Wait();
            }
        }

        public ICommand StartSessionCommand { get; }
        public ICommand SaveSessionCommand { get; }
    }
}