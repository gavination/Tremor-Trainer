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
        private string _readingText = "Sample XYZ values";
        private string _timerText;
        private readonly IMessageService _messageService;
        private readonly ISessionService _sessionService;
        private int _sessionLength;
        private readonly Timer _sessiontimer;
        private bool _isSessionRunning = false;

        public string ReadingText => _readingText;
        public string TimerText => _timerText;

        public AccelerometerViewModel(IMessageService messageService, ISessionService sessionService)
        {
            //ViewModel Page Setup
            Title = "Start Training";
            _sessionLength = (int)App.Current.Properties["SessionLength"];
            _sessiontimer = new Timer(1000);
            TimeSpan timespan = TimeSpan.FromMilliseconds(_sessionLength);
            _timerText = $"Time Remaining: {(int)timespan.TotalMinutes}:{(int)timespan.TotalSeconds}";
            OnPropertyChanged("TimerText");

            // Register Button Press Commands and subscribe to necessary events
            StartSessionCommand = new Command(async () => await ToggleAccelerometer());
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

        private async Task ToggleAccelerometer()
        {
            try
            {
                if (Accelerometer.IsMonitoring)
                {
                    Accelerometer.Stop();
                }
                else
                {
                    Accelerometer.Start(Constants.SENSOR_SPEED);
                    StartTimerAsync();
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

        private async Task StartTimerAsync()
        {
            if (_sessionLength > 0 && !_isSessionRunning)
            {
                //trigger a timer event every for every interval that passes 
                _sessiontimer.Elapsed += OnTimedEvent;
                _sessiontimer.Enabled = true;
                _isSessionRunning = true;
            }
            else
            {
                await _messageService.ShowAsync("Session is already active.");
            }

        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            _sessionLength -= 1000;

            //update the ui with a propertychanged event here
            TimeSpan span = TimeSpan.FromMilliseconds(_sessionLength);

            //TODO: modify this to incorporate leading zeros in the Label.
            _timerText = $"Time Remaining: {(int)span.TotalMinutes}:{(int)span.TotalSeconds}";
            OnPropertyChanged("TimerText");


            if (_sessionLength == 0)
            {
                OnPropertyChanged("TimerText");

                //stop the timer, saves the result. resets the _sessionRunning flag
                _isSessionRunning = false;
                _sessiontimer.Stop();
                ToggleAccelerometer().Wait();
                SaveSessionAsync().Wait();
            }
        }

        public ICommand StartSessionCommand { get; }
        public ICommand SaveSessionCommand { get; }
    }
}