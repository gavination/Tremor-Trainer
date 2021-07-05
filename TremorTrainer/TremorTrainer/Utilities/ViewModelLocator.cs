using System;
using System.Collections.Generic;
using System.Text;
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
            //register types in the constructor

            _unityContainer = new UnityContainer();
            _unityContainer.RegisterType<ISessionService, SessionService>();
            _unityContainer.RegisterType<IAccelerometerService, AccelerometerService>();
        }

        public ItemsViewModel ItemsViewModel => _unityContainer.Resolve<ItemsViewModel>();
        public ItemDetailViewModel ItemDetailViewModel => _unityContainer.Resolve<ItemDetailViewModel>();
        public NewItemViewModel NewItemViewModel => _unityContainer.Resolve<NewItemViewModel>();
        public AccelerometerViewModel AccelerometerViewModel => _unityContainer.Resolve<AccelerometerViewModel>();
    }
}
