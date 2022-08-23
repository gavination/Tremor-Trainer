using System;
using System.IO;
using System.Threading.Tasks;
using AVFoundation;
using Foundation;
using TremorTrainer.iOS;
using TremorTrainer.Services;
using Xamarin.Forms;

[assembly: Dependency(typeof(SoundService))]
namespace TremorTrainer.iOS
{
    public class SoundService : ISoundService
    {

        private AVAudioPlayer _player;
        private string fileName;
        private NSUrl url;

        public SoundService()
        {
            fileName = "metronomeding.mp3";
            string sFilePath = NSBundle.MainBundle.PathForResource
                (Path.GetFileNameWithoutExtension(fileName), Path.GetExtension(fileName));
            url = NSUrl.FromString(sFilePath);
            _player = AVAudioPlayer.FromUrl(url);

        }
        public Task playSound()
        {
            _player.Play();

            _player.FinishedPlaying += (object sender, AVStatusEventArgs e) =>
            {
                // debug code to determine sound event plays
                //Console.WriteLine("Sound playing.");
            };
            return Task.CompletedTask;
        }
    }
}