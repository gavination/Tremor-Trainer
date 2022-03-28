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
    // WHERE GAVIN LEFT OFF...
    // todo: compare the current tremor level vs the baseline
    // todo: log when the tremor is detected
    // todo: update the UI with the detected baseline value
    public class AccelerometerViewModel : BaseViewModel
    {
        private readonly IMessageService _messageService;
        private readonly ISessionService _sessionService;
        private readonly ITimerService _mainTimerService;
        private readonly IAccelerometerService _accelerometerService;

        private Timer sessiontimer;

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
        private string _tremorText = "Placeholder tremor detection text";

        private string _timerText;
        private string _sessionButtonText;
        private int _currentSessionLength;
        private int _tremorCount;
        private int _invokedCount;
        private DateTime _sessionStartTime;
        private readonly int _sampleRate;
        private double _baselineTremorLevel;
        private double _currentTremorLevel;
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

            sessiontimer = new Timer();

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

            _invokedCount = 0;


            TimerText = FormatTimeSpan(TimeSpan.FromMilliseconds(_samplingTimeLimit));


            SessionButtonText = "Start Session";

            // Register Button Press Commands 
            StartSessionCommand = new Command(async () => await ToggleSessionAsync());
            ViewResultsCommand = new Command(async () => await Shell.Current.GoToAsync("/SessionsPage"));

            // Subscribe to necessary events
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
            _mainTimerService.Timer.Elapsed += OnSamplingTimedEvent;

            sessiontimer.Interval = Constants.CompareInterval;
            sessiontimer.Elapsed += OnDetectingTimedEvent;


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
                    break;

                case SessionState.Running:

                    await WrapUpSessionAsync();
                    _mainTimerService.SessionRunning = false;
                    SessionButtonText = "Start Session";
                    _currentSessionLength = _samplingTimeLimit;

                    _currentSessionState = SessionState.Idle;
                    break;

                case SessionState.Sampling:

                    // don't bother saving session results as we never made it to the session
                    await WrapUpSessionAsync(false);
                    _mainTimerService.SessionRunning = false;
                    SessionButtonText = "Start Session";

                    _currentSessionState = SessionState.Idle;
                    break;

                case SessionState.Detecting:

                    _mainTimerService.SessionRunning = false;

                    // don't bother saving session results as we never made it to the session
                    await WrapUpSessionAsync(false);
                    SessionButtonText = "Stop Session";
                    _currentSessionState = SessionState.Idle;
                    break;
            }
        }

        private async Task WrapUpSessionAsync(bool shouldSaveSession = true)
        {
            //Accelerometer and timer wrap up
            await _mainTimerService.StopTimerAsync();
            sessiontimer.Stop();
            await _accelerometerService.StopAccelerometer();
            var sessionDuration = DateTime.Now - _sessionStartTime;
            _mainTimerService.SessionRunning = false;

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

                if (!result)
                {
                    //todo: consider throwing an exception here, perhaps.
                    string errorMessage = "Unable to save the results of your session.";
                    await _messageService.ShowAsync(errorMessage);
                }
            }
        }

        private void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
            AccelerometerData data = e.Reading;
            _accelerometerService.Readings.Add(data.Acceleration);
            string readingFormat = $"Reading: X: { data.Acceleration.X}, Y: { data.Acceleration.Y}, Z: { data.Acceleration.Z}";

            ReadingText = readingFormat;
        }

        private async void OnSamplingTimedEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                _currentSessionLength -= (int)_mainTimerService.Timer.Interval;

                //update the ui
                TimeSpan span = TimeSpan.FromMilliseconds(_currentSessionLength);
                TimerText = FormatTimeSpan(span);

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


                            //todo: update the gauge max value control on the ui here
                            //todo: create a toast notification here to inform the user of the timer change


                            // proceed to the detection session state
                            // reassign the current session time limit and restart the timer
                            // start another timer with a different interval for comparing values
                            await _accelerometerService.StartAccelerometer(_baseSessionTimeLimit);
                            _currentSessionLength = _detectionTimeLimit;
                            TimeSpan sessionSpan = TimeSpan.FromMilliseconds(_currentSessionLength);
                            TimerText = FormatTimeSpan(sessionSpan);
                            await _mainTimerService.StartTimerAsync(_currentSessionLength);
                            _mainTimerService.Timer.Elapsed += OnSessionTimedEvent;

                            sessiontimer.Start();
                            _currentSessionState = SessionState.Detecting;
                            break;

                        case SessionState.Running:

                            // todo: evaluate if this case is necessary
                            // assume this marks the end of the session

                            _mainTimerService.SessionRunning = false;
                            SessionButtonText = "Start Session";
                            await _mainTimerService.StopTimerAsync();

                            sessiontimer.Stop();
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

            // stop the session timer and deregister the event handler
            if (_currentSessionLength == 0)
            {
                await WrapUpSessionAsync();
                _mainTimerService.Timer.Elapsed -= OnSessionTimedEvent;
            }
        }

        private async void OnDetectingTimedEvent(object sender, ElapsedEventArgs e)
        {
            // Run an FFT over the newly collected values
            _invokedCount++;

            // todo: update the gauge ui control here
            if (_accelerometerService.Readings.Count > 0)
            {
                // # of times invoked
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
            if (_currentSessionLength == 0)
            {
                // this marks the end of the detection phase.
                // stop the main timer
                sessiontimer.Stop();
                Console.WriteLine($"Total Times Threshold Exceeded: {_tremorCount}");
                Console.WriteLine($"Total times invoked: {_invokedCount}");
                // proceed to the session running phase
                _currentSessionState = SessionState.Running;
            }
        }

        private string FormatTimeSpan(TimeSpan span)
        {
            return $"Time Remaining: {span}";
        }

        private int CreateTotalSessionTimeLimit()
        {
            return _sessionService.GetSessionLength(_isPrescribedSession);
        }


        public ICommand StartSessionCommand { get; }
        public ICommand ViewResultsCommand { get; }
    }
}