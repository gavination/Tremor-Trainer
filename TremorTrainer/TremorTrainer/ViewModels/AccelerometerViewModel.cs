using System;
using System.Windows.Input;
using TremorTrainer.Services;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace TremorTrainer.ViewModels
{
    public class AccelerometerViewModel : BaseViewModel
    {
        private IAccelerometerService accelerationService;

        public string ReadingText;
        public AccelerometerViewModel(IAccelerometerService accelService)
        {
            accelerationService = accelService;
            Title = "Start Training";
            StartAccelerometerCommand = new Command(() => accelerationService.ToggleAccelerometer());
        }

        public ICommand StartAccelerometerCommand { get; }
    }
}