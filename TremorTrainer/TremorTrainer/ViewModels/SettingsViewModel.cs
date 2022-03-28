using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TremorTrainer.Services;
using Xamarin.Forms;

namespace TremorTrainer.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly ISessionService _sessionService;
        private readonly IMessageService _messageService;

        private string _buildIdMessage = "Build number: 0000";

        public Command SessionExportCommand { get; }
        public Command SessionsDeleteCommand { get; }
        public Command ShowAppInfoCommand { get; }

        public string BuildIdMessage
        {
            get { return _buildIdMessage; }
            private set
            {
                _buildIdMessage = value;
                OnPropertyChanged("BuildIdMessage");
            }
        }

        public SettingsViewModel(ISessionService sessionService, IMessageService messageService)
        {
            _sessionService = sessionService;
            _messageService = messageService;

            SessionExportCommand = new Command(OnExportButtonClicked);
            SessionsDeleteCommand = new Command(OnDeleteButtonClicked);
            ShowAppInfoCommand = new Command(OnAboutButtonClicked);

            BuildIdMessage = $"Build number: {Constants.BuildNumber}";

        }

        private async void OnDeleteButtonClicked(object obj)
        {
            string messageTitle = "Delete All Session Data?";
            string message = "Are you sure you want to delete all session data? If you have not used the Export option, this data will be deleted forever.";
            var result = await _messageService.ShowCancelAlertAsync(messageTitle, message);
            if (result)
            {
                _sessionService.DeleteSessions();
                await _messageService.ShowAsync("User Session data has been deleted");
            }

        }

        private async void OnExportButtonClicked(object obj)
        {

            await _sessionService.ExportUserSessions();
        }

        private async void OnAboutButtonClicked(object obj)
        {

            await _messageService.ShowAsync(Constants.AboutMessage);
        }
    }
}
