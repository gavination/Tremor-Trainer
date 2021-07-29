using SQLite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TremorTrainer.Models;
using TremorTrainer.Utilities;

namespace TremorTrainer.Repositories
{
    public class SessionRepository : ISessionRepository
    {
        private IConnection _database;

        public SessionRepository(IConnection dbConnection)
        {
            _database = dbConnection;
            _database.Connection.CreateTable<Session>();
        }

        public List<Session> GetSessions()
        {
            return _database.Connection.Table<Session>().ToList();
        }

        public async Task<Session> GetSessionById(Guid id)
        {
            return _database.Connection.Table<Session>().FirstOrDefault(i => i.Id == id);
        }

        public async Task<int> AddSession(Session session)
        {
            //check to see if the session already exists
            Session fetchedSession = await GetSessionById(session.Id);

            if (fetchedSession != null)
            {
                return  _database.Connection.Update(session);
            }
            else
            {
                return  _database.Connection.Insert(session);
            }
        }

        public int DeleteSessionAsync(Session session)
        {
            return _database.Connection.Delete(session);
        }
    }

    public interface ISessionRepository
    {
        Task<int> AddSession(Session newItem);
       List<Session> GetSessions();
    }
}
