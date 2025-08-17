using System;
using TremorTrainer.Views;
using Xamarin.Forms;

namespace TremorTrainer
{
    public partial class AppShell : Xamarin.Forms.Shell
    {

        public AppShell()
        {
            InitializeComponent();
            try
            {
                Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
                Routing.RegisterRoute(nameof(NewItemPage), typeof(NewItemPage));

                // disables the hamburger menu
                // needed to ensure we don't show it on the GetStartedPage
                this.FlyoutBehavior = FlyoutBehavior.Disabled;
            }
            catch (Exception e)
            {
                //todo: replace this with user friendly message
                var message = $"Error: Something went wrong. Details {e.Message}";
                Console.WriteLine(message);
                App.Current.MainPage.DisplayAlert(Constants.AppName, message, "Ok");
                throw;
            }
        }

        private async void OnLogoutButtonClicked(object sender, EventArgs e)
        {
            await App.AuthService.Logout();
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}