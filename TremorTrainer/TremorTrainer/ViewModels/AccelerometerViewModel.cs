using System;
using System.Windows.Input;
using TremorTrainer.Services;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace TremorTrainer.ViewModels
{
    public class AccelerometerViewModel : BaseViewModel
    {
        private readonly IAccelerometerService accelerationService;

        private string _readingText = "Sample XYZ values";
        public string ReadingText
        {
            get { return _readingText; }
            set
            {
                _readingText = value;
                OnPropertyChanged();
            }
        }
        public AccelerometerViewModel(IAccelerometerService accelService)
        {
            accelerationService = accelService;
            Title = "Start Training";
            StartAccelerometerCommand = new Command(() => accelerationService.ToggleAccelerometer());
        }

        public ICommand StartAccelerometerCommand { get; }
    }
}