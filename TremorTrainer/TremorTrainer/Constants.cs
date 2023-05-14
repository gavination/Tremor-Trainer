using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xamarin.Essentials;

namespace TremorTrainer
{
    public static class Constants
    {
        // Application runtime constants

        // SensorSpeed: defines the rate at which the device polls for accelerometer data. Faster = more accurate
        public static readonly SensorSpeed SensorSpeed = SensorSpeed.Fastest;

        // DatabaseFilename: the name of the DB file that will be saved locally to the device
        private const string DatabaseFilename = "TremorTrainer.db3";

        // CsvFileName: the name of the exported file reflecting DB records
        private const string CsvFileName = "TremorTrainerSessions.csv";

        // FirstPrescribedSessionTimeLimit: the time span (in ms) for the first, typically longer, Prescribed Session
        public const int FirstPrescribedSessionTimeLimit = 3600000;

        // PrescribedSessionTimeLimit: the time span (in ms) for a running Prescribed Session run after the first
        public const int InductionSessionTimeLimit = 10000;

        // AsNeededSessionTimeLimit: the time span for a running As Needed Session
        public const int MaintenanceSessionTimeLimit = 1800000;

        // SamplingTimeLimit: the time span for the Sampling state run before the Session starts
        public const int SamplingTimeLimit = 10000;

        // CountdownInterval: the time interval, in milliseconds, at which the timer counts down. 1000 ms is recommmended
        public const int CountdownInterval = 1000;

        // DetectionInterval: the time interval, in milliseconds, at which values will be compared to check for a Tremor. 
        public const int DetectionInterval = 50;

        // DownSampleRate: measured in Hz, the desired rate for the accelerometer values to be downsampled to
        public const int DownSampleRate = 50;

        // BuildNumber: the current version of the Tremor Trainer App
        public const string BuildNumber = "0.0.4";



        // Debug, info, and exception messages
        public const string ContactEmail = "gavin@bionicpanda.net";
        public const string AppName = "Tremor Trainer";
        public const string DeviceNotSupportedMessage =
            "Unfortunately, this device does not have an Accelerometer and we cannot measure your tremor levels";
        public static string UnknownErrorMessage =
            $"An Unknown error has occurred. Please contact the Developer at {ContactEmail}";
        public const string AboutMessage =
            "The Tremor Trainer app was developed in collaboration with neurologists at the University of Virginia and University of Cincinnati for treatment of functional tremor, which is a subset of Functional Neurologic Disorder. It is currently an experimental treatment only to be used in the setting of a clinical trial." +
            "\n Citation: Espay AJ, Edwards MJ, Oggioni GD, et al. Tremor retrainment as therapeutic strategy in psychogenic (functional) tremor. Parkinsonism & Related Disorders. 2014;20(6):647-650. doi:10.1016/j.parkreldis.2014.02.029 ";
        
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
                return Path.Combine(basePath, CsvFileName);
            }
        }


    }
}
