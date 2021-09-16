using CsvHelper;
using SQLite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using TremorTrainer.Models;
using TremorTrainer.Utilities;
using Xamarin.Forms;

namespace TremorTrainer.Repositories
{
    public class SessionRepository : ISessionRepository
    {
        private readonly IConnection _database;
        private readonly IStorageRepository _storageRepository;

        public SessionRepository(IConnection dbConnection)
        {
            _database = dbConnection;
            _database.Connection.CreateTable<Session>();
            //DeviceOrientation orientation = DependencyService.Get<IDeviceOrientationService>().GetOrientation();

            _storageRepository = DependencyService.Get<IStorageRepository>();
        }

        public List<Session> GetSessions()
        {
            return _database.Connection.Table<Session>().ToList();
        }

        public Session GetSessionById(Guid id)
        {
            return _database.Connection.Table<Session>().FirstOrDefault(i => i.Id == id);
        }

        public int AddSession(Session session)
        {
            //check to see if the session already exists
            Session fetchedSession = GetSessionById(session.Id);

            if (fetchedSession != null)
            {
                return _database.Connection.Update(session);
            }
            else
            {
                return _database.Connection.Insert(session);
            }
        }

        public int DeleteSession(Session session)
        {
            return _database.Connection.Delete(session);
        }

        public string ExportSessions(List<Session> sessions)
        {
            var path = _storageRepository.GetDownloadPath();
            var filename = DateTime.Now.ToString("MMMM dd HH:mm:ss") + ".csv";
            var filepath = Path.Combine(path, filename);

            if (sessions.Count > 0)
            {
                // perform export operation
                using (var writer = new StreamWriter(filepath))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(sessions);
                    return filepath;
                }
            }
            else
            {
                // argument must have records
                return null;
            }

        }

    }

    public interface ISessionRepository
    {
        int AddSession(Session newItem);
        List<Session> GetSessions();
        string ExportSessions(List<Session> sessions);
    }

}
