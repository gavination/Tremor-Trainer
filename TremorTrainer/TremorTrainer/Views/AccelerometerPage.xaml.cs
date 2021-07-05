using System;
using System.ComponentModel;
using TremorTrainer.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TremorTrainer.Views
{
    public partial class AccelerometerPage : ContentPage
    {
        AccelerometerViewModel _viewModel;
        public AccelerometerPage()
        {
            InitializeComponent();

            BindingContext = _viewModel = App.Locator.AccelerometerViewModel;
        }
    }
}