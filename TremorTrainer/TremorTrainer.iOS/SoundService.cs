using System;
using System.IO;
using System.Threading.Tasks;
using AVFoundation;
using CoreVideo;
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

        private NSUrl url;
        private string fileName;
        private int playerIndex;
        private int playCount;
        private NSError error;
        private AVAudioPlayerNode playerNode;
        private AVAudioPcmBuffer buffer;

        public SoundService()
        {

            fileName = "metronomeding.mp3";
            playerIndex = 0;
            playCount = 0;
            error = new NSError();


            string sFilePath = NSBundle.MainBundle.PathForResource
                (Path.GetFileNameWithoutExtension(fileName), Path.GetExtension(fileName));
            url = NSUrl.FromString(sFilePath);

            audioFile = new AVAudioFile(url, AVAudioCommonFormat.PCMFloat32, false, out error);
            var audioFormat = new AVAudioFormat();


            buffer = new AVAudioPcmBuffer(audioFile.ProcessingFormat, (uint)audioFile.Length);
            audioFile.ReadIntoBuffer(buffer, out error);
            audioEngine = new AVAudioEngine();

            playerNode = new AVAudioPlayerNode();

            audioEngine.AttachNode(playerNode);
            audioEngine.Connect(playerNode, audioEngine.MainMixerNode, audioFile.ProcessingFormat);
            audioEngine.Prepare();
            //audioEngine.EnableManualRenderingMode(AVAudioEngineManualRenderingMode.Realtime, buffer.Format, buffer.FrameLength, out error);
            audioEngine.StartAndReturnError(out error);

            AVAudioSession.SharedInstance().Init();
            AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.Playback);
            AVAudioSession.SharedInstance().SetMode(AVAudioSession.ModeDefault, out error);
            AVAudioSession.SharedInstance().SetActive(true);

        }


        public Task playSound()
        {
            try
            {
                playerNode.ScheduleBuffer(buffer, null);
                playerNode.Play();

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Playback Error: {ex.Message}");
                throw;
            }
        }
    }
}