using System;
using System.Collections.Generic;
using System.Text;
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
            PrescribedCommand = new Command(OnPrescribedButtonClicked);
            MaintenanceCommand = new Command(OnMaintenanceButtonClicked);
        }

        private static void OnMaintenanceButtonClicked(object obj)
        {
            //Todo: Check for a token to determine what content to present to the user
            Application.Current.MainPage = new AppShell(Constants.MaintenanceSessionTimeLimit, false);

        }

        private static void OnPrescribedButtonClicked(object obj)
        {
            //Todo: Check for a token to determine what content to present to the user
            Application.Current.MainPage = new AppShell(Constants.InductionSessionTimeLimit, true);

        }
    }
}
