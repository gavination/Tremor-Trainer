﻿using TremorTrainer.Repositories;
using TremorTrainer.Services;
using TremorTrainer.ViewModels;
using Unity;

namespace TremorTrainer.Utilities
{
    public class ViewModelLocator
    {
        private readonly IUnityContainer _unityContainer;

        public ViewModelLocator()
        {

            _unityContainer = new UnityContainer();

            // Register DB Instance 
            _unityContainer.RegisterInstance<IConnection>(new DbConnection(Constants.DatabasePath, Constants.Flags));

            // Register types in the constructor
            // -----------------SERVICES------------------------
            _unityContainer.RegisterType<ITimerService, TimerService>();
            _unityContainer.RegisterType<ISessionService, SessionService>();
            _unityContainer.RegisterType<IMessageService, MessageService>();
            _unityContainer.RegisterType<IAccelerometerService, AccelerometerService>();

            // -----------------REPOSITORIES------------------------
            _unityContainer.RegisterType<ISessionRepository, SessionRepository>();
            _unityContainer.RegisterType<IAccelerometerRepository, AccelerometerRepository>();

        }

        // Register types in the constructor
        // Resolving classes as they are called

        public SessionsViewModel SessionsViewModel => _unityContainer.Resolve<SessionsViewModel>();
        public ItemDetailViewModel ItemDetailViewModel => _unityContainer.Resolve<ItemDetailViewModel>();
        public NewItemViewModel NewItemViewModel => _unityContainer.Resolve<NewItemViewModel>();
        public AccelerometerViewModel AccelerometerViewModel => _unityContainer.Resolve<AccelerometerViewModel>();
        public SettingsViewModel SettingsViewModel => _unityContainer.Resolve<SettingsViewModel>();
        public GetStartedViewModel GetStartedViewModel => _unityContainer.Resolve<GetStartedViewModel>();
        public LoginViewModel LoginViewModel => _unityContainer.Resolve<LoginViewModel>();
    }
}
