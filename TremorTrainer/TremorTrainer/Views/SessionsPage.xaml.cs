using TremorTrainer.ViewModels;
using Xamarin.Forms;

namespace TremorTrainer.Views
{
    public partial class SessionsPage : ContentPage
    {
        SessionsViewModel _viewModel;

        public SessionsPage()
        {
            InitializeComponent();

            BindingContext = _viewModel = App.Locator.SessionsViewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.OnAppearingAsync();
        }

    }
}