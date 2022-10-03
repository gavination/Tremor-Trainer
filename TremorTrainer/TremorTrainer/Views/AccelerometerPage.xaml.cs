using Syncfusion.XForms.PopupLayout;
using System;
using TremorTrainer.ViewModels;
using Xamarin.Forms;

namespace TremorTrainer.Views
{
    public partial class AccelerometerPage : ContentPage
    {
        private readonly AccelerometerViewModel _viewModel;
        public AccelerometerPage()
        {
            InitializeComponent();
            BindingContext = _viewModel = App.Locator.AccelerometerViewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            popupLayout.Show(false);
        }
    }
}