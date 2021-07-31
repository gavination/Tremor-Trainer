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
        public float XAverageVariance { get; set; }
        public float YAverageVariance { get; set; }
        public float ZAverageVariance { get; set; }
        public float XBaseline { get; set; }
        public float YBaseline { get; set; }
        public float ZBaseline { get; set; }
        public int Score { get; set; }
        public string Details { get; set; }
        public string Status { get; set; }
    }
}