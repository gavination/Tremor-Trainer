using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TremorTrainer.Views;
using Xamarin.Forms;

namespace TremorTrainer.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        public Command LoginCommand { get; }


        public LoginViewModel()
        {
            LoginCommand = new Command(OnLoginButtonClicked);
        }

        // todo: update this with supabase login logic
        private static async void OnLoginButtonClicked(object obj)
        {
            Console.WriteLine("Login button clicked");
            App.Current.Properties["IsInductionSession"] = true;
            await Shell.Current.GoToAsync("//AccelerometerPage");

        }
    }
}