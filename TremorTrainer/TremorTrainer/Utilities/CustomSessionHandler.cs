using Newtonsoft.Json;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xamarin.Essentials;

namespace TremorTrainer.Utilities
{
    public class CustomSessionHandler : IGotrueSessionPersistence<Session>
    {
        public void SaveSession(Session session)
        {
            var cacheFileName = ".gotrue.cache";

            try
            {
                var cacheDir = FileSystem.CacheDirectory;
                var path = Path.Combine(cacheDir, cacheFileName);
                var str = JsonConvert.SerializeObject(session);

                using (StreamWriter file = new StreamWriter(path))
                {
                    file.Write(str);
                    file.Dispose();
                };
                Console.WriteLine("!--------------SAVED SESSION--------------!");
            }
            catch (Exception err)
            {
                Console.WriteLine("Unable to write cache file." + err);
            }

        }

        public void DestroySession()
        {
            // Destroy Session on Filesystem or in browser storage
            var cacheFileName = ".gotrue.cache";
            var cacheDir = FileSystem.AppDataDirectory;
            var path = Path.Combine(cacheDir, cacheFileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            //Other logic Delete cache

            Console.WriteLine("!--------------DESTROY SESSION--------------!");
        }

        public Session LoadSession()
        {
            try
            {
                var cacheFileName = ".gotrue.cache";
                var cacheDir = FileSystem.CacheDirectory;
                var path = Path.Combine(cacheDir, cacheFileName);
                using (var reader = new StreamReader(path))
                {
                    string json = reader.ReadToEnd();
                    // Retrieve Session from Filesystem or from browser storage
                    Console.WriteLine("!--------------LOAD SESSION--------------!");
                    return JsonConvert.DeserializeObject<Session>(json);
                }

            }
            catch (Exception err)
            {
                Console.WriteLine("Unable to write cache file." + err);
                return null;
            }
        }
    }
}
