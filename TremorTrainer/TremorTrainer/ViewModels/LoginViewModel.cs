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

        private static async void OnLoginButtonClicked(object obj)
        {
            await Shell.Current.GoToAsync("//GetStartedPage");

        }

    }
}