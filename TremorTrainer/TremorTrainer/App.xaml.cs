using System;
using TremorTrainer.Services;
using TremorTrainer.Utilities;
using TremorTrainer.Views;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TremorTrainer
{
    public partial class App : Application
    {
        private static ViewModelLocator _locator;
        public static ViewModelLocator Locator => _locator = _locator ?? new ViewModelLocator();
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
