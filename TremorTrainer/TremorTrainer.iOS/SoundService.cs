using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AVFoundation;
using CoreFoundation;
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
        private static Mutex mutex = new Mutex();

        private NSUrl url;
        private string fileName;
        private int maxPlayerCount;
        private NSError error;
        private List<Player> playerNodes;
        private AVAudioPcmBuffer buffer;
        private DispatchQueue dispatchQueue;
        private Action playAction;

        public SoundService()
        {

            fileName = "metronomeding.mp3";
            maxPlayerCount = 100;
            error = new NSError();
            dispatchQueue = new DispatchQueue("com.uva.tremortrainer.audioPlayback");
            playAction = PlayAsync;

            string sFilePath = NSBundle.MainBundle.PathForResource
                (Path.GetFileNameWithoutExtension(fileName), Path.GetExtension(fileName));
            url = NSUrl.FromString(sFilePath);

            audioFile = new AVAudioFile(url, AVAudioCommonFormat.PCMFloat32, false, out error);
            var audioFormat = new AVAudioFormat();


            buffer = new AVAudioPcmBuffer(audioFile.ProcessingFormat, (uint)audioFile.Length);
            audioFile.ReadIntoBuffer(buffer, out error);
            audioEngine = new AVAudioEngine();
            playerNodes = new List<Player>();
            playerNodes.Add(createAndAttachPlayerNode());

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
                dispatchQueue.DispatchAsync(playAction);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Playback Error: {ex.Message}");
                throw;
            }
        }

        public void PlayAsync() {
            var playerNode = getFirstAvailableNode();
            playerNode.Node.ScheduleBuffer(buffer, delegate() { playerNode.Status = PlayerStatus.Idle; });
            playerNode.Node.Play();
        }

        private Player getFirstAvailableNode()
        {
            // start of locked mutex scope
            mutex.WaitOne();
                var node = playerNodes.Find(n => n.Status == PlayerStatus.Idle);
                if (node == null && playerNodes.Count < maxPlayerCount)
                {
                    node = createAndAttachPlayerNode();
                    playerNodes.Add(node);
                }
                node.Status = PlayerStatus.Busy;
            mutex.ReleaseMutex();
            return node;
        }

        private Player createAndAttachPlayerNode()
        {
            var playerNode = new Player();
            audioEngine.AttachNode(playerNode.Node);
            audioEngine.Connect(playerNode.Node, audioEngine.MainMixerNode, audioFile.ProcessingFormat);

            return playerNode;
        }
    }
}