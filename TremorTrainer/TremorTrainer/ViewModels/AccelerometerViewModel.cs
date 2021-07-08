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


        public string ReadingText
        {
            get { return _readingText; }
        }
        public AccelerometerViewModel()
        {
            Title = "Start Training";
            StartAccelerometerCommand = new Command(() => ToggleAccelerometer());

            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;

        }
        void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
            var data = e.Reading;
            var readingFormat = $"Reading: X: { data.Acceleration.X}, Y: { data.Acceleration.Y}, Z: { data.Acceleration.Z}";
            
            Console.WriteLine(readingFormat);

            _readingText = readingFormat;
            OnPropertyChanged("ReadingText");
        }
        public void ToggleAccelerometer()
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
                // todo: add a toast notification to display this edge case
                throw;
            }
            catch (Exception ex)
            {
                // Other error has occurred.
                throw;
            }
        }


        public ICommand StartAccelerometerCommand { get; }
    }
}