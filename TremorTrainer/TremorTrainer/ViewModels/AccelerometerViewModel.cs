using System;
using System.ComponentModel;
using System.Windows.Input;
using TremorTrainer.Services;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace TremorTrainer.ViewModels
{
    public class AccelerometerViewModel : BaseViewModel
    {
        private string _readingText = "Sample XYZ values";
        private readonly IMessageService _messageService;

        public string ReadingText
        {
            get { return _readingText; }
        }
        public AccelerometerViewModel(IMessageService messageService)
        {
            Title = "Start Training";
            StartAccelerometerCommand = new Command(() => ToggleAccelerometer());
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
            _messageService = messageService;
        }
        void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
            var data = e.Reading;
            var readingFormat = $"Reading: X: { data.Acceleration.X}, Y: { data.Acceleration.Y}, Z: { data.Acceleration.Z}";
            
            Console.WriteLine(readingFormat);

            _readingText = readingFormat;
            OnPropertyChanged("ReadingText");
        }
        public async void ToggleAccelerometer()
        {
            try
            {
                if (Accelerometer.IsMonitoring)
                    Accelerometer.Stop();
                else
                    Accelerometer.Start(Constants.SENSOR_SPEED);
            }
            catch (FeatureNotSupportedException ex)
            {
                // Feature not supported on device
                await _messageService.ShowAsync(Constants.DEVICE_NOT_SUPPORTED_MESSAGE);

            }
            catch (Exception ex)
            {
                // Other unknown error has occurred.
                await _messageService.ShowAsync(Constants.UNKNOWN_ERROR_MESSAGE);
                throw ex;

            }
        }


        public ICommand StartAccelerometerCommand { get; }
    }
}