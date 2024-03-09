using TremorTrainer.ViewModels;
using Xamarin.Forms;

namespace TremorTrainer.Views
{
    /// <summary>
    /// Page to select the type of session to start
    /// </summary>
    public partial class GetStartedPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetStartedPage" /> class.
        /// </summary>
        public GetStartedPage()
        {
            InitializeComponent();
            BindingContext = new GetStartedViewModel();
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