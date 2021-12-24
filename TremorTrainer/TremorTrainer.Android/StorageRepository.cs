using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using MathNet.Numerics;
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

        public string GetDownloadPath()
        {
            var ctx = Android.App.Application.Context;
            return ctx.GetExternalFilesDir(Android.OS.Environment.DirectoryDocuments).Path;
        }
    }
}
