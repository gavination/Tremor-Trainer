﻿using SQLite;
using TremorTrainer.Models;
using TremorTrainer.Repositories;
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
            //AsyncLazy<SessionRepository> SessionDBInstance
            //= new AsyncLazy<SessionRepository>(async () =>
            //{
            //    SQLiteAsyncConnection database = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
            //    var sessionRepository = new SessionRepository(database);

            //    CreateTableResult result = await database.CreateTableAsync<Session>();
            //    return sessionRepository;
            //});

            _unityContainer = new UnityContainer();

            // Register DB Instance 
            _unityContainer.RegisterInstance<IConnection>(new DBConnection(Constants.DatabasePath, Constants.Flags));

            // Register types in the constructor
            _unityContainer.RegisterType<ISessionRepository, SessionRepository>();
            _unityContainer.RegisterType<ISessionService, SessionService>();
            _unityContainer.RegisterType<IMessageService, MessageService>();
            
        }

        // Register types in the constructor
        // Resolving classes as they are called

        public ItemsViewModel ItemsViewModel => _unityContainer.Resolve<ItemsViewModel>();
        public ItemDetailViewModel ItemDetailViewModel => _unityContainer.Resolve<ItemDetailViewModel>();
        public NewItemViewModel NewItemViewModel => _unityContainer.Resolve<NewItemViewModel>();
        public AccelerometerViewModel AccelerometerViewModel => _unityContainer.Resolve<AccelerometerViewModel>();
    }
}
