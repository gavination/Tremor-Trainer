using System;
using System.Threading.Tasks;
using Android.Media;
using TremorTrainer.Droid;
using TremorTrainer.Services;
using Xamarin.Forms;

[assembly: Dependency(typeof(SoundService))]
namespace TremorTrainer.Droid
{
	public class SoundService : ISoundService
	{
        private MediaPlayer _mediaPlayer;

        public Task playSound()
        {
            _mediaPlayer = MediaPlayer.Create(global::Android.App.Application.Context,
                Resource.Raw.metronomeding);
            _mediaPlayer.Start();
            return Task.CompletedTask;
        }
    }
}

