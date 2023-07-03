using System;
using System.Collections.Generic;
using TremorTrainer.ViewModels;
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
                Routing.RegisterRoute(nameof(SessionsPage), typeof(SessionsPage));
                Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
                Routing.RegisterRoute(nameof(GetStartedPage), typeof(GetStartedPage));

                //todo: remove these hardcoded values
                App.Current.Properties["SessionLength"] = 1000;
                App.Current.Properties["IsPrescribedSession"] = true;

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

        private async void OnMenuItemClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//GetStartedPage");
        }
    }
}