using Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIKit;
using Xamarin.Forms;
using TremorTrainer.Repositories;
using TremorTrainer.iOS;


[assembly: Dependency(typeof(StorageRepository))]
namespace TremorTrainer.iOS
{
    public class StorageRepository : IStorageRepository
    {
        public string GetDownloadPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

    }
}