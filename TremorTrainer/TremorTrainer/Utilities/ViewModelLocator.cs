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
            // Register types in the constructor
            _unityContainer = new UnityContainer();
            //_unityContainer.RegisterType<ISessionRepository, SessionRepository>();
            _unityContainer.RegisterType<ISessionService, SessionService>();
            _unityContainer.RegisterType<IMessageService, MessageService>();
            _unityContainer.RegisterSingleton<ISessionRepository, SessionRepository>();
        }

        // Resolving ViewModels as they are called
        public ItemsViewModel ItemsViewModel => _unityContainer.Resolve<ItemsViewModel>();
        public ItemDetailViewModel ItemDetailViewModel => _unityContainer.Resolve<ItemDetailViewModel>();
        public NewItemViewModel NewItemViewModel => _unityContainer.Resolve<NewItemViewModel>();
        public AccelerometerViewModel AccelerometerViewModel => _unityContainer.Resolve<AccelerometerViewModel>();
    }
}
