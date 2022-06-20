using System;
using System.IO;
using AVFoundation;
using Foundation;
using TremorTrainer.Services;
using Xamarin.Forms;

namespace TremorTrainer.iOS
{
	public class SoundService : ISoundService
	{

        AVAudioPlayer _player;


        public bool playSound()
        {
            var fileName = "metronomeding.mp3";
            string sFilePath = NSBundle.MainBundle.PathForResource
              (Path.GetFileNameWithoutExtension(fileName), Path.GetExtension(fileName));
            var url = NSUrl.FromString(sFilePath);
            _player = AVAudioPlayer.FromUrl(url);
            _player.FinishedPlaying += (object sender, AVStatusEventArgs e) => {
                _player = null;
            };
            _player.Play();
            return true;
        }
    }
}