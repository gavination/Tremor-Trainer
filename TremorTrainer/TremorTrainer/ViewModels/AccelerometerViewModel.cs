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
        private bool _isSampling;
        

        // todo: replace this once code solidifies
        private string _readingText = "Accelerometer values will appear here.";
        private string _timerText;
        private string _sessionButtonText;
        private int _currentSessionLength;
        private DateTime _sessionStartTime;
        private int _sampleRate;
        private float _baselineTremorLevel;
        private float _currentTremorLevel;

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
            _isSampling = false;
            _samplingTimeLimit = Constants.SamplingTimeLimit;
            _downSampleRate = Constants.DownSampleRate;
            _baseSessionTimeLimit = _sessionService.GetSessionLength(_isPrescribedSession);
            _currentSessionLength = _samplingTimeLimit;
            _baselineTremorLevel = new float();
            _currentTremorLevel = new float();
            _sampleRate = 0;


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

            // Determine if this is the start of a new Session
            if(_currentSessionLength == _samplingTimeLimit && !_mainTimerService.SessionRunning)
            {
                // Start the accelerometer and the timer
                _mainTimerService.SessionRunning = true;
                _sessionStartTime = DateTime.Now;
                _isSampling = true;

                await _accelerometerService.StartAccelerometer(_currentSessionLength);
                await _mainTimerService.StartTimerAsync(_currentSessionLength);

                SessionButtonText = "Stop Session";

            }
            // Determine if restarting a session after a previous run
            else if (_currentSessionLength <= 0 && !_mainTimerService.SessionRunning)
            {
                // reset the session length and start a new Session
                _mainTimerService.SessionRunning = true;
                _sessionStartTime = DateTime.Now;

                _currentSessionLength = _samplingTimeLimit;

                TimeSpan span = TimeSpan.FromMilliseconds(_currentSessionLength);
                TimerText = FormatTimeSpan(span);

                await _accelerometerService.StartAccelerometer(_currentSessionLength);
                await _mainTimerService.StartTimerAsync(_currentSessionLength);

                SessionButtonText = "Stop Session";


            }
            // Determine if the session is still running
            // Cancel the run and save the current status as a result
            else if (_currentSessionLength >= 0 && _mainTimerService.SessionRunning)
            {
                // Creates a timestamp if none exists yet
                if (_sessionStartTime == null)
                {
                    _sessionStartTime = DateTime.Now;
                }

                if (!_isSampling)
                {
                    // Stop the Running Session, save the results, and dispose of the timer
                    await WrapUpSessionAsync();
                }
                _mainTimerService.SessionRunning = false;

                SessionButtonText = "Start Session";
            }
            // Determine if the session was stopped and a new one needs to be created
            // Reset the current session length, flip the SessionRunning bool, and restart the timer
            else if(_currentSessionLength > 0 && !_mainTimerService.SessionRunning)
            {
                _sessionStartTime = DateTime.Now;
                _currentSessionLength = _samplingTimeLimit;
                _mainTimerService.SessionRunning = true;
                _isSampling = true;

                await _accelerometerService.StartAccelerometer(_currentSessionLength);
                await _mainTimerService.StartTimerAsync(_currentSessionLength);

                TimeSpan span = TimeSpan.FromMilliseconds(_currentSessionLength);
                TimerText = FormatTimeSpan(span);

                SessionButtonText = "Stop Session";
            }
        }

        private async Task WrapUpSessionAsync()
        {
            //Accelerometer and timer wrap up
            await _mainTimerService.StopTimerAsync();
            await _accelerometerService.StopAccelerometer();
            var sessionDuration = DateTime.Now - _sessionStartTime;
            _mainTimerService.SessionRunning = false;

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

                //update the ui with a propertychanged event here
                TimeSpan span = TimeSpan.FromMilliseconds(_currentSessionLength);

                TimerText = FormatTimeSpan(span);


                if (_currentSessionLength == 0 && _isSampling)
                {
                    // get sampling rate from the samples derived over time.
                    // converts ms to s and passes it over to the AccelerometerService
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
                    _isSampling = false;
                    await _accelerometerService.StartAccelerometer(_baseSessionTimeLimit);
                    _currentSessionLength = _baseSessionTimeLimit;
                    await _mainTimerService.StartTimerAsync(_currentSessionLength);
                    _sessionTimerService.Interval = Constants.CompareInterval;
                    _sessionTimerService.Timer.Elapsed += OnSessionTimedEvent;
                    await _sessionTimerService.StartTimerAsync(_currentSessionLength);
                }

                else if (_currentSessionLength == 0)
                {
                    // assume this marks the end of the session 
                    _mainTimerService.SessionRunning = false;
                    SessionButtonText = "Start Session";
                    await _mainTimerService.StopTimerAsync();
                    await _sessionTimerService.StopTimerAsync();
                    await _accelerometerService.StopAccelerometer();
                    await WrapUpSessionAsync();
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
            Console.WriteLine($"Current Tremor Level: {_currentTremorLevel}");
            // Compare the magnitude to the baseline tremor level

            if (_currentTremorLevel >= _baselineTremorLevel)
            {
                Console.WriteLine("Tremor Detected");
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