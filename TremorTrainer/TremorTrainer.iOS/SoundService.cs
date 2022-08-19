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

        }
        public Task playSound()
        {
            _player = AVAudioPlayer.FromUrl(url);
            _player.Play();

            _player.FinishedPlaying += (object sender, AVStatusEventArgs e) =>
            {
                _player = null;
            };
            return Task.CompletedTask;
        }
    }
}