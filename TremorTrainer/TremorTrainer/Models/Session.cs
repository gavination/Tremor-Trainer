using System;

namespace TremorTrainer.Models
{
    public class Session
    {
        [SQLite.PrimaryKey]
        public Guid Id { get; set; }
        public string Text { get; set; }
        public string Description { get; set; }
    }
}