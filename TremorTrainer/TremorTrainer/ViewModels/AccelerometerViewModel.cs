using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using TremorTrainer.Models;
using TremorTrainer.Services;
using Xamarin.Essentials;
using Xamarin.Forms;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace TremorTrainer.ViewModels
{
    
    public class AccelerometerViewModel : BaseViewModel
    {
        private readonly IMessageService _messageService;
        private readonly ISessionService _sessionService;
        private readonly ITimerService _mainTimerService;
        private readonly IAccelerometerService _accelerometerService;

        private Timer sessionTimer;

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
        private int _tremorCount;
        private DateTime _sessionStartTime;
        private readonly int _sampleRate;
        private double _baselineTremorLevel;
        private double _currentTremorLevel;
        private double _tremorRate;
        private double _goalTremorFrequency;
        private SessionState _currentSessionState;

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
            IAccelerometerService accelerometerService)
        {
            // Fulfill external services 
            _messageService = messageService;
            _sessionService = sessionService;
            _mainTimerService = timerService;
            _accelerometerService = accelerometerService;

            sessionTimer = new Timer();

            // ViewModel Page Setup
            // Setup UI elements, initialize vars, and register propertyChanged events
            Title = "Start Training";
            _isPrescribedSession = (bool)App.Current.Properties["IsPrescribedSession"];
            _samplingTimeLimit = Constants.SamplingTimeLimit;
            _detectionTimeLimit = Constants.DetectionTimeLimit;
            _downSampleRate = Constants.DownSampleRate;
            _baseSessionTimeLimit = _sessionService.GetSessionLength(_isPrescribedSession);
            _detectionTimeLimit =
            _currentSessionLength = _samplingTimeLimit;
            _sampleRate = 50;
            _currentSessionState = SessionState.Idle;
            _tremorCount = 0;


            TimerText = FormatTimeSpan(TimeSpan.FromMilliseconds(_samplingTimeLimit));


            SessionButtonText = "Start Session";
            
            Console.WriteLine("Constructor invoked properly");

            // Register Button Press Commands 
            StartSessionCommand = new Command(async () => await ToggleSessionAsync());
            ViewResultsCommand = new Command(async () => await Shell.Current.GoToAsync("/SessionsPage"));

            // Subscribe to necessary events
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
            _mainTimerService.Timer.Elapsed += OnSamplingTimedEvent;

            sessionTimer.Interval = Constants.CompareInterval;
            sessionTimer.Elapsed += OnDetectingTimedEvent;
        }

        private async Task ToggleSessionAsync()
        {
            Console.WriteLine("Button pressed");

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
            sessionTimer.Stop();
            await _accelerometerService.StopAccelerometer();
            var sessionDuration = DateTime.Now - _sessionStartTime;
            _mainTimerService.SessionRunning = false;

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

                bool result = await _sessionService.AddSessionAsync(newSession);
                Analytics.TrackEvent("Saving Session Details...");

                if (!result)
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
            AccelerometerData data = e.Reading;
            _accelerometerService.Readings.Add(data.Acceleration);

        }
        private async void OnSamplingTimedEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                _currentSessionLength -= (int)_mainTimerService.Timer.Interval;

                //update the ui
                TimeSpan span = TimeSpan.FromMilliseconds(_currentSessionLength);
                TimerText = FormatTimeSpan(span);
                ReadingText = "Currently Sampling Your Tremor Activity...";

                // run the FFT logic when timer hits 0
                if (_currentSessionLength == 0)
                {
                    switch (_currentSessionState)
                    {
                        case SessionState.Sampling:
                            Console.WriteLine($"Sample Rate: {_sampleRate} samples per second");

                            Console.WriteLine("Processing values...");
                            _baselineTremorLevel = await _accelerometerService.ProcessFftAsync(
                                _detectionTimeLimit,
                                _downSampleRate);

                            Console.WriteLine($"Baseline Tremor Level: {_baselineTremorLevel}");

                            // stop the timer and unsubscribe from the event here
                            await _mainTimerService.StopTimerAsync();
                            await _accelerometerService.StopAccelerometer();
                            _accelerometerService.Readings.Clear();
                            _mainTimerService.Timer.Elapsed -= OnSamplingTimedEvent;


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
                            _mainTimerService.Timer.Elapsed += OnSessionTimedEvent;

                            sessionTimer.Start();
                            _currentSessionState = SessionState.Detecting;
                            break;

                        case SessionState.Running:

                            // todo: evaluate if this case is necessary
                            // assume this marks the end of the session

                            _mainTimerService.SessionRunning = false;
                            SessionButtonText = "Start Session";
                            await _mainTimerService.StopTimerAsync();

                            sessionTimer.Stop();
                            await _accelerometerService.StopAccelerometer();
                            await WrapUpSessionAsync();
                            _currentSessionState = SessionState.Idle;
                            break;
                    }
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
        private async void OnSessionTimedEvent(object sender, ElapsedEventArgs e)
        {
            _currentSessionLength -= (int)_mainTimerService.Timer.Interval;

            // Update the ui
            TimeSpan span = TimeSpan.FromMilliseconds(_currentSessionLength);
            TimerText = FormatTimeSpan(span);

            if (_currentSessionLength == 0)
            {
                switch (_currentSessionState)
                {
                    case SessionState.Running:
                        
                        // stop the session timer and deregister the event handler
                        await WrapUpSessionAsync();
                        sessionTimer.Stop();
                        _mainTimerService.Timer.Elapsed -= OnSessionTimedEvent;
                        break;
                }
            }

            if (_currentSessionState == SessionState.Running)
            {
                ReadingText = "Running the Session";

                OnCompareTremorRates();
            }
        }
        private async void OnDetectingTimedEvent(object sender, ElapsedEventArgs e)
        {
            // Run an FFT over the newly collected values

            if (_accelerometerService.Readings.Count > 0)
            {
                await DetectTremor();
            }

            switch (_currentSessionState)
            {
                case SessionState.Detecting:
                    if (_currentSessionLength == 0)
                    {
                        // this marks the end of the detection phase.
                        // stop the main timer
                        // stop the accelerometer
                        sessionTimer.Stop();
                        await _accelerometerService.StopAccelerometer();

                        // determine tremor rate and convert ms to s for the time limit
                        var t = _detectionTimeLimit/ 1000;
                        _tremorRate = _tremorCount / (double)t;

                        Console.WriteLine($"Current tremor rate: {_tremorRate}");
                        // proceed to the session running phase
                        _currentSessionState = SessionState.Running;


                        // proceed to the running session state
                        // reassign the current session time limit and restart the timer
                        // reset the tremor count
                        await _accelerometerService.StartAccelerometer(_baseSessionTimeLimit);
                        _currentSessionLength = _baseSessionTimeLimit;
                        
                        // goal tremor frequency is 2/3 of the currently prescribed rate
                        _goalTremorFrequency = _tremorRate * 0.66;
                        Console.WriteLine($"Goal tremor frequency: {_goalTremorFrequency}");

                        _tremorCount = 0;

                        TimeSpan sessionSpan = TimeSpan.FromMilliseconds(_currentSessionLength);
                        TimerText = FormatTimeSpan(sessionSpan);                        

                        
                        sessionTimer.Start();
                        _currentSessionState = SessionState.Running;
                    }
                    break;
           
            }
        }
        private async void OnCompareTremorRates()
        {

            Console.WriteLine("compare method was hit");
            await DetectTremor();

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
        private string FormatTimeSpan(TimeSpan span)
        {
            return $"Time Remaining: {span}";
        }
        private async Task DetectTremor()
        {
            _currentTremorLevel = await _accelerometerService.ProcessFftAsync(
                Constants.CompareInterval);
            var message = $"Current Tremor Velocity: {_currentTremorLevel}";
            Console.WriteLine(message);
            // Compare the magnitude to the baseline tremor level

            if (_currentTremorLevel >= _baselineTremorLevel)
            {
                _tremorCount++;
                var tremorMessage = $"Tremors Detected: {_tremorCount}";
                
                Console.WriteLine(tremorMessage);
                TremorText = tremorMessage;
            }
        }
        public ICommand StartSessionCommand { get; }
        public ICommand ViewResultsCommand { get; }
    }
}