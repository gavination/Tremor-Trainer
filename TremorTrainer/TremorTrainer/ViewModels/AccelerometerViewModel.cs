using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Timers;
using System.Diagnostics;
using System.Windows.Input;
using TremorTrainer.Models;
using TremorTrainer.Services;
using TremorTrainer.Repositories;
using Xamarin.Essentials;
using Xamarin.Forms;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using MathNet.Numerics;

namespace TremorTrainer.ViewModels
{
    
    public class AccelerometerViewModel : BaseViewModel
    {
        private readonly IMessageService _messageService;
        private readonly ISessionService _sessionService;
        private readonly ITimerService _mainTimerService;
        private readonly IAccelerometerService _accelerometerService;
        private readonly ISoundService _soundService;

        private Timer detectingTimer;
        private Timer goalTremorTimer;

        // setup private timer and ui vars
        private readonly int _samplingTimeLimit;
        private readonly int _detectionTimeLimit;
        private readonly bool _isPrescribedSession;
        private readonly int _downSampleRate;

        private enum SessionState
        {
            Idle,
            Sampling,
            Detecting
        }

        // todo: replace these fields once code solidifies
        private string _readingText = "Press Start Session to begin measuring";
        private string _tremorText = "";
        private string _timerText;
        private string _pointerPosition;
        private string _sessionButtonText;
        private int _currentSessionLength;
        private DateTime _sessionStartTime;
        private readonly int _sampleRate;

        //todo: consider renaming to baselineTremorMagnitude
        private SessionState _currentSessionState;

        public ICommand StartSessionCommand { get; }
        public ICommand ViewResultsCommand { get; }
        public ICommand PlaySoundCommand { get; }

        public string TremorText
        {
            get => _tremorText;
            private set
            {
                _tremorText = value;
                OnPropertyChanged("TremorText");
            }
        }
        public string ReadingText
        {
            get => _readingText;
            private set
            {
                _readingText = value;
                OnPropertyChanged("ReadingText");
            }
        }
        public string TimerText
        {
            get => _timerText;
            private set
            {
                _timerText = value;
                OnPropertyChanged("TimerText");
            }
        }
        public string SessionButtonText
        {
            get => _sessionButtonText;
            private set
            {
                _sessionButtonText = value;
                OnPropertyChanged("SessionButtonText");
            }
        }
        public string PointerPosition
        {
            get => _pointerPosition;
            private set
            {
                _pointerPosition = value; 
                OnPropertyChanged("PointerPosition");
            }
        }

        public AccelerometerViewModel(
            IMessageService messageService,
            ISessionService sessionService,
            ITimerService timerService,
            IAccelerometerService accelerometerService,
            ISessionRepository sessionRepository)
        {
            // Fulfill external services 
            _messageService = messageService;
            _sessionService = sessionService;
            _mainTimerService = timerService;
            _accelerometerService = accelerometerService;
            _soundService = DependencyService.Get<ISoundService>();

            // Setup accelerometer and detection based timers
            detectingTimer = new Timer();
            goalTremorTimer = new Timer();

            // ViewModel Page Setup
            // Setup UI elements, initialize vars, and register propertyChanged events
            Title = "Start Training";
            _isPrescribedSession = (bool)App.Current.Properties["IsPrescribedSession"];
            _samplingTimeLimit = Constants.SamplingTimeLimit;
            _detectionTimeLimit = _sessionService.GetSessionLength(_isPrescribedSession);
            _downSampleRate = Constants.DownSampleRate;
            _currentSessionLength = _samplingTimeLimit;
            _sampleRate = 50;
            _currentSessionState = SessionState.Idle;

            TimerText = FormatTimeSpan(TimeSpan.FromMilliseconds(_samplingTimeLimit));

            SessionButtonText = "Start Session";

            // Register Button Press Commands 
            StartSessionCommand = new Command(async () => await ToggleSessionAsync());
            ViewResultsCommand = new Command(async () => await Shell.Current.GoToAsync("/SessionsPage"));
            PlaySoundCommand = new Command(async () => await _soundService.playSound());

            // Subscribe to necessary events
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;

            detectingTimer.Interval = Constants.DetectionInterval;
            detectingTimer.Elapsed += OnDetectingTimedEvent;
        }

