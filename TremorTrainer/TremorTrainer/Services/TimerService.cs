﻿using System;
using System.Threading.Tasks;
using System.Timers;

namespace TremorTrainer.Services
{
    public class TimerService : ITimerService
    {
        // todo: DESTROY THIS 
        private Timer _timer;
        private bool _sessionRunning;
        private IMessageService _messageService;
        private int _interval;

        public Timer Timer => _timer;
        public int Interval
        {
            get => _interval;
            set => _interval = value;
        }
        public bool SessionRunning
        {
            get => _sessionRunning;
            set => _sessionRunning = value;
        }

        public TimerService(IMessageService messageService)
        {
            _timer = new Timer();
            _interval = Constants.CountdownInterval;
            _timer.Interval = Constants.CountdownInterval;
            _sessionRunning = false;
            _messageService = messageService;
        }

        public Task StartTimerAsync(int timerLength)
        {
            if (timerLength > 0)
            {
                //trigger a timer event every for every interval that passes 
                _timer.Enabled = true;
                _sessionRunning = true;
            }
            else
            {
                //reset the session length to start a new one
                //todo: remove this once we validate we no longer need it
                timerLength = (int)Xamarin.Forms.Application.Current.Properties["SessionLength"];
                _timer = new Timer(_interval);
                _sessionRunning = true;

                //trigger a timer event every for every interval that passes
                _timer.Enabled = true;
            }

            return Task.CompletedTask;
        }

        public async Task StopTimerAsync()
        {
            try
            {
                _timer.Stop();
                _timer.Enabled = false;
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
        bool SessionRunning { get; set; }
        Task StartTimerAsync(int timerLength);
        Task StopTimerAsync();
    }
}
