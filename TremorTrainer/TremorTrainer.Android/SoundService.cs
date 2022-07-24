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
        private SoundPool _soundPool;
        private int _soundId;

        public SoundService()
        {
            var builder = new SoundPool.Builder();
            builder.SetMaxStreams(1);

            _soundPool = builder.Build();
            _soundId = _soundPool.Load(global::Android.App.Application.Context, Resource.Raw.metronomeding, 1);
        }

        public Task playSound()
        {
            var streamId = _soundPool.Play(_soundId, 1, 1, 1, 0, 1.0f);
            Console.WriteLine($"Playing sound with stream id: {streamId}");

            return Task.CompletedTask;
        }
    }
}

