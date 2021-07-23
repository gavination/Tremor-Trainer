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
            Application.Current.MainPage = new AppShell(Constants.AsNeededSessionTimeLimit);

        }

        private static void OnPrescribedButtonClicked(object obj)
        {
            //Todo: Check for a token to determine what content to present to the user
            Application.Current.MainPage = new AppShell(Constants.PrescribedSessionTimeLimit);

        }
    }
}
