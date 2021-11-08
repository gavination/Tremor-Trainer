using System;
using TremorTrainer.Services;
using TremorTrainer.Utilities;
using TremorTrainer.Views;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

[assembly: ExportFont("Montserrat-Bold.ttf", Alias = "Montserrat-Bold")]
[assembly: ExportFont("Montserrat-Medium.ttf", Alias = "Montserrat-Medium")]
[assembly: ExportFont("Montserrat-Regular.ttf", Alias = "Montserrat-Regular")]
[assembly: ExportFont("Montserrat-SemiBold.ttf", Alias = "Montserrat-SemiBold")]
[assembly: ExportFont("UIFontIcons.ttf", Alias = "FontIcons")]
namespace TremorTrainer
{
    public partial class App : Application
    {
        private static ViewModelLocator _locator;
        public static ViewModelLocator Locator => _locator = _locator ?? new ViewModelLocator();
        public App()
        {
            InitializeComponent();

            MainPage = new GetStartedPage();
        }

        protected override void OnStart()
        {
            var androidAppCenterSecret = AppSettingsManager.Settings["AndroidAppCenterId"];
            AppCenter.Start( androidAppCenterSecret +
                  "uwp={Your UWP App secret here};" +
                  "ios={Your iOS App secret here}",
                  typeof(Analytics), typeof(Crashes));
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
