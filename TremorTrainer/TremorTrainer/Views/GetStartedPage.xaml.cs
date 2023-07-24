using TremorTrainer.ViewModels;
using Xamarin.Forms;

namespace TremorTrainer.Views
{
    /// <summary>
    /// Page to login with user name and password
    /// </summary>
    public partial class GetStartedPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetStartedPage" /> class.
        /// </summary>
        public GetStartedPage()
        {
            InitializeComponent();
            BindingContext = new LoginViewModel();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (Shell.Current != null)
            {
                Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            }
        }
    }
}