using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Essentials;


namespace TremorTrainer.Services
{
    public class AccelerometerService : IAccelerometerService
    {
        //todo: consider making this a constant value in another file
        private readonly SensorSpeed speed = SensorSpeed.UI;

        public AccelerometerService()
        {
            //Note: this constructor registers for reading the Accelerometer. Be sure to unsubscribe when finished
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;

        }

        void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
            //printing acceleration data to the console
            var data = e.Reading;
            Console.WriteLine($"Reading: X: {data.Acceleration.X}, Y: {data.Acceleration.Y}, Z: {data.Acceleration.Z}");
        }

        public void ToggleAccelerometer()
        {
            try
            {
                if (Accelerometer.IsMonitoring)
                    Accelerometer.Stop();
                else
                    Accelerometer.Start(speed);
            }
            catch (FeatureNotSupportedException ex)
            {
                // Feature not supported on device
                // todo: add a toast notification to display this edge case

            }
            catch (Exception ex)
            {
                // Other error has occurred.
            }
        }
    }

    public interface IAccelerometerService
    {
        void ToggleAccelerometer();

    }

}
