using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Xamarin.Forms;    
using TremorTrainer.Models;
using TremorTrainer.Views;
using TremorTrainer.Services;

namespace TremorTrainer.ViewModels
{
    public class ItemsViewModel : BaseViewModel
    {
        private Session _selectedItem;

        public ObservableCollection<Session> Sessions { get; set; }
        public Command LoadItemsCommand { get; }
        public Command AddItemCommand { get; }
        public Command<Session> ItemTapped { get; }
        private readonly SessionService _sessionService;

        public ItemsViewModel()
        {
            Title = "Sessions";
            Sessions = new ObservableCollection<Session>();
            LoadItemsCommand = new Command(() => ExecuteLoadItemsCommand());
            ItemTapped = new Command<Session>(OnItemSelected);
            AddItemCommand = new Command(OnAddItem);

            _sessionService = new SessionService();
            GetSessions();
        }

        private void GetSessions()
        {
            Sessions = new ObservableCollection<Session>(_sessionService.GetSessions().Result);
        }

        void ExecuteLoadItemsCommand()
        {
            IsBusy = true;

            try
            {
                Sessions.Clear();
                var items = _sessionService.GetSessions().Result;
                foreach (var item in items)
                {
                    Sessions.Add(item);
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

        public void OnAppearing()
        {
            IsBusy = true;
            SelectedItem = null;
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

        private async void OnAddItem(object obj)
        {
            await Shell.Current.GoToAsync(nameof(NewItemPage));
        }

        async void OnItemSelected(Session item)
        {
            if (item == null)
                return;

            // This will push the ItemDetailPage onto the navigation stack
            await Shell.Current.GoToAsync($"{nameof(ItemDetailPage)}?{nameof(ItemDetailViewModel.ItemId)}={item.SessionId}");
        }
    }
}