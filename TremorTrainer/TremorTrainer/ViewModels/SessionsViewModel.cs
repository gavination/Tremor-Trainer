﻿using System;
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
        private readonly IMessageService _messageService;

        public ObservableCollection<Session> Items { get; }
        public Command LoadItemsCommand { get; }
        public Command ExportSessionsCommand { get; }
        public Command<Session> ItemTapped { get; }

        public SessionsViewModel(ISessionService sessionService, IMessageService messageService)
        {
            _sessionService = sessionService;
            _messageService = messageService;

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
                var items = await _sessionService.GetItemsAsync(true);
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
            string result = await _sessionService.ExportSessions();

            if (result != null)
            {
                await _messageService.ShowAsync($"Exported sessions to csv file at this location: {Constants.ExportPath}");
            }
            else
            {
                string errorMessage = $"Error: {Constants.UnknownErrorMessage} Details: Unable to export sessions to file";
                await _messageService.ShowAsync(errorMessage);
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