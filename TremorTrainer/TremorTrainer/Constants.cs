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
        public static readonly SensorSpeed SENSOR_SPEED = SensorSpeed.UI;
        public const string DATABASE_FILENAME = "TremorTrainer.db3";
        public const int PRESCRIBED_SESSION_TIME_LIMIT = 30000;
        public const int AS_NEEDED_SESSION_TIME_LIMIT = 15000;

        // Debug and exception messages
        public const string CONTACT_EMAIL = "gavin@bionicpanda.net";
        public const string APP_NAME = "Tremor Trainer";
        public const string DEVICE_NOT_SUPPORTED_MESSAGE = "Unfortunately, this device does not have an Accelerometer and we cannot measure your tremor levels";
        public static string UNKNOWN_ERROR_MESSAGE = $"An Unknown error has occurred. Please contact the Developer at {CONTACT_EMAIL}";

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
                return Path.Combine(basePath, DATABASE_FILENAME);
            }
        }
    }
}
