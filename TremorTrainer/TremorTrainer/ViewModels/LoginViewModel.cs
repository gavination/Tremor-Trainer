using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TremorTrainer.Views;
using Xamarin.Essentials;
using Supabase.Core;
using Xamarin.Forms;
using TremorTrainer.Services;

namespace TremorTrainer.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        
        public Command LoginCommand { get; }


        public LoginViewModel(IAuthService authService)
        {
            LoginCommand = new Command(OnLoginButtonClicked);
            _authService = authService;

        }

        // todo: update this with supabase login logic
        private async void OnLoginButtonClicked(object obj)
        {
            Console.WriteLine("Login button clicked");

            var authResult = await _authService.Login("test", "test");
            if (authResult)
            {
                App.Current.Properties["IsInductionSession"] = true;
                await Shell.Current.GoToAsync("//AccelerometerPage");
            }

        }
    }
}