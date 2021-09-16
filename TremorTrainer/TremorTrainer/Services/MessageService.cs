﻿using System.Threading.Tasks;

namespace TremorTrainer.Services
{
    public class MessageService : IMessageService
    {
        public async Task ShowAsync(string message)
        {
            await App.Current.MainPage.DisplayAlert(Constants.AppName, message, "Ok");
            }
    }

    public interface IMessageService
    {
        Task ShowAsync(string message);
    }
}
