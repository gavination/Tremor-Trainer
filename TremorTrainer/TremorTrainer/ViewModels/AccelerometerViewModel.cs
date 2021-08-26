using System;
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
        private readonly IMessageService _messageService;
        private readonly ISessionService _sessionService;
        private readonly ITimerService _timerService;
        private readonly IAccelerometerService _accelerometerService;

        // setup private timer and ui vars
        private readonly int _sessionTimeLimit;
        //todo: replace this once code solidifies
        private string _readingText = "Placeholder XYZ values";
        private string _timerText;
        private string _sessionButtonText;
        private int _currentSessionLength;
        private DateTime _sessionStartTime;

        public string ReadingText
        {
            get { return _readingText; }
            private set
            {
                _readingText = value;
                OnPropertyChanged("ReadingText");
            }
        }
        public string TimerText
        {
            get { return _timerText; }
            private set
            {
                _timerText = value;
                OnPropertyChanged("TimerText");
            }
        }
        public string SessionButtonText 
        {
            get { return _sessionButtonText; }
            private set
            {
                _sessionButtonText = value;
                OnPropertyChanged("SessionButtonText");
            }
        }

        public AccelerometerViewModel(
            IMessageService messageService,
            ISessionService sessionService,
            ITimerService timerService,
            IAccelerometerService accelerometerService)
        {
            // Fulfill external services 
            _messageService = messageService;
            _sessionService = sessionService;
            _timerService = timerService;
            _accelerometerService = accelerometerService;

            // ViewModel Page Setup
            // Setup UI elements and register propertychanged events
            Title = "Start Training";
            _currentSessionLength = (int)App.Current.Properties["SessionLength"];
            _sessionTimeLimit = _currentSessionLength;

            TimerText = FormatTimeSpan(TimeSpan.FromMilliseconds(_currentSessionLength));


            SessionButtonText = "Start Session";

            // Register Button Press Commands 
            StartSessionCommand = new Command(async () => await ToggleSessionAsync());
            ViewResultsCommand = new Command(async () => await Shell.Current.GoToAsync("/SessionsPage"));

            // Subscribe to necessary events
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
            _timerService.Timer.Elapsed += OnTimedEvent;

        }

        private async Task ToggleSessionAsync()
        {
            // todo: have the service methods return booleans to determine
            // whether to record start time and flipping the sessionRunning var

            // Determine if this is the start of a new Session
            if (_sessionTimeLimit == _currentSessionLength && !_timerService.SessionRunning)
            {
                // Start the accelerometer and the timer
                _timerService.SessionRunning = true;
                _sessionStartTime = DateTime.Now;

                await _accelerometerService.StartAccelerometer(_currentSessionLength);
                await _timerService.StartTimerAsync(_currentSessionLength);

                SessionButtonText = "Stop Session";

            }
            // Determine if restarting a session after a previous run
            else if (_currentSessionLength <= 0 && !_timerService.SessionRunning)
            {
                // reset the session length and start a new Session
                _timerService.SessionRunning = true;
                _sessionStartTime = DateTime.Now;

                _currentSessionLength = _sessionTimeLimit;

                TimeSpan span = TimeSpan.FromMilliseconds(_currentSessionLength);
                TimerText = FormatTimeSpan(span);

                await _accelerometerService.StartAccelerometer(_currentSessionLength);
                await _timerService.StartTimerAsync(_currentSessionLength);

                SessionButtonText = "Stop Session";


            }
            // Determine if the session is still running
            // Cancel the run and save the current status as a result
            else if (_currentSessionLength >= 0 && _timerService.SessionRunning)
            {
                // Creates a timestamp if none exists yet
                if (_sessionStartTime == null)
                {
                    _sessionStartTime = DateTime.Now;
                }
                // Stop the Running Session, save the results, and dispose of the timer
                await WrapUpSessionAsync();

                _timerService.SessionRunning = false;

                SessionButtonText = "Start Session";
            }
            // Determine if the session was stopped and a new one needs to be created
            // Reset the current session length, flip the SessionRunning bool, and restart the timer
            else if(_currentSessionLength > 0 && !_timerService.SessionRunning)
            {
                _sessionStartTime = DateTime.Now;
                _currentSessionLength = _sessionTimeLimit;
                _timerService.SessionRunning = true;

                await _accelerometerService.StartAccelerometer(_currentSessionLength);
                await _timerService.StartTimerAsync(_currentSessionLength);

                TimeSpan span = TimeSpan.FromMilliseconds(_currentSessionLength);
                TimerText = FormatTimeSpan(span);

                SessionButtonText = "Stop Session";
            }
        }

        private async Task WrapUpSessionAsync()
        {
            //Accelerometer and timer wrap up
            await _timerService.StopTimerAsync();
            await _accelerometerService.StopAccelerometer();
            var averageReading = _accelerometerService.GetAverageReading();
            var sessionDuration = DateTime.Now - _sessionStartTime;
            _timerService.SessionRunning = false;

            //todo: test session until new features roll in. create a session with values to determine baselines and sessiontype

            Session newSession = new Session
            {
                Id = Guid.NewGuid(),
                Details = $"Session Type: {SessionType.Induction}. Average Session Values - X: {averageReading.X}, Y: {averageReading.Y}, Z: {averageReading.Z}",
                XAverageVariance = averageReading.X,
                YAverageVariance = averageReading.Y,
                ZAverageVariance = averageReading.Z,
                XBaseline = 0,
                YBaseline = 0,
                ZBaseline = 0,
                Duration = sessionDuration,
                Score = 0,
                StartTime = DateTime.Now,
                Type = SessionType.Induction
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
            _accelerometerService.Readings.Add(data.Acceleration);
            string readingFormat = $"Reading: X: { data.Acceleration.X}, Y: { data.Acceleration.Y}, Z: { data.Acceleration.Z}";

            Console.WriteLine(readingFormat);

            ReadingText = readingFormat;
        }

        private async void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            _currentSessionLength -= _timerService.Interval;
            Console.WriteLine($"Timed event triggered. Session Length Remaining: {_currentSessionLength}");

            //update the ui with a propertychanged event here
            TimeSpan span = TimeSpan.FromMilliseconds(_currentSessionLength);

            TimerText = FormatTimeSpan(span);

            if (_currentSessionLength == 0)
            {
                //stop the timer, saves the result. resets the _sessionRunning flag
                _timerService.SessionRunning = false;
                SessionButtonText = "Start Session";
                await WrapUpSessionAsync();
            }
        }

        private string FormatTimeSpan(TimeSpan span)
        {
            //TODO: modify this to incorporate leading zeros in the Label.
            return $"Time Remaining: {(int)span.TotalMinutes}:{(int)span.TotalSeconds}";
        }


        public ICommand StartSessionCommand { get; }
        public ICommand SaveSessionCommand { get; }
        public ICommand ViewResultsCommand { get; }
    }
}