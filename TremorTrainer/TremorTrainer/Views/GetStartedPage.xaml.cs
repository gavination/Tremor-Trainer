using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;
using TremorTrainer.ViewModels;

namespace TremorTrainer.Views
{
    /// <summary>
    /// Page to login with user name and password
    /// </summary>
    [Preserve(AllMembers = true)]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class GetStartedPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetStartedPage" /> class.
        /// </summary>
        public GetStartedPage()
        {
            this.InitializeComponent();
            BindingContext = new LoginViewModel();
            //BindingContext = _viewModel = App.Locator.ItemsViewModel;
        }
    }
}