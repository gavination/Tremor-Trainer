using System;
using AVFoundation;

namespace TremorTrainer.iOS
{
    public class Player
    {
        public PlayerStatus Status;
        public AVAudioPlayerNode Node;

        public Player()
        {
            Status = PlayerStatus.Idle;
            Node = new AVAudioPlayerNode();
        }
    }
    public enum PlayerStatus
    {
        Idle,
        Busy
    };
}

