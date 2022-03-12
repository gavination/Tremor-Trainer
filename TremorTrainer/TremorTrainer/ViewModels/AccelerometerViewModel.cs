using MathNet.Numerics;
using System;
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
        private readonly IMessageService _messageService;
        private readonly ISessionService _sessionService;
        private readonly ITimerService _mainTimerService;
        private readonly ITimerService _sessionTimerService;
        private readonly IAccelerometerService _accelerometerService;
          
        // setup private timer and ui vars
        private readonly int _baseSessionTimeLimit;
        private readonly int _samplingTimeLimit;
        private readonly bool _isPrescribedSession;
        private readonly int _downSampleRate;

        private enum SessionState
        {
            Idle,
            Sampling,
            Detecting,
            Running,
        }

        // todo: replace this once code solidifies
        private string _readingText = "Accelerometer values will appear here.";
        private string _timerText;
        private string _tremorText = "Placeholder tremor detection text";
        private string _sessionButtonText;
        private int _currentSessionLength;
        private DateTime _sessionStartTime;
        private int _sampleRate;
        private float _baselineTremorLevel;
        private float _currentTremorLevel;
        private SessionState _currentSessionState;


        public string TremorText
        {
            get { return _tremorText; }
            private set
            {
                _tremorText = value;
                OnPropertyChanged("TremorText");
            }
        }
            
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
            ITimerService sessionTimerService,
            IAccelerometerService accelerometerService)
        {
            // Fulfill external services 
            _messageService = messageService;
            _sessionService = sessionService;
            _mainTimerService = timerService;
            _sessionTimerService = sessionTimerService;
            _accelerometerService = accelerometerService;

            // ViewModel Page Setup
            // Setup UI elements and register propertychanged events
            Title = "Start Training";
            _isPrescribedSession = (bool)App.Current.Properties["IsPrescribedSession"];
            _samplingTimeLimit = Constants.SamplingTimeLimit;
            _downSampleRate = Constants.DownSampleRate;
            _baseSessionTimeLimit = _sessionService.GetSessionLength(_isPrescribedSession);
            _currentSessionLength = _samplingTimeLimit;
            _baselineTremorLevel = new float();
            _currentTremorLevel = new float();
            _sampleRate = 50;
            _currentSessionState = SessionState.Idle;


            TimerText = FormatTimeSpan(TimeSpan.FromMilliseconds(_samplingTimeLimit));
            

            SessionButtonText = "Start Session";

            // Register Button Press Commands 
            StartSessionCommand = new Command(async () => await ToggleSessionAsync());
            ViewResultsCommand = new Command(async () => await Shell.Current.GoToAsync("/SessionsPage"));

            // Subscribe to necessary events
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
            _mainTimerService.Timer.Elapsed += OnSamplingTimedEvent;

        }

        private async Task ToggleSessionAsync()
        {
            // todo: have the service methods return booleans to determine
            // whether to record start time and flipping the sessionRunning var


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

                    _currentSessionState = SessionState.Idle;
                    break;

                case SessionState.Sampling:

                    await WrapUpSessionAsync(false);
                    _mainTimerService.SessionRunning = false;
                    SessionButtonText = "Stop Session";

                    _currentSessionState = SessionState.Idle;
                    break;

            }

        }

        private async Task WrapUpSessionAsync(bool shouldSaveSession = true)
        {
            //Accelerometer and timer wrap up
            await _mainTimerService.StopTimerAsync();
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
                _currentSessionLength -= _mainTimerService.Interval;

                //update the ui
                TimeSpan span = TimeSpan.FromMilliseconds(_currentSessionLength);

                TimerText = FormatTimeSpan(span);

                if (_currentSessionLength == 0)
                {
                    switch (_currentSessionState)
                    {
                        case SessionState.Sampling:
                            Console.WriteLine($"Sample Rate: {_sampleRate} samples per second");

                            Console.WriteLine("Processing values...");
                            _baselineTremorLevel = await _accelerometerService.ProcessFFTAsync(_downSampleRate, _samplingTimeLimit);

                            Console.WriteLine($"Baseline Tremor Magnitude: {_baselineTremorLevel}");

                            // stop the timer and unsubscribe from the event here
                            await _mainTimerService.StopTimerAsync();
                            await _accelerometerService.StopAccelerometer();
                            _accelerometerService.Readings.Clear();

                            //todo: update the gauge max value control on the ui here
                            //todo: create a toast notification here to inform the user of the timer change


                            // proceed to the main session state
                            // reassign the current session time limit and restart the timer
                            // start another timer with a different interval for comparing values
                            await _accelerometerService.StartAccelerometer(_baseSessionTimeLimit);
                            _currentSessionLength = _baseSessionTimeLimit;
                            await _mainTimerService.StartTimerAsync(_currentSessionLength);
                            _sessionTimerService.Interval = Constants.CompareInterval;
                            _sessionTimerService.Timer.Elapsed += OnSessionTimedEvent;
                            await _sessionTimerService.StartTimerAsync(_currentSessionLength);
                            _currentSessionState = SessionState.Running;
                            break;
                        
                        case SessionState.Running:

                            // assume this marks the end of the session 
                            _mainTimerService.SessionRunning = false;
                            SessionButtonText = "Start Session";
                            await _mainTimerService.StopTimerAsync();
                            await _sessionTimerService.StopTimerAsync();
                            await _accelerometerService.StopAccelerometer();
                            await WrapUpSessionAsync();
                            _currentSessionState = SessionState.Idle;
                            break;
                    }
                }
            }
            catch(Exception ex)
            {
                await _messageService.ShowAsync($"An unknown error has occurred. Contact the team with the error details: {ex.Message}");
                throw;
            }

            
        }
        private async void OnSessionTimedEvent(object sender, ElapsedEventArgs e)
        {
            // Run an FFT over the newly collected values
            _currentTremorLevel = await _accelerometerService.ProcessFFTAsync(_downSampleRate, Constants.CompareInterval);
            var message = $"Current Tremor Level: {_currentTremorLevel}";
            Console.WriteLine(message);
            TremorText = message;
            // Compare the magnitude to the baseline tremor level

            if (_currentTremorLevel >= _baselineTremorLevel)
            {
                var tremorMessage = "Tremor Detected!";
                Console.WriteLine(tremorMessage);
                TremorText = tremorMessage;
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
        public ICommand SaveSessionCommand { get; }
        public ICommand ViewResultsCommand { get; }
    }
}