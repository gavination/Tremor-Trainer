using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using TremorTrainer.Services;
using TremorTrainer.Views;
using Unity;
using TremorTrainer.ViewModels;
using Unity.ServiceLocation;
using CommonServiceLocator;

namespace TremorTrainer
{
    public partial class App : Application
    {
        //TODO: Replace with *.azurewebsites.net url after deploying backend to Azure
        //To debug on Android emulators run the web backend against .NET Core not IIS
        //If using other emulators besides stock Google images you may need to adjust the IP address
        public static string AzureBackendUrl =
            DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5000" : "http://localhost:5000";
        public static bool UseMockDataStore = true;

        public App()
        {
            InitializeComponent();

            var unityContainer = new UnityContainer();

            //register dependencies here
            unityContainer.RegisterType<ISessionService, SessionService>();
            unityContainer.RegisterInstance(typeof(ItemsViewModel));//optional

            var unityServiceLocator = new UnityServiceLocator(unityContainer);
            ServiceLocator.SetLocatorProvider(() => unityServiceLocator);

            if (UseMockDataStore)
                DependencyService.Register<MockDataStore>();
            else
                DependencyService.Register<AzureDataStore>();
            MainPage = new AppShell();      

        }

        protected override void OnStart()
        {
            //check cache to find user records. If none, assume first time setup
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
