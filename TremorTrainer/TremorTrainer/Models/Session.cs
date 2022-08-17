using System;
using System.Numerics;

namespace TremorTrainer.Models
{
    public class Session
    {
        [SQLite.PrimaryKey]
        public Guid Id { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public SessionType Type { get; set; }
        public string Details { get; set; }
        public string Status { get; set; }
    }

    public enum SessionType
    {
        Induction,
        Maintenance,
        AsNeeded
    }
}