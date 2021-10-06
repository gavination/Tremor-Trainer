using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xamarin.Essentials;

namespace TremorTrainer
{
    public static class Constants
    {
        //Application runtime constants
        public static readonly SensorSpeed SensorSpeed = SensorSpeed.UI;
        public const string DatabaseFilename = "TremorTrainer.db3";
        public const string CSVFileName = "TremorTrainerSessions.csv";
        public const int PrescribedSessionTimeLimit = 30000;
        public const int AsNeededSessionTimeLimit = 15000;
        public const int FirstPrescribedSessionTimeLimit = 60000;
        public const int CountdownInterval = 1000;
        public const string BuildNumber = "0.0.1";


        // Debug, info, and exception messages
        public const string ContactEmail = "gavin@bionicpanda.net";
        public const string AppName = "Tremor Trainer";
        public const string DeviceNotSupportedMessage = "Unfortunately, this device does not have an Accelerometer and we cannot measure your tremor levels";
        public static string UnknownErrorMessage = $"An Unknown error has occurred. Please contact the Developer at {ContactEmail}";

        public const string AboutMessage = "Tremor Trainer is a cross-platform mobile app lovingly built using Xamarin.Forms 5 by a team of cybernetic pandas. The application seeks to provide access to frontline therapy for those suffering from Functional Tremors. At the time of implementation, there are no known methodologies for providing a means to temper the effects of functional tremors other than Tremor Trainer. Thanks for the combined research and efforts of Drs. Jordan Garris, Alberto Espay, and Amanda Lin, we were able to implement those learnings into a simple to use mobile application that requires not much more than a smartphone (Android, iOS) with an Accelerometer.";

        public const SQLite.SQLiteOpenFlags Flags =
            // open the database in read/write mode
            SQLite.SQLiteOpenFlags.ReadWrite |
            // create the database if it doesn't exist
            SQLite.SQLiteOpenFlags.Create |
            // enable multi-threaded database access
            SQLite.SQLiteOpenFlags.SharedCache;
        // encrypting the file while the device is locked
        //SQLite.SQLiteOpenFlags.ProtectionComplete;

        public static string DatabasePath
        {
            get
            {
                string basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(basePath, DatabaseFilename);
            }
        }
        public static string ExportPath
        {
            get
            {
                string basePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                return Path.Combine(basePath, CSVFileName);
            }
        }

        
    }
}
