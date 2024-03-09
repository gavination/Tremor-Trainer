using TremorTrainer.ViewModels;
using Xamarin.Forms;

namespace TremorTrainer.Views
{
    /// <summary>
    /// Page to login with user name and password
    /// </summary>
    public partial class LoginPage : ContentPage
    {
        private readonly LoginViewModel _viewModel;

        public LoginPage()
        {
            InitializeComponent();
            BindingContext = _viewModel = App.Locator.LoginViewModel;

        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (Shell.Current != null)
            {
                Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            }
        }

        private void SubmitLogin(object sender, System.EventArgs e)
        {
            _viewModel.LoginCommand.Execute(sender);
        }

        private void FocusPassWordEntry(object sender, System.EventArgs e)
        {
            PasswordEntry.Focus();
        }


    }
}