        private async Task ToggleSessionAsync()
        {

            switch (_currentSessionState)
            {
                case SessionState.Idle:

                    // Start the accelerometer and the timer
                    _mainTimerService.Timer.Elapsed += OnSamplingMainTimerEvent;
                    _mainTimerService.SessionRunning = true;
                    _sessionStartTime = DateTime.Now;
                    _currentSessionLength = _samplingTimeLimit;
                    ToggleScreenLock();

                    await _accelerometerService.StartAccelerometer(_currentSessionLength);
                    await _mainTimerService.StartTimerAsync(_currentSessionLength);

                    _currentSessionState = SessionState.Sampling;

                    TimeSpan span = TimeSpan.FromMilliseconds(_currentSessionLength);
                    TimerText = FormatTimeSpan(span);

                    SessionButtonText = "Stop Session";
                    TremorText = "Measuring your tremor levels...";


                    Analytics.TrackEvent($"Session Started at {_sessionStartTime} ");
                    break;

                case SessionState.Detecting:

                    await WrapUpSessionAsync();
                    _mainTimerService.SessionRunning = false;
                    SessionButtonText = "Start Session";
                    TremorText = "Session completed. Tap Start Session to begin a new one.";
                    _currentSessionLength = _samplingTimeLimit;

                    _currentSessionState = SessionState.Idle;
                    ToggleScreenLock();
                    Analytics.TrackEvent($"Stopping a Running session at {DateTime.Now}...");

                    break;

                case SessionState.Sampling:

                    // don't bother saving session results as we never made it to the session
                    await WrapUpSessionAsync(false);
                    _mainTimerService.SessionRunning = false;
                    SessionButtonText = "Start Session";

                    _currentSessionState = SessionState.Idle;
                    ToggleScreenLock();
                    Analytics.TrackEvent($"Stopping a session during Sampling stage at {DateTime.Now}...");

                    break;
            }
        }

        private async Task WrapUpSessionAsync(bool shouldSaveSession = true)
        {
            //Accelerometer and timer wrap up
            await _mainTimerService.StopTimerAsync();
            detectingTimer.Stop();
            goalTremorTimer.Stop();
            await _accelerometerService.StopAccelerometer();
            var sessionDuration = DateTime.Now - _sessionStartTime;
            _mainTimerService.SessionRunning = false;

            //var result = _accelerometerService.Dump();
            Analytics.TrackEvent("Session has been wrapped up and sensors stopped");

            if (shouldSaveSession)
            {
                var sessionType = _sessionService.GetSessionType(_isPrescribedSession);
                var sessionDurationText = new DateTime(sessionDuration.Ticks).ToString("HH:mm:ss");
                Session newSession = new Session
                {
                    Id = Guid.NewGuid(),
                    Details = $"Session Type: {sessionType}. Duration: {sessionDurationText}",
                    Duration = sessionDuration,
                    StartTime = DateTime.Now,
                    Type = sessionType
                };

                bool didSucceed = await _sessionService.AddSessionAsync(newSession);
                Analytics.TrackEvent("Saving Session Details...");

                if (!didSucceed)
                {
                    //todo: consider throwing an exception here, perhaps.
                    string errorMessage = "Unable to save the results of your session.";
                    await _messageService.ShowAsync(errorMessage);
                    Analytics.TrackEvent(errorMessage);

                }
            }
        }

        private void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
            _accelerometerService.AddAccelerometerReading(e.Reading);

            //debug code for showing accelerometer readings
            //string readingFormat =
            //    $"Reading X:{data.Acceleration.X}, Y:{data.Acceleration.Y}, Z:{data.Acceleration.Z}";
            //ReadingText = readingFormat;

        }

        private async void OnSamplingMainTimerEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                _currentSessionLength -= (int)_mainTimerService.Timer.Interval;

                //update the ui
                TimeSpan span = TimeSpan.FromMilliseconds(_currentSessionLength);
                TimerText = FormatTimeSpan(span);
                ReadingText = "Currently Sampling Your Tremor Activity...";

