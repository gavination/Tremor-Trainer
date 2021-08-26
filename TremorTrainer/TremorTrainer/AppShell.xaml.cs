using System;
using System.Collections.Generic;
using TremorTrainer.ViewModels;
using TremorTrainer.Views;
using Xamarin.Forms;

namespace TremorTrainer
{
    public partial class AppShell : Xamarin.Forms.Shell
    {

        public AppShell(int sessionLength)
        {
            InitializeComponent();
            try
            {
                Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
                Routing.RegisterRoute(nameof(NewItemPage), typeof(NewItemPage));
                Routing.RegisterRoute(nameof(SessionsPage), typeof(SessionsPage));

                if (!App.Current.Properties.ContainsKey("SessionLength"))
                {
                    App.Current.Properties.Add("SessionLength", sessionLength);
                }
                else
                {
                    Console.WriteLine($"The Session Length argument already exists. Overwriting it with the new value");
                    App.Current.Properties["SessionLength"] = sessionLength;
                }
            }
            catch (Exception e)
            {
                //todo: replace this with user friendly message
                // NEVER ADMIT FAULT
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