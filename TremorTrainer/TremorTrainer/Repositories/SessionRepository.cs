using SQLite;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TremorTrainer.Models;
using TremorTrainer.Utilities;

namespace TremorTrainer.Repositories
{
    class SessionRepository : ISessionRepository
    {
        static SQLiteAsyncConnection Database;

        public static readonly AsyncLazy<SessionRepository> Instance = new AsyncLazy<SessionRepository>(async () =>
        {
            var instance = new SessionRepository();
            CreateTableResult result = await Database.CreateTableAsync<Session>();
            return instance;
        });

        public SessionRepository()
        {
            Database = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
        }

        public Task<List<Session>> GetSessionsAsync()
        {
            return Database.Table<Session>().ToListAsync();
        }


        public async Task<Session> GetSessionByIdAsync(Guid id)
        {
            return await Database.Table<Session>().Where(i => i.Id == id).FirstOrDefaultAsync();
        }

        public async Task<int> SaveSessionAsync(Session session)
        {
            //check to see if the session already exists
            Session fetchedSession = await GetSessionByIdAsync(session.Id);

            if (fetchedSession != null)
            {
                return await Database.UpdateAsync(session);
            }
            else
            {
                return await Database.InsertAsync(session);
            }
        }

        public Task<int> DeleteSessionAsync(Session session)
        {
            return Database.DeleteAsync(session);
        }

    }

    public interface ISessionRepository
    {

    }
}
