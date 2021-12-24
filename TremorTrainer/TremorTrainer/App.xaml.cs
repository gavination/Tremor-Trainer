using System;
using TremorTrainer.Utilities;
using TremorTrainer.Views;
using Xamarin.Forms;
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
            try
            {
                var androidAppCenterSecret = AppSettingsManager.Settings["AndroidAppCenterId"];
                var iosAppCenterSecret = AppSettingsManager.Settings["IOSAppCenterId"];
                AppCenter.Start(androidAppCenterSecret +
                      iosAppCenterSecret,
                      typeof(Analytics), typeof(Crashes));
            }
            catch(NullReferenceException ex)
            {
                var errorMessage = "Unable to fetch appsettings for the mobile client. " +
                    "Ensure the appsettings file is present in the shared project and the Build Action is set to EmbeddedResource";
                ex.Data.Add("ErrorDetails", errorMessage);
            }
            catch (Exception ex)
            {
                var errorMessage = "An unknown error occurred during app startup. Confirm configuration details are properly set";
                ex.Data.Add("ErrorDetails", errorMessage);
            }

        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
