using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace TremorTrainer.Services
{
    public class TimerService : ITimerService
    {
        private Timer _timer;
        private TimeSpan _span;
        private bool _isSessionRunning;
        private IMessageService _messageService;
        private readonly int _interval;

        public TimeSpan Span => _span;
        public Timer Timer => _timer;


        public TimerService(IMessageService messageService)
        {
            _timer = new Timer();
            _isSessionRunning = false;
            _interval = Constants.CountdownInterval;
        }

        public string FormatTimeSpan(TimeSpan span)
        {
            return $"Time Remaining: {(int)span.TotalMinutes}:{(int)span.TotalSeconds}";
        }

        public async Task StartTimerAsync(int timerLength)
        {
            if (timerLength > 0 && !_isSessionRunning)
            {
                //trigger a timer event every for every interval that passes 
                _timer.Enabled = true;
                _isSessionRunning = true;
            }
            else if (!_isSessionRunning && timerLength <= 0)
            {
                //reset the session length to start a new one
                timerLength = (int)App.Current.Properties["SessionLength"];
                _timer = new Timer(_interval);
                _isSessionRunning = true;

                //trigger a timer event every for every interval that passes
                _timer.Enabled = true;
            }
            else
            {
                // session is still active if boolean is true
                await _messageService.ShowAsync("Session is already active.");
            }
        }

        public async Task StopTimerAsync()
        {
            try
            {
                _timer.Stop();
                _timer.Enabled = false;
                _timer.Dispose();
            }
            catch (Exception e)
            {
                var message = $"{Constants.UnknownErrorMessage}. Details: {e.Message}";
                Console.WriteLine(message);
                await _messageService.ShowAsync(Constants.UnknownErrorMessage);
            }

        }
    }

    public interface ITimerService
    {
        Timer Timer { get; }
        string FormatTimeSpan(TimeSpan span);
        Task StartTimerAsync(int timerLength);
        Task StopTimerAsync();
    }
}