                // run the FFT logic when timer hits 0
                if (_currentSessionLength <= 0)
                {

                    Debug.Assert(_currentSessionState == SessionState.Sampling);

                    //The sampling phase is over.

                    Console.WriteLine($"Sample Rate: {_sampleRate} samples per second");

                    Console.WriteLine("Processing values...");
                    await _accelerometerService.ProcessSamplingStage(
                        _samplingTimeLimit,
                        _downSampleRate);

                    // stop the timer and unsubscribe from the event here
                    _mainTimerService.Timer.Elapsed -= OnSamplingMainTimerEvent;
                    await _mainTimerService.StopTimerAsync();
                    await _accelerometerService.StopAccelerometer();
                    _accelerometerService.Reset();


                    //todo: create a toast notification here to inform the user of the timer change


                    // proceed to the detection session state
                    // reassign the current session time limit and restart the timer
                    // start another timer with a different interval for comparing values
                    await _accelerometerService.StartAccelerometer(_detectionTimeLimit);
                    _currentSessionLength = _detectionTimeLimit;
                    TimeSpan sessionSpan = TimeSpan.FromMilliseconds(_currentSessionLength);
                    TimerText = FormatTimeSpan(sessionSpan);
                    ReadingText = "Try to keep pace with the sound";

                    _mainTimerService.Timer.Elapsed += OnDetectingMainTimerEvent;
                    await _mainTimerService.StartTimerAsync(_currentSessionLength);

                    detectingTimer.Start();
                    _currentSessionState = SessionState.Detecting;

                    // setup the metronome to play audio at proper intervals
                    ConfigureMetronome();
                }
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync(
                    $"An unknown error has occurred. Contact the team with the error details:" +
                    $" {ex.Message}");
                throw;
            }
        }

        private async void OnDetectingMainTimerEvent(object sender, ElapsedEventArgs e)
        {

            _currentSessionLength -= (int)_mainTimerService.Timer.Interval;

            TimeSpan span = TimeSpan.FromMilliseconds(_currentSessionLength);
            TimerText = FormatTimeSpan(span);

            if (_currentSessionLength <= 0)
            {
                Debug.Assert(_currentSessionState == SessionState.Detecting);

                // this marks the end of the detection phase.
                // stop the detectingTimer
                // stop the accelerometer
                detectingTimer.Stop();
                await _accelerometerService.StopAccelerometer();

                await WrapUpSessionAsync();
                _mainTimerService.Timer.Elapsed -= OnDetectingMainTimerEvent;
                _mainTimerService.SessionRunning = false;
                SessionButtonText = "Start New Session";
                ReadingText = "Treatment session complete. Good job!";
                _currentSessionLength = _samplingTimeLimit;

                _currentSessionState = SessionState.Idle;
                ToggleScreenLock();
                Analytics.TrackEvent($"Ended a Running session at {DateTime.Now}...");

            }
        }
        
        // Runs on the detectingTimer during the Detection Stage
        private async void OnDetectingTimedEvent(object sender, ElapsedEventArgs e)
        {
            // This event only fires during the detection stage.

            // Run an FFT over the newly collected values
            // Ensure there are readings from the accelerometer first

            if (_accelerometerService.IsReadyToDetect)
            {
                await DetectTremor(Constants.DetectionInterval);
            }
        }

        private async void OnMetronomeInterval(object sender, ElapsedEventArgs e)
        {            
            await _soundService.playSound();
        }

        private string FormatTimeSpan(TimeSpan span)
        {
            return $"Time Remaining: {span}";
        }

        private async Task DetectTremor(int millisecondsElapsed)
        {
            var tremorFrequency = await _accelerometerService.ProcessDetectionStage(millisecondsElapsed);
            
            //var message = $"Current Tremor Velocity: {_currentTremorLevel}";
            // Compare the magnitude to the baseline tremor level

            var tremorMessage = $"Tremors Detected: {_accelerometerService.TremorCount}";


            //Try to create +/- 2 HZ range to display on the meter. We take the Max with 0.5 to avoid anything slower than once every 2 seconds.
            double minPointerFrequency = Math.Max(0.25, _accelerometerService.GoalTremorFrequency - 2.0);
            //To calculate the max side of the +/- 2 HZ range we make sure to use the size min side of the range to avoid an unbalanced range size.
            double maxPointerFrequency = _accelerometerService.BaselineTremorFrequency + (_accelerometerService.GoalTremorFrequency - minPointerFrequency);
            // Map the tremor frequency in the range we picked to a value between 0 and 1
            var pointerPosition = Math.Min(1.0, Math.Max(0.0, (tremorFrequency - minPointerFrequency) / (maxPointerFrequency - minPointerFrequency)));

            PointerPosition = (pointerPosition * 100).ToString(CultureInfo.InvariantCulture);
        }

        private void ConfigureMetronome()
        {

            Console.WriteLine($"Starting Metronome at {_accelerometerService.GoalTremorFrequency}");

            // metronome must be evenly spaced. Divide 1 second by goal tremor rate to determine interval
            goalTremorTimer.Interval = 1000 / (_accelerometerService.GoalTremorFrequency);
            goalTremorTimer.Elapsed += OnMetronomeInterval;
            goalTremorTimer.Start();
        }

        private void ToggleScreenLock()
        {
            DeviceDisplay.KeepScreenOn = !DeviceDisplay.KeepScreenOn;
            Console.WriteLine($"Screen on configuration: {DeviceDisplay.KeepScreenOn.ToString()}");
        }

    }
}