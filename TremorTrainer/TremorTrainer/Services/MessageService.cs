using System.Threading.Tasks;
using Xamarin.Forms;

namespace TremorTrainer.Services
{
    public class MessageService : IMessageService
    {
        public Task ShowAsync(string message)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await Application.Current.MainPage.DisplayAlert(Constants.AppName, message, "Ok");

            });
            return Task.CompletedTask;
        }

        public async Task<bool> ShowCancelAlertAsync(string title, string message)
        {

            return await App.Current.MainPage.DisplayAlert(title, message, "OK", "Cancel");
        }
    }

    public interface IMessageService
    {
        Task ShowAsync(string message);
        Task<bool> ShowCancelAlertAsync(string title, string message);
    }
}
