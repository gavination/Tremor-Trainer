using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using TremorTrainer.Droid;
using TremorTrainer.Models;
using TremorTrainer.Repositories;
using Xamarin.Forms;

[assembly: Dependency(typeof(StorageRepository))]
namespace TremorTrainer.Droid
{
    public class StorageRepository : IStorageRepository
    {
        public StorageRepository()
        {
        }

        public bool ExportSessions(List<Session> sessions)
        {
            var ctx = Android.App.Application.Context;
            var path = ctx.GetExternalFilesDir(Android.OS.Environment.DirectoryDocuments).Path;
            var filename = DateTime.Now.ToString("MMMM dd HH:mm:ss") + ".csv";
            var filepath = Path.Combine(path, filename);
           

            if (sessions.Count > 0)
            {
                // perform export operation
                using (var writer = new StreamWriter(filepath))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteHeader<Session>();
                    csv.NextRecord();
                    foreach (var session in sessions)
                    {
                        csv.WriteRecord(session);
                        csv.NextRecord();
                    }
                    return true;
                }
            }
            else
            {
                // argument must have records
                return false;
            }
        }

        public string GetDownloadPath()
        {
            var ctx = Android.App.Application.Context;
            return ctx.GetExternalFilesDir(Android.OS.Environment.DirectoryDocuments).Path;
        }
    }
}
