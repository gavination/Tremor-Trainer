using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using TremorTrainer.Models;
using TremorTrainer.Services;
using TremorTrainer.Views;
using Xamarin.Forms;

namespace TremorTrainer.ViewModels
{
    public class SessionsViewModel : BaseViewModel
    {
        private Session _selectedItem;
        private readonly ISessionService _sessionService;
        private ObservableCollection<Session> _items;

        public ObservableCollection<Session> Items
        {
            get { return _items; }
            private set
            {
                _items = value;
                OnPropertyChanged("Items");
            }
        }
        public Command LoadItemsCommand { get; }
        public Command ExportSessionsCommand { get; }
        public Command<Session> ItemTapped { get; }

        public SessionsViewModel(ISessionService sessionService)
        {
            _sessionService = sessionService;

            Title = "Sessions";
            Items = new ObservableCollection<Session>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
            ItemTapped = new Command<Session>(OnItemSelected);
            ExportSessionsCommand = new Command(OnExportButtonClicked);
        }

        async Task ExecuteLoadItemsCommand()
        {
            IsBusy = true;

            try
            {
                Items.Clear();
                var items = await _sessionService.GetSessionsAsync(true);
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task OnAppearingAsync()
        {
            IsBusy = true;
            SelectedItem = null;
            await ExecuteLoadItemsCommand();

        }

        public Session SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                OnItemSelected(value);
            }
        }

        private async void OnExportButtonClicked(object obj)
        {
            var result = await _sessionService.ExportUserSessions();
            if (result)
            {
                await _sessionService.DeleteSessions();
                Items.Clear();
            }

        }

        async void OnItemSelected(Session item)
        {
            if (item == null)
                return;

            // This will push the ItemDetailPage onto the navigation stack
            await Shell.Current.GoToAsync($"{nameof(ItemDetailPage)}?{nameof(ItemDetailViewModel.ItemId)}={item.Id}");
        }
    }
}