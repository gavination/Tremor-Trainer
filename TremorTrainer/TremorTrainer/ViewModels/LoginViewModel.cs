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
            PrescribedCommand = new Command(OnDay1SessionClicked);
            MaintenanceCommand = new Command(OnSubsequentDaysButtonClicked);
        }

        private static async void OnDay1SessionClicked(object obj)
        {
            App.Current.Properties["IsInductionSession"] = true;
            await Shell.Current.GoToAsync("//AccelerometerPage");

        }

        private static async void OnSubsequentDaysButtonClicked(object obj)
        {
            App.Current.Properties["IsInductionSession"] = false;
            await Shell.Current.GoToAsync("//AccelerometerPage");
        }
    }
}