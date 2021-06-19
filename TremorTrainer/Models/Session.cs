using System;
namespace TremorTrainer.Models
{
    public class Session
    {
        public string UserId { get; set; }
        public string SessionId { get; set; }
        public string  SessionLength { get; set; }
        public SessionType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public SessionStatus Status { get; set; }
        public string Description{ get; set; }
    }

    public enum SessionStatus
    {
       Passed,
       Failed
    }
    public enum SessionType
    {
        Introductory,
        Therapeutic
    }
}

