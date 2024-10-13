using CsvHelper;
using MathNet.Numerics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
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

        public int DeleteSessions()
        {
            return _database.Connection.DeleteAll<Session>();
        }

        public string ExportSessions(List<Session> sessions)
        {
            var path = _storageRepository.GetDownloadPath();
            var filename = DateTime.Now.ToString("yyyy'-'MM'-'dd'-'HH'-'mm'-'ss'Z'") + ".csv";

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
        public string ExportReadings(List<Vector3> readings, string axisName)
        {
            var path = _storageRepository.GetDownloadPath();
            var filename = $"{axisName}ReadingData.json";
            var filepath = Path.Combine(path, filename);


            if (readings.Count > 0)
            {
                using (var streamWriter = new StreamWriter(filepath))
                using (var jsonWriter = new JsonTextWriter(streamWriter))
                {
                    var json = JsonConvert.SerializeObject(readings);
                    streamWriter.Write(json);
                }
                return filepath;
            }
            else
            {
                return null;
            }

        }
    }

    public interface ISessionRepository
    {
        int AddSession(Session session);
        int DeleteSessions();
        List<Session> GetSessions();
        string ExportSessions(List<Session> sessions);
        string ExportReadings(List<Vector3> readings, string axisName);
    }
}