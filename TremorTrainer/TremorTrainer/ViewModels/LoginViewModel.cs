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
        public Command PrescribedCommand { get; }
        public Command MaintenanceCommand { get; }


        public LoginViewModel()
        {
            PrescribedCommand = new Command(OnSubsequenDaysButtonClicked);
            MaintenanceCommand = new Command(OnDay1SessionClicked);
        }

        private static async void OnDay1SessionClicked(object obj)
        {
            //Todo: Check for a token to determine what content to present to the user
            //Application.Current.MainPage = new AppShell(Constants.MaintenanceSessionTimeLimit, false);
            await Shell.Current.GoToAsync("//AccelerometerPage");

        }

        private static async void OnSubsequenDaysButtonClicked(object obj)
        {
            //Todo: Check for a token to determine what content to present to the user
            //Application.Current.MainPage = new AppShell(Constants.InductionSessionTimeLimit, true);
            await Shell.Current.GoToAsync("//AccelerometerPage");

        }
    }
}
