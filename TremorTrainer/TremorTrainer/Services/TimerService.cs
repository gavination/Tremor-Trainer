﻿using System;
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
        private bool _sessionRunning;
        private IMessageService _messageService;
        private readonly int _interval;

        public TimeSpan Span => _span;
        public Timer Timer => _timer;
        public int Interval => _interval;
        public bool SessionRunning { 
            get => _sessionRunning;
            set => _sessionRunning = value; }

        public TimerService(IMessageService messageService)
        {
            _timer = new Timer();
            _interval = Constants.CountdownInterval;
            _timer.Interval = Constants.CountdownInterval;
            _sessionRunning = false;
            _messageService = messageService;
        }

        public string FormatTimeSpan(TimeSpan span)
        {
            return $"Time Remaining: {(int)span.TotalMinutes}:{(int)span.TotalSeconds}";
        }

        public async Task StartTimerAsync(int timerLength)
        {
            if (timerLength > 0 && !_sessionRunning)
            {
                //trigger a timer event every for every interval that passes 
                _timer.Enabled = true;
                _sessionRunning = true;
            }
            else if (!_sessionRunning && timerLength <= 0)
            {
                //reset the session length to start a new one
                timerLength = (int)App.Current.Properties["SessionLength"];
                _timer = new Timer(_interval);
                _sessionRunning = true;

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
        int Interval { get; }
        bool SessionRunning { get; set; }
        string FormatTimeSpan(TimeSpan span);
        Task StartTimerAsync(int timerLength);
        Task StopTimerAsync();
    }
}
