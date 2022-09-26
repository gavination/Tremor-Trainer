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
            audioEngine.Prepare();
            try
            {
                audioEngine.StartAndReturnError(out error);
                playerNode.ScheduleFile(audioFile, null, null);

            }
            catch (Exception ex){
                Console.WriteLine($"Playback Error: {ex.Message}");
                throw;
            }

        }


        public Task playSound()
        {

            playerNode.Play();
            return Task.CompletedTask;

        }

        public Task stopPlayback()
        {
            try
            {
                audioEngine.Stop();
                playerNode.Stop();

                return Task.CompletedTask;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Unable to stop playback: {ex.Message}");
                throw;
            }
        }

    }
}