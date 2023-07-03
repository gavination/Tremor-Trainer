using System;
using TremorTrainer.Utilities;
using TremorTrainer.Views;
using Xamarin.Forms;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Syncfusion.Licensing;

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
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(AppSettingsManager.Settings["SyncfusionCommunityLicenseKey"]);
            InitializeComponent();

            MainPage = new GetStartedPage();
        }

        protected override void OnStart()
        {
            try
            {
#if DEBUG
                var androidAppCenterSecret = AppSettingsManager.Settings["AndroidAppCenterId"];
                var iosAppCenterSecret = AppSettingsManager.Settings["IOSAppCenterId"];
#endif

#if RELEASE
                // todo: remove this and add a pre-build script to generate appsettings.json
                var androidAppCenterSecret = Environment.GetEnvironmentVariable("AndroidAppCenterId");
                var iosAppCenterSecret = Environment.GetEnvironmentVariable("IOSAppCenterId");
#endif


                AppCenter.LogLevel = LogLevel.Verbose;
                AppCenter.Start(androidAppCenterSecret +
                      iosAppCenterSecret,
                      typeof(Analytics), typeof(Crashes));

                // todo: remove this once pre-build script is in place. 
                if (androidAppCenterSecret != null && androidAppCenterSecret.Length > 0)
                {
                    // assume the value actually gets populated and this works. 
                    Analytics.TrackEvent
                        ($"android app center secret is present on this build. character length: ${androidAppCenterSecret.Length}");

                }
            }
            catch (NullReferenceException ex)
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
            // todo: teardown the accelerometer and timing fixtures on sleep
        }

        protected override void OnResume()
        {
            // todo: this fails with a null reference when killed and relaunched
            MainPage = new GetStartedPage();

        }
    }
}
