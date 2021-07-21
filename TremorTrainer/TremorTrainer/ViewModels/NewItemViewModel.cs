using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using TremorTrainer.Models;
using TremorTrainer.Services;
using Xamarin.Forms;

namespace TremorTrainer.ViewModels
{
    public class NewItemViewModel : BaseViewModel
    {
        private string _text;
        private string _description;

        private readonly ISessionService _dataStore;

        public NewItemViewModel(ISessionService dataStore)
        {
            _dataStore = dataStore;

            SaveCommand = new Command(OnSave, ValidateSave);
            CancelCommand = new Command(OnCancel);
            this.PropertyChanged +=
                (_, __) => SaveCommand.ChangeCanExecute();
        }

        private bool ValidateSave()
        {
            return !String.IsNullOrWhiteSpace(_text)
                && !String.IsNullOrWhiteSpace(_description);
        }

        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        private async void OnCancel()
        {
            // This will pop the current page off the navigation stack
            await Shell.Current.GoToAsync("..");
        }

        private async void OnSave()
        {
            Session newItem = new Session()
            {
                Id = Guid.NewGuid(),
                Text = Text,
                Description = Description
            };

            await _dataStore.AddItemAsync(newItem);

            // This will pop the current page off the navigation stack
            await Shell.Current.GoToAsync("..");
        }
    }
}
