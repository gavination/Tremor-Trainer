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
        private Timer runningTimer;

        // setup private timer and ui vars
        private readonly int _baseSessionTimeLimit;
        private readonly int _samplingTimeLimit;
        private readonly int _detectionTimeLimit;
        private readonly bool _isPrescribedSession;
        private readonly int _downSampleRate;

        private enum SessionState
        {
            Idle,
            Sampling,
            Detecting,
            Running,
        }

        // todo: replace these fields once code solidifies
        private string _readingText = "Accelerometer values will appear here.";
        private string _tremorText = "";

        private string _timerText;
        private string _pointerPosition;
        private string _sessionButtonText;
        private int _currentSessionLength;
        private double _tremorCount;
        private DateTime _sessionStartTime;
        private readonly int _sampleRate;

        //todo: consider renaming to baselineTremorMagnitude
        private SessionState _currentSessionState;

        public ICommand StartSessionCommand { get; }
        public ICommand ViewResultsCommand { get; }
        public ICommand PlaySoundCommand { get; }

        public string TremorCount
        {
            get => _tremorCount.ToString();
            private set
            {
                _tremorText = value;
                OnPropertyChanged("TremorCount");
            }
        }
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

            detectingTimer = new Timer();
            goalTremorTimer = new Timer();
            runningTimer = new Timer();
            _soundService = DependencyService.Get<ISoundService>();

            // ViewModel Page Setup
            // Setup UI elements, initialize vars, and register propertyChanged events
            Title = "Start Training";
            TremorCount = "Placeholder tremor count text.";
            _isPrescribedSession = (bool)App.Current.Properties["IsPrescribedSession"];
            _samplingTimeLimit = Constants.SamplingTimeLimit;
            _detectionTimeLimit = Constants.DetectionTimeLimit;
            _downSampleRate = Constants.DownSampleRate;
            _baseSessionTimeLimit = _sessionService.GetSessionLength(_isPrescribedSession);
            _currentSessionLength = _samplingTimeLimit;
            _sampleRate = 50;
            _currentSessionState = SessionState.Idle;
            _tremorCount = 0;

            TimerText = FormatTimeSpan(TimeSpan.FromMilliseconds(_samplingTimeLimit));


            SessionButtonText = "Start Session";

            // Register Button Press Commands 
            StartSessionCommand = new Command(async () => await ToggleSessionAsync());
            ViewResultsCommand = new Command(async () => await Shell.Current.GoToAsync("/SessionsPage"));
            PlaySoundCommand = new Command(async () => await _soundService.playSound());

            // Subscribe to necessary events
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
            _mainTimerService.Timer.Elapsed += OnSamplingMainTimerEvent;


            detectingTimer.Interval = Constants.CompareInterval;
            detectingTimer.Elapsed += OnDetectingTimedEvent;

            runningTimer.Interval = Constants.CompareInterval;
            runningTimer.Elapsed += OnComparingTimedEvent;
        }

        private async Task ToggleSessionAsync()
        {

            switch (_currentSessionState)
            {
                case SessionState.Idle:

                    // Start the accelerometer and the timer
                    _mainTimerService.SessionRunning = true;
                    _sessionStartTime = DateTime.Now;
                    _currentSessionLength = _samplingTimeLimit;

                    await _accelerometerService.StartAccelerometer(_currentSessionLength);
                    await _mainTimerService.StartTimerAsync(_currentSessionLength);

                    _currentSessionState = SessionState.Sampling;

                    TimeSpan span = TimeSpan.FromMilliseconds(_currentSessionLength);
                    TimerText = FormatTimeSpan(span);

                    SessionButtonText = "Stop Session";
                    TremorText = "Measuring your tremor levels...";


                    Analytics.TrackEvent($"Session Started at {_sessionStartTime} ");
                    break;

                case SessionState.Running:

                    await WrapUpSessionAsync();
                    _mainTimerService.SessionRunning = false;
                    SessionButtonText = "Start Session";
                    _currentSessionLength = _samplingTimeLimit;

                    _currentSessionState = SessionState.Idle;
                    Analytics.TrackEvent($"Stopping a Running session at {DateTime.Now}...");

                    break;

                case SessionState.Sampling:

                    // don't bother saving session results as we never made it to the session
                    await WrapUpSessionAsync(false);
                    _mainTimerService.SessionRunning = false;
                    SessionButtonText = "Start Session";

                    _currentSessionState = SessionState.Idle;
                    Analytics.TrackEvent($"Stopping a session during Sampling stage at {DateTime.Now}...");

                    break;

                case SessionState.Detecting:

                    _mainTimerService.SessionRunning = false;

                    // don't bother saving session results as we never made it to the session
                    await WrapUpSessionAsync(false);
                    SessionButtonText = "Stop Session";
                    _currentSessionState = SessionState.Idle;
                    Analytics.TrackEvent($"Stopping a session during Detecting stage at {DateTime.Now}...");

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

            var result = _accelerometerService.Dump();


            Analytics.TrackEvent("Session has been wrapped up and sensors stopped");



            if (shouldSaveSession)
            {
                var sessionType = _sessionService.GetSessionType(_isPrescribedSession);

                // todo: fulfill this with proper baseline information
                Session newSession = new Session
                {
                    Id = Guid.NewGuid(),
                    Details = $"Session Type: {sessionType}. Saved to the local DB",
                    XAverageVariance = 0,
                    YAverageVariance = 0,
                    ZAverageVariance = 0,
                    XBaseline = 0,
                    YBaseline = 0,
                    ZBaseline = 0,
                    Duration = sessionDuration,
                    Score = 0,
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
                    _mainTimerService.Timer.Elapsed -= OnSamplingMainTimerEvent;

                    Debug.Assert(_currentSessionState == SessionState.Sampling);

                    //The sampling phase is over.

                    Console.WriteLine($"Sample Rate: {_sampleRate} samples per second");

                    Console.WriteLine("Processing values...");
                    await _accelerometerService.ProcessSamplingStage(
                        _detectionTimeLimit,
                        _downSampleRate);

                    //Console.WriteLine($"Baseline Tremor Level: {_baselineTremorLevel}");

                    // stop the timer and unsubscribe from the event here
                    // todo: implement rest flow for acceleromter 
                    await _mainTimerService.StopTimerAsync();
                    await _accelerometerService.StopAccelerometer();
                    _accelerometerService.Reset();


                    //todo: create a toast notification here to inform the user of the timer change


                    // proceed to the detection session state
                    // reassign the current session time limit and restart the timer
                    // start another timer with a different interval for comparing values
                    await _accelerometerService.StartAccelerometer(_baseSessionTimeLimit);
                    _currentSessionLength = _detectionTimeLimit;
                    TimeSpan sessionSpan = TimeSpan.FromMilliseconds(_currentSessionLength);
                    TimerText = FormatTimeSpan(sessionSpan);
                    ReadingText = "Detecting Your Tremor Rate...";

                    await _mainTimerService.StartTimerAsync(_currentSessionLength);
                    _mainTimerService.Timer.Elapsed += OnDetectingMainTimerEvent;
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

                // determine tremor rate and convert ms to s for the time limit
                var t = _detectionTimeLimit / 1000;
                //_tremorRate = _tremorCount / (double)t;

                await WrapUpSessionAsync();
                _mainTimerService.Timer.Elapsed -= OnDetectingMainTimerEvent;

                /*
                // proceed to the session running phase
                _currentSessionState = SessionState.Running;

                // reassign the current session time limit and restart the timer
                // reset the tremor count

                ReadingText = "Running the Session";
                await _accelerometerService.StartAccelerometer(_baseSessionTimeLimit);
                _currentSessionLength = _baseSessionTimeLimit;

                _tremorCount = 0;
                TimeSpan sessionSpan = TimeSpan.FromMilliseconds(_currentSessionLength);
                TimerText = FormatTimeSpan(sessionSpan);

                // Start the running stage's timers
                runningTimer.Start();
                await _mainTimerService.StartTimerAsync(_currentSessionLength);
                _mainTimerService.Timer.Elapsed += OnRunningMainTimerEvent;*/

            }
        }

        // Runs on the _mainTimerService during the SessionState.Running
        private async void OnRunningMainTimerEvent(object sender, ElapsedEventArgs e)
        {
            _currentSessionLength -= (int)_mainTimerService.Timer.Interval;

            // Update the ui
            TimeSpan span = TimeSpan.FromMilliseconds(_currentSessionLength);
            TimerText = FormatTimeSpan(span);

            if (_currentSessionLength <= 0)
            {
                Debug.Assert(_currentSessionState == SessionState.Running);

                // The running phase is over.
                
                // stop the session timer and deregister the event handler
                await WrapUpSessionAsync();
                runningTimer.Stop();
                _mainTimerService.Timer.Elapsed -= OnRunningMainTimerEvent;
            }

            //OnCompareTremorRates();
           
        }
        
        // Runs on the detectingTimer during the Detection Stage
        private async void OnDetectingTimedEvent(object sender, ElapsedEventArgs e)
        {
            // This event only fires during the detection stage.

            // Run an FFT over the newly collected values
            // Ensure there are readings from the accelerometer first

            if (_accelerometerService.IsReadyToDetect)
            {
                await DetectTremor(Constants.CompareInterval);
            }
        }

        // Runs on the runningTimer
        // This event fires during the running stage.
        private async void OnComparingTimedEvent(object sender, ElapsedEventArgs e)
        {
            // Don't look here

            // Run an FFT over the newly collected values
            // Ensure there are readings from the accelerometer first

            if (_accelerometerService.IsReadyToDetect)
            {
                await DetectTremor(Constants.CompareInterval);
            }
        }
        /*
        private async void OnCompareTremorRates()
        {
            // todo: will compare rate of tremors to the global tremor rate
            double t = 1;
            var currentTremorRate = _tremorCount / t;
            var tremorPercentage = (currentTremorRate / _tremorRate) * 100;
            if (tremorPercentage >= 100)
            {
                // update the gauge control to its max value
                PointerPosition = "100";
            }
            // will adjust the position of the dial according to the rate
            PointerPosition = tremorPercentage.ToString(CultureInfo.InvariantCulture);

            // set tremor count to 0 again for the next event invocation
            _tremorCount = 0;
        }
        */
        private async void OnMetronomeInterval(object sender, ElapsedEventArgs e)
        {
            string datetime = DateTime.Now.ToString("hh:mm:ss tt");
            //Console.WriteLine($"we boopin at {datetime}");
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
            TremorCount = tremorMessage;


            double minPointerFrequency = Math.Max(0.5, _accelerometerService.GoalTremorFrequency - 2.0);
            //Create an equidistant max
            double maxPointerFrequency = _accelerometerService.BaselineTremorFrequency + (_accelerometerService.GoalTremorFrequency - minPointerFrequency);

            var pointerPosition = Math.Min(1.0, Math.Max(0.0, (tremorFrequency - minPointerFrequency) / (maxPointerFrequency - minPointerFrequency)));

            PointerPosition = (pointerPosition * 100).ToString(CultureInfo.InvariantCulture);
        }

        private void ConfigureMetronome()
        {
            // determine the goal rate for the metronome
            // goal rate = 2/3rds of the current rate
            //_goalTremorRate = _tremorRate * .66;

            // metronome must be evenly spaced. Divide 1 second by goal tremor rate to determine interval
            //var metronomeInterval = 1 / _goalTremorRate;
            //goalTremorTimer.Interval = metronomeInterval;

            // start the timer
            //goalTremorTimer.Elapsed += OnMetronomeInterval;
            //goalTremorTimer.Start();

            Console.WriteLine($"Starting Metronome at {_accelerometerService.GoalTremorFrequency}");

            // metronome must be evenly spaced. Divide 1 second by goal tremor rate to determine interval
            goalTremorTimer.Interval = 1000 / (_accelerometerService.GoalTremorFrequency);
            goalTremorTimer.Elapsed += OnMetronomeInterval;
            goalTremorTimer.Start();
        }

    }
}