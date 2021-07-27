using SQLite;
using System;
using System.Collections.Generic;
using System.Text;
using TremorTrainer.Models;
using Unity;

namespace TremorTrainer.Utilities
{
    public class DBConnection : IConnection
    {
        public SQLiteConnection Connection { get; private set; }

        public DBConnection(string dbPath, SQLiteOpenFlags flags)
        {
            Connection = new SQLiteConnection(dbPath, flags);
        }

        public void Dispose()
        {
            Connection.Dispose();
        }
    }
    public interface IConnection : IDisposable
    {
        SQLiteConnection Connection { get; }
    }
}
