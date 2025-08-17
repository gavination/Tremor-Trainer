using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TremorTrainer.Views;
using Xamarin.Essentials;
using Supabase.Core;
using Xamarin.Forms;
using TremorTrainer.Services;
using TremorTrainer.Utilities;
using Microsoft.AppCenter;

namespace TremorTrainer.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IMessageService _messageService;
        private string _username;
        private string _password;
        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged("Username");
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged("Password");
            }
        }


        public Command LoginCommand { get; }
        public Command SubmitCommand { get; }


        public LoginViewModel(IMessageService messageService)
        {
            Username = "";
            Password = "";
            LoginCommand = new Command(OnLoginButtonClicked);
            _messageService = messageService;

        }

        private async void OnLoginButtonClicked(object obj)
        {
            try
            {
                var authResult = await App.AuthService.Login(Username, Password);
                if (authResult)
                {
                    App.Current.Properties["IsInductionSession"] = true;
                    await Shell.Current.GoToAsync("//GetStartedPage");
                }
                else
                {
                    throw new InternalLoginFailedException();
                }
            }
            catch (InternalLoginFailedException ex)
            {
                await _messageService.ShowAsync("Something went wrong when trying to log in. Try again in a few minutes");
                Console.Error.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync("Login Failed: Try again using the correct username and password");
                Console.Error.WriteLine(ex.Message);
            }


        }

         

    }
}