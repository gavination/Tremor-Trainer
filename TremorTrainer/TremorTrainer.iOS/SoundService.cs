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

        private AVAudioFile audioFile;
        private AVAudioEngine audioEngine;
        private AVAudioPlayerNode playerNode;

        private NSUrl url;
        private string fileName;


        public SoundService()
        {
            fileName = "metronomeding.mp3";
            string sFilePath = NSBundle.MainBundle.PathForResource
                (Path.GetFileNameWithoutExtension(fileName), Path.GetExtension(fileName));
            url = NSUrl.FromString(sFilePath);
            var error = new NSError();

            audioFile = new AVAudioFile(url, AVAudioCommonFormat.PCMFloat32, false, out error);
            audioEngine = new AVAudioEngine();
            playerNode = new AVAudioPlayerNode();

            // attach the player node to the engine
            audioEngine.AttachNode(playerNode);
            audioEngine.Connect(playerNode, audioEngine.OutputNode, audioFile.ProcessingFormat);

        }


        public Task playSound()
        {

            playerNode.ScheduleFile(audioFile, null, null);

            try
            {
                var error = new NSError();
                audioEngine.StartAndReturnError(out error);
                playerNode.Play();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Playback Error: {ex.Message}");
                throw;
            }
            return Task.CompletedTask;
        }

    }
}